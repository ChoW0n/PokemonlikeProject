using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class PlayerProgressionStore
{
    private readonly DatabaseContextExecutor _database;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    [ActivatorUtilitiesConstructor]
    public PlayerProgressionStore(DatabaseContextExecutor database)
    {
        _database = database;
    }

    public PlayerProgressionStore(AppDbContext db)
        : this(new DatabaseContextExecutor(db))
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
            profile.MovePreferencesJson = JsonSerializer.Serialize(preferences, JsonOptions);
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
            profile.MovePreferencesJson = JsonSerializer.Serialize(preferences, JsonOptions);
            profile.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });
    }

    public async Task<List<PokemonLoadout>?> GetPendingRivalAsync(string username)
    {
        return await _database.ExecuteAsync("progression.pending-rival", async db =>
        {
            var profile = await GetOrCreateAsync(db, username);
            if (!profile.RivalPending) return null;

            var latest = DeserializeLoadouts(profile.LatestLoadoutsJson);
            var preferences = DeserializePreferences(profile.MovePreferencesJson);
            return BuildRivalLoadouts(latest, preferences);
        });
    }

    public async Task CompleteBattleAsync(
        string username,
        IEnumerable<PokemonLoadout> latestLoadouts,
        bool isRivalBattle,
        bool won)
    {
        var loadouts = latestLoadouts.ToList();
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
                    string rewardMoveKey = PickRewardMove(profile);
                    await AddTechnicalMachineAsync(db, username, rewardMoveKey);
                    await AddMessageIfMissingAsync(
                        db,
                        username,
                        $"{keyPrefix}-reward",
                        "기술머신 보상",
                        $"{MoveDatabase.All[rewardMoveKey].Name} 기술머신을 1개 획득했습니다.");
                }
            }
            else
            {
                profile.CompletedBattles++;
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
            string rewardMoveKey = PickRewardMove(profile);
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

    private static string PickRewardMove(PlayerProgression profile)
    {
        var latest = DeserializeLoadouts(profile.LatestLoadoutsJson);
        var preferences = DeserializePreferences(profile.MovePreferencesJson);
        var alreadySelected = latest
            .SelectMany(loadout => loadout.ChosenMoveNames)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = latest
            .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
            .SelectMany(loadout => PokemonDatabase.All[loadout.PokemonId].MoveNames)
            .Distinct()
            .Where(moveKey => !alreadySelected.Contains(moveKey))
            .OrderByDescending(moveKey => MovePreferenceRules.CountFor(preferences, moveKey))
            .ThenBy(moveKey => moveKey, StringComparer.Ordinal)
            .ToList();
        return candidates.FirstOrDefault()
            ?? latest
                .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
                .SelectMany(loadout => PokemonDatabase.All[loadout.PokemonId].MoveNames)
                .FirstOrDefault()
            ?? "tackle";
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
        JsonSerializer.Serialize(loadouts.ToList(), JsonOptions);

    private static List<PokemonLoadout> DeserializeLoadouts(string json) =>
        JsonSerializer.Deserialize<List<PokemonLoadout>>(json, JsonOptions)
        ?? new List<PokemonLoadout>();

    private static MovePreferenceProfile DeserializePreferences(string json) =>
        JsonSerializer.Deserialize<MovePreferenceProfile>(json, JsonOptions)
        ?? new MovePreferenceProfile();
}