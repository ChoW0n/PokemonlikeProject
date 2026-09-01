using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class AdminDashboardService
{
    private readonly DatabaseContextExecutor _database;
    private readonly CurrentUserService _currentUser;
    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };

    [ActivatorUtilitiesConstructor]
    public AdminDashboardService(
        DatabaseContextExecutor database,
        CurrentUserService currentUser)
    {
        _database = database;
        _currentUser = currentUser;
    }

    public AdminDashboardService(AppDbContext db, CurrentUserService currentUser)
        : this(new DatabaseContextExecutor(db), currentUser)
    {
    }

    public async Task<bool> IsCurrentUserAdminAsync() =>
        await _database.ExecuteAsync("admin-dashboard.check-admin", async db =>
            await GetCurrentAdminAsync(db) != null);

    public async Task<AdminDashboardSnapshot?> LoadAsync() =>
        await _database.ExecuteAsync("admin-dashboard.load", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return null;

            var users = await db.Users.AsNoTracking()
                .OrderBy(user => user.Username)
                .ToListAsync();
            var runs = await db.PlayerRuns.AsNoTracking().ToListAsync();
            var presets = await db.UserPresets.AsNoTracking().ToListAsync();
            var unlocks = await db.UnlockedPokemons.AsNoTracking().ToListAsync();
            var progressions = await db.PlayerProgressions.AsNoTracking().ToListAsync();
            var auditLogs = await db.AdminAuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.CreatedAtUtc)
                .Take(25)
                .Select(log => new AdminAuditLogSnapshot(
                    log.AdminUsername,
                    log.Action,
                    log.TargetUsername,
                    log.Details,
                    log.CreatedAtUtc))
                .ToListAsync();
            var issues = new List<string>();

            foreach (var duplicateRun in runs.GroupBy(run => run.Username).Where(group => group.Count() > 1))
                issues.Add($"{duplicateRun.Key}: 진행 런 레코드가 {duplicateRun.Count()}개입니다.");

            foreach (var duplicateUnlock in unlocks
                .GroupBy(unlock => new { unlock.Username, unlock.PokemonId })
                .Where(group => group.Count() > 1))
            {
                issues.Add($"{duplicateUnlock.Key.Username}: 포켓몬 #{duplicateUnlock.Key.PokemonId} 해금 레코드가 중복됩니다.");
            }

            var userRows = users.Select(user =>
            {
                var run = runs
                    .Where(candidate => candidate.Username == user.Username)
                    .OrderByDescending(candidate => candidate.Id)
                    .FirstOrDefault();
                var userPresets = presets.Count(preset => preset.Username == user.Username);
                var userUnlocks = unlocks
                    .Where(unlock => unlock.Username == user.Username)
                    .Select(unlock => unlock.PokemonId)
                    .Distinct()
                    .Count();

                List<PokemonLoadout> loadouts = new();
                List<LegendaryEncounterHistoryEntry> history = new();
                if (run != null)
                {
                    try
                    {
                        loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(
                            run.LoadoutsJson, JsonOptions) ?? new();
                        history = JsonSerializer.Deserialize<List<LegendaryEncounterHistoryEntry>>(
                            run.LegendaryEncounterHistoryJson, JsonOptions) ?? new();
                    }
                    catch (JsonException)
                    {
                        issues.Add($"{user.Username}: 런 JSON을 읽을 수 없습니다.");
                    }

                    int invalidLoadouts = loadouts.Count(
                        loadout => !PokemonDatabase.All.ContainsKey(loadout.PokemonId));
                    if (invalidLoadouts > 0)
                        issues.Add($"{user.Username}: 알 수 없는 포켓몬이 포함된 로드아웃 {invalidLoadouts}개가 있습니다.");

                    int invalidHistoryIds = history
                        .SelectMany(entry => entry.PokemonIds)
                        .Count(id => !PokemonDatabase.All.ContainsKey(id));
                    if (invalidHistoryIds > 0)
                        issues.Add($"{user.Username}: 이력에 알 수 없는 포켓몬 번호가 {invalidHistoryIds}개 있습니다.");

                    if (run.LegendaryProgressPercent is < 0 or > LegendaryProgression.MaxProgressPercent)
                        issues.Add($"{user.Username}: 전설 진행도가 범위를 벗어났습니다.");
                }

                return new AdminUserSnapshot(
                    user.Username,
                    user.IsAdmin,
                    run?.CurrentScore ?? 0,
                    run?.HighScore ?? 0,
                    run?.LegendaryProgressPercent ?? 0,
                    userUnlocks,
                    userPresets,
                    history.Count);
            }).ToList();

            var analytics = BuildAnalytics(users, progressions, runs);

            return new AdminDashboardSnapshot(
                DateTimeOffset.UtcNow,
                users.Count,
                users.Count(user => user.IsAdmin),
                runs.Count,
                presets.Count,
                unlocks.Select(unlock => unlock.PokemonId).Distinct().Count(),
                PokemonDatabase.All.Count,
                MoveDatabase.All.Count,
                userRows,
                issues,
                auditLogs,
                analytics);
        });

    private static AdminAnalyticsSnapshot BuildAnalytics(
        IReadOnlyList<UserAccount> users,
        IReadOnlyList<PlayerProgression> progressions,
        IReadOnlyList<PlayerRun> runs)
    {
        var normalUsernames = users.Where(user => !user.IsAdmin)
            .Select(user => user.Username)
            .ToHashSet(StringComparer.Ordinal);
        var pokemonUsers = new Dictionary<int, HashSet<string>>();
        var moveCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var abilityCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int usersWithTeams = 0;

        foreach (var progression in progressions.Where(item => normalUsernames.Contains(item.Username)))
        {
            var loadouts = DeserializeList<PokemonLoadout>(progression.LatestLoadoutsJson);
            if (loadouts.Count > 0) usersWithTeams++;
            foreach (var loadout in loadouts)
            {
                if (!pokemonUsers.TryGetValue(loadout.PokemonId, out var owners))
                {
                    owners = new HashSet<string>(StringComparer.Ordinal);
                    pokemonUsers[loadout.PokemonId] = owners;
                }
                owners.Add(progression.Username);
                if (!string.IsNullOrWhiteSpace(loadout.ChosenAbility))
                    Increment(abilityCounts, loadout.ChosenAbility);
            }

            var preferences = Deserialize<MovePreferenceProfile>(progression.MovePreferencesJson);
            if (preferences != null)
            {
                foreach (var pair in preferences.MoveCounts)
                    moveCounts[pair.Key] = moveCounts.TryGetValue(pair.Key, out var current)
                        ? current + Math.Max(0, pair.Value)
                        : Math.Max(0, pair.Value);
            }
        }

        var winRateBuckets = new[] { 0, 0, 0, 0, 0 };
        int usersWithRoundData = 0;
        foreach (var run in runs.Where(run => normalUsernames.Contains(run.Username))
                     .GroupBy(run => run.Username)
                     .Select(group => group.OrderByDescending(run => run.Id).First()))
        {
            var performances = DeserializeList<RunRoundPerformance>(run.RoundPerformancesJson);
            if (performances.Count == 0) continue;
            usersWithRoundData++;
            double winRate = performances.Count(performance => performance.Cleared) * 100d / performances.Count;
            int bucket = winRate >= 76 ? 4 : winRate >= 51 ? 3 : winRate >= 26 ? 2 : winRate > 0 ? 1 : 0;
            winRateBuckets[bucket]++;
        }

        var pokemonBars = pokemonUsers.Select(pair => (
                Label: PokemonDatabase.All.TryGetValue(pair.Key, out var data) ? data.Name : $"#{pair.Key}",
                Count: pair.Value.Count))
            .OrderByDescending(item => item.Count).ThenBy(item => item.Label).Take(8);
        int teamDenominator = Math.Max(1, usersWithTeams);
        return new AdminAnalyticsSnapshot(
            BuildBars(pokemonBars, item => item.Count * 100d / teamDenominator),
            BuildBars(moveCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Take(8)
                .Select(pair => (
                    Label: MoveDatabase.All.TryGetValue(pair.Key, out var data) ? data.Name : pair.Key,
                    Count: pair.Value)),
                item => item.Count * 100d / Math.Max(1, moveCounts.Values.Sum())),
            BuildBars(abilityCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Take(8)
                .Select(pair => (Label: pair.Key, Count: pair.Value)),
                item => item.Count * 100d / Math.Max(1, abilityCounts.Values.Sum())),
            new[]
            {
                new AdminAnalyticsBar("0%", winRateBuckets[0], Share(winRateBuckets[0], usersWithRoundData), Fill(winRateBuckets[0], winRateBuckets)),
                new AdminAnalyticsBar("1–25%", winRateBuckets[1], Share(winRateBuckets[1], usersWithRoundData), Fill(winRateBuckets[1], winRateBuckets)),
                new AdminAnalyticsBar("26–50%", winRateBuckets[2], Share(winRateBuckets[2], usersWithRoundData), Fill(winRateBuckets[2], winRateBuckets)),
                new AdminAnalyticsBar("51–75%", winRateBuckets[3], Share(winRateBuckets[3], usersWithRoundData), Fill(winRateBuckets[3], winRateBuckets)),
                new AdminAnalyticsBar("76–100%", winRateBuckets[4], Share(winRateBuckets[4], usersWithRoundData), Fill(winRateBuckets[4], winRateBuckets))
            },
            usersWithTeams,
            usersWithRoundData);
    }

    private static IReadOnlyList<AdminAnalyticsBar> BuildBars(
        IEnumerable<(string Label, int Count)> values,
        Func<(string Label, int Count), double> share)
    {
        var materialized = values.ToList();
        int max = materialized.Count == 0 ? 1 : materialized.Max(item => item.Count);
        return materialized.Select(item => new AdminAnalyticsBar(
            item.Label, item.Count, share(item), item.Count * 100d / max)).ToList();
    }

    private static double Share(int count, int total) => total == 0 ? 0 : count * 100d / total;

    private static double Fill(int count, IReadOnlyList<int> values) =>
        count * 100d / Math.Max(1, values.Count == 0 ? 1 : values.Max());

    private static void Increment(Dictionary<string, int> counts, string key) =>
        counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;

    private static List<T> DeserializeList<T>(string json) => Deserialize<List<T>>(json) ?? new();

    private static T? Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions); }
        catch (JsonException) { return default; }
    }

    public async Task<AdminUserDetails?> LoadUserDetailsAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.user-details", async db =>
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Username == username.Trim());
            if (user == null) return null;

            var run = await db.PlayerRuns.AsNoTracking()
                .Where(candidate => candidate.Username == user.Username)
                .OrderByDescending(candidate => candidate.Id)
                .FirstOrDefaultAsync();
            var presets = await db.UserPresets.AsNoTracking()
                .Where(preset => preset.Username == user.Username)
                .OrderBy(preset => preset.Name)
                .Select(preset => new AdminPresetSnapshot(preset.Name, preset.UpdatedAt))
                .ToListAsync();
            var unlockedIds = await db.UnlockedPokemons.AsNoTracking()
                .Where(unlock => unlock.Username == user.Username)
                .Select(unlock => unlock.PokemonId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            var team = new List<AdminTeamMemberSnapshot>();
            var history = new List<LegendaryEncounterHistoryEntry>();
            if (run != null)
            {
                try
                {
                    var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(
                        run.LoadoutsJson, JsonOptions) ?? new();
                    team = loadouts
                        .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
                        .Select(loadout =>
                        {
                            var data = PokemonDatabase.All[loadout.PokemonId];
                            return new AdminTeamMemberSnapshot(
                                data.Name,
                                loadout.Level,
                                loadout.ChosenAbility,
                                loadout.ChosenItem,
                                loadout.ChosenMoveNames.ToList());
                        })
                        .ToList();
                    history = JsonSerializer.Deserialize<List<LegendaryEncounterHistoryEntry>>(
                        run.LegendaryEncounterHistoryJson, JsonOptions) ?? new();
                }
                catch (JsonException)
                {
                    // Integrity issues are reported by the dashboard; details stay safe to view.
                }
            }

            var unlockedNames = unlockedIds
                .Where(PokemonDatabase.All.ContainsKey)
                .Select(id => PokemonDatabase.All[id].Name)
                .ToList();

            return new AdminUserDetails(
                user.Username,
                user.IsAdmin,
                run?.CurrentScore ?? 0,
                run?.HighScore ?? 0,
                run?.LegendaryProgressPercent ?? 0,
                team,
                presets,
                unlockedNames,
                history);
        });

    public async Task<AdminOperationResult> SetAdminAsync(string username, bool isAdmin) =>
        await _database.ExecuteAsync("admin-dashboard.set-admin", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            var target = await db.Users.FirstOrDefaultAsync(user => user.Username == username);
            if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");
            if (!isAdmin && string.Equals(
                target.Username, _currentUser.Username, StringComparison.Ordinal))
                return Failure("현재 로그인한 관리자 계정의 권한은 해제할 수 없습니다.");
            if (target.IsAdmin == isAdmin)
                return Success(isAdmin ? "이미 관리자 권한이 있습니다." : "이미 일반 계정입니다.");
            if (!isAdmin && await db.Users.CountAsync(user => user.IsAdmin) <= 1)
                return Failure("마지막 관리자 권한은 해제할 수 없습니다.");

            target.IsAdmin = isAdmin;
            await db.SaveChangesAsync();
            return Success(isAdmin
                ? $"{username} 계정을 관리자로 지정했습니다."
                : $"{username} 계정의 관리자 권한을 해제했습니다.");
        });

    public async Task<AdminOperationResult> ResetPasswordAsync(
        string username,
        string newPassword) =>
        await _database.ExecuteAsync("admin-dashboard.reset-password", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (string.IsNullOrWhiteSpace(newPassword))
                return Failure("새 비밀번호를 입력해주세요.");

            var target = await db.Users.FirstOrDefaultAsync(user => user.Username == username);
            if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");
            target.PasswordHash = PasswordHasher.Hash(newPassword);
            await db.SaveChangesAsync();
            return Success($"{username} 계정의 비밀번호를 재설정했습니다.");
        });

    public async Task<AdminOperationResult> DeleteUserAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.delete-user", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (string.Equals(username, _currentUser.Username, StringComparison.Ordinal))
                return Failure("현재 로그인한 관리자 계정은 삭제할 수 없습니다.");

            var target = await db.Users.FirstOrDefaultAsync(user => user.Username == username);
            if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");

            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.UnlockedPokemons.Where(item => item.Username == username).ExecuteDeleteAsync();
            await db.PlayerRuns.Where(item => item.Username == username).ExecuteDeleteAsync();
            await db.PlayerSkillRatings.Where(item => item.Username == username).ExecuteDeleteAsync();
            await db.UserPresets.Where(item => item.Username == username).ExecuteDeleteAsync();
            db.Users.Remove(target);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Success($"{username} 계정과 저장된 게임 데이터를 삭제했습니다.");
        });

    public async Task<AdminOperationResult> UnlockAllPokemonAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.unlock-all", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");

            var existingIds = await db.UnlockedPokemons
                .Where(item => item.Username == username)
                .Select(item => item.PokemonId)
                .ToListAsync();
            var missingIds = PokemonDatabase.All.Keys
                .Except(existingIds)
                .Select(id => new UnlockedPokemon { Username = username, PokemonId = id })
                .ToList();
            if (missingIds.Count > 0)
            {
                db.UnlockedPokemons.AddRange(missingIds);
                await db.SaveChangesAsync();
            }
            return Success($"{username} 계정에 도감 포켓몬 {PokemonDatabase.All.Count}종을 해금했습니다.");
        });

    public async Task<AdminOperationResult> ResetUnlocksToStartersAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.reset-unlocks", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");

            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.UnlockedPokemons.Where(item => item.Username == username).ExecuteDeleteAsync();
            db.UnlockedPokemons.AddRange(
                new[] { 1, 4, 7 }.Select(id =>
                    new UnlockedPokemon { Username = username, PokemonId = id }));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Success($"{username} 계정의 해금을 스타터 3종으로 초기화했습니다.");
        });

    public async Task<AdminOperationResult> ResetRunAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.reset-run", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");
            var run = await GetOrCreateRunAsync(db, username);
            run.CurrentScore = 0;
            run.LoadoutsJson = "[]";
            run.DifficultyAdjustment = 0;
            run.RoundPerformancesJson = "[]";
            await db.SaveChangesAsync();
            return Success($"{username} 계정의 현재 런을 초기화했습니다.");
        });

    public async Task<AdminOperationResult> SetScoresAsync(
        string username,
        int currentScore,
        int highScore) =>
        await _database.ExecuteAsync("admin-dashboard.set-scores", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");
            var run = await GetOrCreateRunAsync(db, username);
            run.CurrentScore = Math.Max(0, currentScore);
            run.HighScore = Math.Max(0, highScore);
            await db.SaveChangesAsync();
            return Success($"{username} 계정의 현재 점수와 최고 기록을 저장했습니다.");
        });

    public async Task<AdminOperationResult> SetLegendaryProgressAsync(
        string username,
        int progressPercent) =>
        await _database.ExecuteAsync("admin-dashboard.set-progress", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");
            var run = await GetOrCreateRunAsync(db, username);
            run.LegendaryProgressPercent = Math.Clamp(
                progressPercent, 0, LegendaryProgression.MaxProgressPercent);
            await db.SaveChangesAsync();
            return Success(
                $"{username} 계정의 전설 진행도를 {run.LegendaryProgressPercent}%로 저장했습니다.");
        });

    public async Task<AdminOperationResult> ClearLegendaryHistoryAsync(string username) =>
        await _database.ExecuteAsync("admin-dashboard.clear-history", async db =>
        {
            if (await GetCurrentAdminAsync(db) == null) return Forbidden();
            if (!await UserExistsAsync(db, username)) return Failure("대상 계정을 찾을 수 없습니다.");
            var run = await GetOrCreateRunAsync(db, username);
            run.LegendaryEncounterHistoryJson = "[]";
            await db.SaveChangesAsync();
            return Success($"{username} 계정의 전설 출현 이력을 삭제했습니다.");
        });

    private async Task<UserAccount?> GetCurrentAdminAsync(AppDbContext db)
    {
        if (!_currentUser.IsLoggedIn) return null;
        return await db.Users.FirstOrDefaultAsync(user =>
            user.Username == _currentUser.Username && user.IsAdmin);
    }

    private static Task<bool> UserExistsAsync(AppDbContext db, string username) =>
        db.Users.AnyAsync(user => user.Username == username);

    private static async Task<PlayerRun> GetOrCreateRunAsync(
        AppDbContext db,
        string username)
    {
        var run = await db.PlayerRuns
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(item => item.Username == username);
        if (run != null) return run;

        run = new PlayerRun { Username = username };
        db.PlayerRuns.Add(run);
        return run;
    }

    private static AdminOperationResult Forbidden() => Failure("관리자 권한이 필요합니다.");
    private static AdminOperationResult Success(string message) => new(true, message);
    private static AdminOperationResult Failure(string message) => new(false, message);
}

