using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;
using System.Text.Json;

namespace PokemonBattle.Services;

public sealed class PlayerProgressionStore
{
    private readonly DatabaseContextExecutor _database;
    private readonly Random _random;

    [ActivatorUtilitiesConstructor]
    public PlayerProgressionStore(DatabaseContextExecutor database)
        : this(database, Random.Shared)
    {
    }

    private PlayerProgressionStore(DatabaseContextExecutor database, Random random)
    {
        _database = database;
        _random = random;
    }

    public PlayerProgressionStore(AppDbContext db)
        : this(db, Random.Shared)
    {
    }

    public PlayerProgressionStore(AppDbContext db, Random random)
        : this(new DatabaseContextExecutor(db), random)
    {
    }

    public async Task<(int completedBattles, bool rivalPending, List<MailboxMessage> messages,
        List<TechnicalMachineInventory> machines)> LoadAsync(string username)
    {
        return await _database.ExecuteAsync("progression.load", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            var messages = await db.MailboxMessages
                .AsNoTracking()
                .Where(message => message.Username == username)
                .OrderByDescending(message => message.CreatedAtUtc)
                .ToListAsync();
            var machines = await db.TechnicalMachines
                .AsNoTracking()
                .Where(machine => machine.Username == username && machine.Quantity > 0)
                .OrderBy(machine => machine.MoveKey)
                .ToListAsync();
            return (profile.CompletedBattles, profile.RivalPending, messages, machines);
        });
    }

    public async Task SaveLatestLoadoutsAsync(string username, IEnumerable<PokemonLoadout> loadouts)
    {
        await _database.ExecuteAsync("progression.save-loadouts", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            profile.LatestLoadoutsJson = SerializeLoadouts(loadouts);
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });
    }

    public async Task RecordMoveSelectionAsync(string username, string moveKey)
    {
        await _database.ExecuteAsync("progression.record-move", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            var preferences = DeserializePreferences(profile.MovePreferencesJson);
            MovePreferenceRules.Record(preferences, moveKey);
            profile.MovePreferencesJson = System.Text.Json.JsonSerializer.Serialize(preferences);
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });
    }

    public async Task RecordTeamSelectionsAsync(string username, IEnumerable<PokemonLoadout> loadouts)
    {
        await _database.ExecuteAsync("progression.record-team", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            var preferences = DeserializePreferences(profile.MovePreferencesJson);
            foreach (var moveKey in loadouts.SelectMany(loadout => loadout.ChosenMoveNames))
            {
                MovePreferenceRules.Record(preferences, moveKey);
            }
            profile.MovePreferencesJson = System.Text.Json.JsonSerializer.Serialize(preferences);
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });
    }

    public async Task<PendingRival?> GetPendingRivalAsync(string username)
    {
        return await _database.ExecuteAsync("progression.pending-rival", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            if (!profile.RivalPending) return null;

            double currentRating = await db.PlayerSkillRatings
                .AsNoTracking()
                .Where(rating => rating.Username == username)
                .Select(rating => (double?)rating.Rating)
                .FirstOrDefaultAsync()
                ?? SkillRatingCalculator.DefaultRating;

            // 자기 자신·관리자·빈 팀은 라이벌 후보에서 제외한다.
            var candidateProfiles = await (
                from candidate in db.PlayerProgressions.AsNoTracking()
                join account in db.Users.AsNoTracking()
                    on candidate.Username equals account.Username
                where candidate.Username != username && !account.IsAdmin
                select new
                {
                    candidate.Username,
                    candidate.LatestLoadoutsJson,
                    candidate.MovePreferencesJson
                })
                .ToListAsync();

            var candidateUsernames = candidateProfiles
                .Select(candidate => candidate.Username)
                .ToList();
            var candidateRatings = await db.PlayerSkillRatings
                .AsNoTracking()
                .Where(rating => candidateUsernames.Contains(rating.Username))
                .ToDictionaryAsync(rating => rating.Username, rating => rating.Rating);

            var candidates = new List<RivalCandidate>();
            foreach (var candidate in candidateProfiles)
            {
                try
                {
                    var latest = DeserializeLoadouts(candidate.LatestLoadoutsJson);
                    if (latest.Count == 0) continue;

                    var preferences = DeserializePreferences(candidate.MovePreferencesJson);
                    var loadouts = BuildRivalLoadouts(latest, preferences);
                    if (loadouts.Count == 0) continue;

                    double rating = candidateRatings.GetValueOrDefault(
                        candidate.Username,
                        SkillRatingCalculator.DefaultRating);
                    candidates.Add(new RivalCandidate(
                        candidate.Username,
                        double.IsFinite(rating)
                            ? Math.Clamp(rating, 400, 2000)
                            : SkillRatingCalculator.DefaultRating,
                        loadouts));
                }
                catch (JsonException)
                {
                    // 깨진 상대 프로필은 후보에서 건너뛴다.
                }
            }

            if (candidates.Count == 0)
            {
                // 상대가 없으면 예약을 소비하고 일반 전투로 진행한다.
                profile.RivalPending = false;
                profile.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return null;
            }

            double safeCurrentRating = double.IsFinite(currentRating)
                ? Math.Clamp(currentRating, 400, 2000)
                : SkillRatingCalculator.DefaultRating;
            double closestDistance = candidates
                .Min(candidate => Math.Abs(candidate.Rating - safeCurrentRating));
            var closestCandidates = candidates
                .Where(candidate =>
                    Math.Abs(candidate.Rating - safeCurrentRating) <= closestDistance + 0.0001)
                .ToList();
            const double similarRatingRange = 100;
            var similarCandidates = candidates
                .Where(candidate =>
                    Math.Abs(candidate.Rating - safeCurrentRating) <= similarRatingRange)
                .ToList();
            var selectionPool = similarCandidates.Count > 0
                ? similarCandidates
                : closestCandidates;
            var selected = selectionPool[_random.Next(selectionPool.Count)];
            return new PendingRival(selected.Username, selected.Loadouts);
        });
    }

    public async Task CompleteBattleAsync(
        string username,
        IEnumerable<PokemonLoadout> latestLoadouts,
        bool isRivalBattle,
        bool won,
        int round = 1,
        int turns = 0,
        double playerHpRatio = 0,
        double enemyHpRatio = 0,
        bool isLegendaryBattle = false,
        int difficultyAdjustment = 0,
        double? skillRating = null)
    {
        var loadouts = latestLoadouts.ToList();
        await TryRecordBattleResultAsync(
            username,
            isRivalBattle,
            won,
            round,
            turns,
            playerHpRatio,
            enemyHpRatio,
            isLegendaryBattle,
            difficultyAdjustment,
            skillRating);
        await _database.ExecuteAsync("progression.complete-battle", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            profile.LatestLoadoutsJson = SerializeLoadouts(loadouts);

            if (isRivalBattle)
            {
                if (!profile.RivalPending) return;

                profile.RivalPending = false;
                string keyPrefix = $"rival-{profile.RivalNumber}";
                await AddMessageIfMissingAsync(
                    db,
                    username,
                    $"{keyPrefix}-{(won ? "win" : "loss")}",
                    won ? "라이벌전 승리" : "라이벌전 결과",
                    won
                        ? "플레이 성향을 반영한 라이벌을 이겼습니다. 보관함에 기술머신 1개를 지급했습니다."
                        : "라이벌에게 패배했습니다. 다음 일반 전투에서 다시 도전할 수 있습니다.");

                if (won)
                {
                    var ownedMoveKeys = await GetOwnedTechnicalMachineKeysAsync(db, username);
                    string? rewardMoveKey = PickRewardMove(profile, ownedMoveKeys);
                    if (rewardMoveKey != null)
                    {
                        await AddTechnicalMachineAsync(db, username, rewardMoveKey);
                        await AddMessageIfMissingAsync(
                            db,
                            username,
                            $"{keyPrefix}-reward",
                            "기술머신 보상",
                            $"{MoveDatabase.All[rewardMoveKey].Name} 기술머신을 1개 획득했습니다.");
                    }
                }
            }
            else
            {
                profile.CompletedBattles++;
                if (won && _random.Next(100) < 12)
                {
                    var ownedMoveKeys = await GetOwnedTechnicalMachineKeysAsync(db, username);
                    string? rewardMoveKey = PickRewardMove(profile, ownedMoveKeys);
                    if (rewardMoveKey != null)
                    {
                        await AddTechnicalMachineAsync(db, username, rewardMoveKey);
                        await AddMessageIfMissingAsync(
                            db,
                            username,
                            $"battle-{profile.CompletedBattles}-technical-machine",
                            "기술머신 획득",
                            $"{MoveDatabase.All[rewardMoveKey].Name} 기술머신을 1개 획득했습니다.");
                    }
                }

                if (profile.CompletedBattles % 50 == 0 && loadouts.Count > 0)
                {
                    profile.RivalPending = true;
                    profile.RivalNumber = profile.CompletedBattles / 50;
                    await AddMessageIfMissingAsync(
                        db,
                        username,
                        $"rival-{profile.RivalNumber}-scheduled",
                        "라이벌전 예약",
                        $"일반 전투 {profile.CompletedBattles}회를 완료했습니다. 다음 상대는 라이벌입니다.");
                }
            }

            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });
    }

    private async Task TryRecordBattleResultAsync(
        string username,
        bool isRivalBattle,
        bool won,
        int round,
        int turns,
        double playerHpRatio,
        double enemyHpRatio,
        bool isLegendaryBattle,
        int difficultyAdjustment,
        double? skillRating)
    {
        try
        {
            await _database.ExecuteAsync("progression.record-battle-result", async db =>
            {
                int rivalNumber = isRivalBattle
                    ? await db.PlayerProgressions
                        .Where(profile => profile.Username == username)
                        .Select(profile => profile.RivalNumber)
                        .FirstOrDefaultAsync()
                    : 0;
                // 종료 시점의 계정 지표를 스냅샷한다.
                var ratingRecord = await db.PlayerSkillRatings
                        .Where(rating => rating.Username == username)
                        .Select(rating => new
                        {
                            rating.Rating,
                            rating.CompletedRuns
                        })
                        .FirstOrDefaultAsync();
                double recordedRating = skillRating ?? ratingRecord?.Rating ?? 0;
                int unlockedCount = await db.UnlockedPokemons
                    .Where(unlock => unlock.Username == username)
                    .Select(unlock => unlock.PokemonId)
                    .Distinct()
                    .CountAsync();

                db.BattleResults.Add(new BattleResult
                {
                    Username = username,
                    IsRivalBattle = isRivalBattle,
                    IsLegendaryBattle = isLegendaryBattle,
                    RivalNumber = Math.Max(0, rivalNumber),
                    Won = won,
                    EndReason = won ? "win" : "team-wipe",
                    Round = Math.Max(1, round),
                    Turns = Math.Max(0, turns),
                    PlayerHpRatio = Math.Clamp(playerHpRatio, 0, 1),
                    EnemyHpRatio = Math.Clamp(enemyHpRatio, 0, 1),
                    DifficultyAdjustment = difficultyAdjustment,
                    SkillRating = recordedRating <= 0
                        ? SkillRatingCalculator.DefaultRating
                        : recordedRating,
                    UnlockedCount = Math.Max(0, unlockedCount),
                    RunSeq = Math.Max(0, ratingRecord?.CompletedRuns ?? 0)
                });
                await db.SaveChangesAsync();
            });
        }
        catch
        {
            // 통계 기록 실패가 전투 종료를 막지 않게 한다.
        }
    }

    public async Task<bool> MarkMessageReadAsync(string username, int messageId)
    {
        return await _database.ExecuteAsync("progression.mark-message-read", async db =>
        {
            var message = await db.MailboxMessages
                .FirstOrDefaultAsync(item => item.Id == messageId && item.Username == username);
            if (message == null) return false;
            message.IsRead = true;
            await db.SaveChangesAsync();
            return true;
        });
    }

    public async Task<int> MarkAllMessagesReadAsync(string username) =>
        await _database.ExecuteAsync("progression.mark-all-messages-read", async db =>
        {
            var messages = await db.MailboxMessages
                .Where(message => message.Username == username && !message.IsRead)
                .ToListAsync();
            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            if (messages.Count > 0)
            {
                await db.SaveChangesAsync();
            }
            return messages.Count;
        });

    public async Task<bool> TryConsumeTechnicalMachineAsync(string username, string moveKey)
    {
        if (!MoveDatabase.All.ContainsKey(moveKey)) return false;

        return await _database.ExecuteAsync("progression.consume-machine", async db =>
        {
            var machine = await db.TechnicalMachines
                .FirstOrDefaultAsync(item => item.Username == username
                    && item.MoveKey == moveKey
                    && item.Quantity > 0);
            if (machine == null) return false;

            machine.Quantity--;
            await db.SaveChangesAsync();
            return true;
        });
    }

    public async Task<string?> GrantTechnicalMachineRewardAsync(
        string username,
        IEnumerable<PokemonLoadout> latestLoadouts) =>
        await _database.ExecuteAsync("progression.grant-machine-reward", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            profile.LatestLoadoutsJson = SerializeLoadouts(latestLoadouts);
            var ownedMoveKeys = await GetOwnedTechnicalMachineKeysAsync(db, username);
            string? rewardMoveKey = PickRewardMove(profile, ownedMoveKeys);
            if (rewardMoveKey == null)
            {
                profile.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return null;
            }

            await AddTechnicalMachineAsync(db, username, rewardMoveKey);
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return MoveDatabase.All.TryGetValue(rewardMoveKey, out var move)
                ? move.Name
                : rewardMoveKey;
        });

    private static async Task<PlayerProgression> GetOrCreateAsync(
        AppDbContext db,
        string username)
    {
        var profile = await db.PlayerProgressions
            .FirstOrDefaultAsync(item => item.Username == username);
        if (profile != null) return profile;

        profile = new PlayerProgression { Username = username };
        db.PlayerProgressions.Add(profile);
        try
        {
            await db.SaveChangesAsync();
            return profile;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another operation may have created the profile between the
            // read and insert. Re-read it rather than surfacing a circuit
            // error for an idempotent get-or-create operation.
            db.Entry(profile).State = EntityState.Detached;
            return await db.PlayerProgressions.AsNoTracking()
                .SingleAsync(item => item.Username == username);
        }
    }

    private static async Task AddMessageIfMissingAsync(
        AppDbContext db,
        string username,
        string deduplicationKey,
        string title,
        string body)
    {
        bool exists = await db.MailboxMessages.AnyAsync(message =>
            message.Username == username && message.DeduplicationKey == deduplicationKey);
        if (exists) return;

        db.MailboxMessages.Add(new MailboxMessage
        {
            Username = username,
            DeduplicationKey = deduplicationKey,
            Title = title,
            Body = body
        });
    }

    private static async Task AddTechnicalMachineAsync(
        AppDbContext db,
        string username,
        string moveKey)
    {
        var machine = await db.TechnicalMachines.FirstOrDefaultAsync(item =>
            item.Username == username && item.MoveKey == moveKey);
        if (machine == null)
        {
            db.TechnicalMachines.Add(new TechnicalMachineInventory
            {
                Username = username,
                MoveKey = moveKey,
                Quantity = 1
            });
        }
        else
        {
            machine.Quantity++;
        }
    }

    private static async Task<HashSet<string>> GetOwnedTechnicalMachineKeysAsync(
        AppDbContext db,
        string username)
    {
        var ownedKeys = await db.TechnicalMachines
            .AsNoTracking()
            .Where(machine => machine.Username == username && machine.Quantity > 0)
            .Select(machine => machine.MoveKey)
            .ToListAsync();
        return ownedKeys.ToHashSet(StringComparer.Ordinal);
    }

    private string? PickRewardMove(
        PlayerProgression profile,
        IReadOnlySet<string> ownedMoveKeys)
    {
        var latest = DeserializeLoadouts(profile.LatestLoadoutsJson);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        var teamData = latest
            .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
            .Select(loadout => PokemonDatabase.All[loadout.PokemonId])
            .ToList();

        // TM 전용 기술의 팀 합집합을 먼저 사용해 새 선택지를 보상한다.
        var candidates = teamData
            .SelectMany(data => data.MachineOnlyMoveNames)
            .Where(MoveDatabase.All.ContainsKey)
            .Distinct()
            .Where(moveKey => !ownedMoveKeys.Contains(moveKey))
            .ToList();

        if (candidates.Count == 0)
        {
            // TM 전용 후보가 없을 때만 일반 기술 목록으로 폴백한다.
            candidates = teamData
                .SelectMany(data => data.MoveNames)
                .Where(MoveDatabase.All.ContainsKey)
                .Distinct()
                .Where(moveKey => !ownedMoveKeys.Contains(moveKey))
                .ToList();
        }

        if (candidates.Count == 0) return null;

        // 선호도는 약하게 반영하고, 최종 선택은 매번 무작위로 한다.
        var weightedCandidates = candidates
            .Select(moveKey => (moveKey, weight: RewardWeight(preferences, moveKey)))
            .ToList();
        int totalWeight = weightedCandidates.Sum(candidate => candidate.weight);
        int roll = _random.Next(totalWeight);
        foreach (var candidate in weightedCandidates)
        {
            if (roll < candidate.weight) return candidate.moveKey;
            roll -= candidate.weight;
        }

        return weightedCandidates[^1].moveKey;
    }

    private static int RewardWeight(MovePreferenceProfile preferences, string moveKey)
    {
        if (!MoveDatabase.All.TryGetValue(moveKey, out var move)) return 100;

        string category = move.IsStatus ? "status" : move.IsSpecial ? "special" : "physical";
        int exactCount = MovePreferenceRules.CountFor(preferences, moveKey);
        int typeCount = preferences.TypeCounts.TryGetValue(move.Type.ToString(), out var type) ? type : 0;
        int categoryCount = preferences.CategoryCounts.TryGetValue(category, out var categoryValue)
            ? categoryValue
            : 0;
        int tacticalCount = 0;
        if (move.Priority > 0)
            tacticalCount += preferences.TacticalCounts.TryGetValue("priority", out var priority) ? priority : 0;
        if (move.StatChanges.Count > 0)
            tacticalCount += preferences.TacticalCounts.TryGetValue("rank-up", out var rankUp) ? rankUp : 0;
        if (!string.Equals(move.AilmentName, "none", StringComparison.Ordinal))
            tacticalCount += preferences.TacticalCounts.TryGetValue("status-effect", out var statusEffect) ? statusEffect : 0;
        if (MoveRuleMetadata.IsProtectionMove(moveKey))
            tacticalCount += preferences.TacticalCounts.TryGetValue("protection", out var protection) ? protection : 0;
        if (!move.IsStatus)
            tacticalCount += preferences.TacticalCounts.TryGetValue("damage", out var damage) ? damage : 0;

        return 100
            + Math.Min(exactCount, 5) * 4
            + Math.Min(typeCount, 5) * 2
            + Math.Min(categoryCount, 5)
            + Math.Min(tacticalCount, 5);
    }

    private static List<PokemonLoadout> BuildRivalLoadouts(
        IEnumerable<PokemonLoadout> latest,
        MovePreferenceProfile preferences)
    {
        var rival = new List<PokemonLoadout>();
        foreach (var loadout in latest)
        {
            if (!PokemonDatabase.All.TryGetValue(loadout.PokemonId, out var data)) continue;

            var moves = data.MoveNames
                .Where(MoveDatabase.All.ContainsKey)
                .OrderByDescending(moveKey => MovePreferenceRules.CountFor(preferences, moveKey))
                .ThenBy(moveKey => moveKey, StringComparer.Ordinal)
                .Take(4)
                .ToList();
            if (moves.Count == 0) moves.Add(data.MoveNames.First());

            rival.Add(new PokemonLoadout
            {
                PokemonId = loadout.PokemonId,
                ChosenMoveNames = moves,
                ChosenAbility = data.AbilityNames.Contains(loadout.ChosenAbility)
                    ? loadout.ChosenAbility
                    : data.AbilityNames.FirstOrDefault() ?? "",
                ChosenItem = ItemDatabase.GetAvailableItems(data.Name)
                    .Any(item => item.Name == loadout.ChosenItem)
                    ? loadout.ChosenItem
                    : TeamLoadoutRules.NoItem,
                Level = Math.Max(1, loadout.Level)
            });
        }
        return TeamLoadoutRules.NormalizeUniqueItems(rival);
    }

    private static string SerializeLoadouts(IEnumerable<PokemonLoadout> loadouts) =>
        LoadoutJson.Serialize(loadouts);

    private static List<PokemonLoadout> DeserializeLoadouts(string json) =>
        LoadoutJson.Deserialize(json);

    private static MovePreferenceProfile DeserializePreferences(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<MovePreferenceProfile>(json)
        ?? new MovePreferenceProfile();

    private sealed record RivalCandidate(
        string Username,
        double Rating,
        List<PokemonLoadout> Loadouts);
}

public sealed record PendingRival(
    string Username,
    List<PokemonLoadout> Loadouts);