public sealed record AdminOperationResult(bool Success, string Message);

public sealed record AdminDashboardSnapshot(
    DateTimeOffset GeneratedAtUtc,
    int UserCount,
    int AdminCount,
    int RunCount,
    int PresetCount,
    int UniqueUnlockedPokemonCount,
    int KnownPokemonCount,
    int KnownMoveCount,
    IReadOnlyList<AdminUserSnapshot> Users,
    IReadOnlyList<string> IntegrityIssues,
    IReadOnlyList<AdminAuditLogSnapshot> AuditLogs,
    AdminAnalyticsSnapshot Analytics);

public sealed record AdminAnalyticsSnapshot(
    IReadOnlyList<AdminAnalyticsBar> PokemonPopularity,
    IReadOnlyList<AdminAnalyticsBar> MovePopularity,
    IReadOnlyList<AdminAnalyticsBar> AbilityPopularity,
    IReadOnlyList<AdminAnalyticsBar> WinRateDistribution,
    int UsersWithTeams,
    int UsersWithRoundData);

public sealed record AdminAnalyticsBar(
    string Label,
    int Count,
    double SharePercent,
    double FillPercent);

public sealed record AdminUserSnapshot(
    string Username,
    bool IsAdmin,
    int CurrentScore,
    int HighScore,
    int LegendaryProgressPercent,
    int UnlockedCount,
    int PresetCount,
    int LegendaryEncounterCount);

public sealed record AdminUserDetails(
    string Username,
    bool IsAdmin,
    int CurrentScore,
    int HighScore,
    int LegendaryProgressPercent,
    IReadOnlyList<AdminTeamMemberSnapshot> CurrentTeam,
    IReadOnlyList<AdminPresetSnapshot> Presets,
    IReadOnlyList<string> UnlockedPokemonNames,
    IReadOnlyList<LegendaryEncounterHistoryEntry> LegendaryHistory);

public sealed record AdminTeamMemberSnapshot(
    string Name,
    int Level,
    string Ability,
    string Item,
    IReadOnlyList<string> Moves);

public sealed record AdminPresetSnapshot(string Name, DateTime UpdatedAt);

public sealed record AdminAuditLogSnapshot(
    string AdminUsername,
    string Action,
    string TargetUsername,
    string Details,
    DateTimeOffset CreatedAtUtc);