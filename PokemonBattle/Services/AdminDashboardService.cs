using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class AdminDashboardService
{
    private readonly AppDbContext _db;
    private readonly CurrentUserService _currentUser;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    private static readonly int[] StarterIds = { 1, 4, 7 };

    public AdminDashboardService(AppDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<bool> IsCurrentUserAdminAsync()
    {
        return await GetCurrentAdminAsync() != null;
    }

    public async Task<AdminDashboardSnapshot?> LoadAsync()
    {
        if (await GetCurrentAdminAsync() == null) return null;

        var users = await _db.Users.AsNoTracking().OrderBy(user => user.Username).ToListAsync();
        var runs = await _db.PlayerRuns.AsNoTracking().ToListAsync();
        var presets = await _db.UserPresets.AsNoTracking().ToListAsync();
        var unlocks = await _db.UnlockedPokemons.AsNoTracking().ToListAsync();
        var auditLogs = await _db.AdminAuditLogs
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
        {
            issues.Add($"{duplicateRun.Key}: 진행 런 레코드가 {duplicateRun.Count()}개입니다.");
        }

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
                        run.LoadoutsJson,
                        JsonOptions) ?? new List<PokemonLoadout>();
                    history = JsonSerializer.Deserialize<List<LegendaryEncounterHistoryEntry>>(
                        run.LegendaryEncounterHistoryJson,
                        JsonOptions) ?? new List<LegendaryEncounterHistoryEntry>();
                }
                catch (JsonException)
                {
                    issues.Add($"{user.Username}: 런 JSON을 읽을 수 없습니다.");
                }

                var invalidLoadouts = loadouts.Count(loadout => !PokemonDatabase.All.ContainsKey(loadout.PokemonId));
                if (invalidLoadouts > 0)
                {
                    issues.Add($"{user.Username}: 알 수 없는 포켓몬이 포함된 로드아웃 {invalidLoadouts}개가 있습니다.");
                }

                var invalidHistoryIds = history
                    .SelectMany(entry => entry.PokemonIds)
                    .Count(id => !PokemonDatabase.All.ContainsKey(id));
                if (invalidHistoryIds > 0)
                {
                    issues.Add($"{user.Username}: 이력에 알 수 없는 포켓몬 번호가 {invalidHistoryIds}개 있습니다.");
                }

                if (run.LegendaryProgressPercent is < 0 or > LegendaryProgression.MaxProgressPercent)
                {
                    issues.Add($"{user.Username}: 전설 진행도가 범위를 벗어났습니다.");
                }
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
            auditLogs);
    }

    public async Task<AdminUserDetails?> LoadUserDetailsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Username == username.Trim());
        if (user == null)
        {
            return null;
        }

        var run = await _db.PlayerRuns
            .AsNoTracking()
            .Where(candidate => candidate.Username == user.Username)
            .OrderByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync();
        var presets = await _db.UserPresets
            .AsNoTracking()
            .Where(preset => preset.Username == user.Username)
            .OrderBy(preset => preset.Name)
            .Select(preset => new AdminPresetSnapshot(preset.Name, preset.UpdatedAt))
            .ToListAsync();
        var unlockedIds = await _db.UnlockedPokemons
            .AsNoTracking()
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
                var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(run.LoadoutsJson, JsonOptions) ?? new();
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
                    run.LegendaryEncounterHistoryJson,
                    JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                // The dashboard integrity panel reports malformed JSON; details remain safe to view.
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
    }

    public async Task<AdminOperationResult> SetAdminAsync(string username, bool isAdmin)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();

        var target = await _db.Users.FirstOrDefaultAsync(user => user.Username == username);
        if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");

        if (!isAdmin && string.Equals(target.Username, _currentUser.Username, StringComparison.Ordinal))
        {
            return Failure("현재 로그인한 관리자 계정의 권한은 해제할 수 없습니다.");
        }

        if (target.IsAdmin == isAdmin)
        {
            return Success(isAdmin ? "이미 관리자 권한이 있습니다." : "이미 일반 계정입니다.");
        }

        if (!isAdmin && await _db.Users.CountAsync(user => user.IsAdmin) <= 1)
        {
            return Failure("마지막 관리자 권한은 해제할 수 없습니다.");
        }

        target.IsAdmin = isAdmin;
        await _db.SaveChangesAsync();
        return Success(isAdmin ? $"{username} 계정을 관리자로 지정했습니다." : $"{username} 계정의 관리자 권한을 해제했습니다.");
    }

    public async Task<AdminOperationResult> ResetPasswordAsync(string username, string newPassword)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return Failure("새 비밀번호를 입력해주세요.");
        }

        var target = await _db.Users.FirstOrDefaultAsync(user => user.Username == username);
        if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");

        target.PasswordHash = PasswordHasher.Hash(newPassword);
        await _db.SaveChangesAsync();
        return Success($"{username} 계정의 비밀번호를 재설정했습니다.");
    }

    public async Task<AdminOperationResult> DeleteUserAsync(string username)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (string.Equals(username, _currentUser.Username, StringComparison.Ordinal))
        {
            return Failure("현재 로그인한 관리자 계정은 삭제할 수 없습니다.");
        }

        var target = await _db.Users.FirstOrDefaultAsync(user => user.Username == username);
        if (target == null) return Failure("대상 계정을 찾을 수 없습니다.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.UnlockedPokemons
            .Where(item => item.Username == username)
            .ExecuteDeleteAsync();
        await _db.PlayerRuns
            .Where(item => item.Username == username)
            .ExecuteDeleteAsync();
        await _db.UserPresets
            .Where(item => item.Username == username)
            .ExecuteDeleteAsync();
        _db.Users.Remove(target);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Success($"{username} 계정과 저장된 게임 데이터를 삭제했습니다.");
    }

    public async Task<AdminOperationResult> UnlockAllPokemonAsync(string username)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        var existingIds = await _db.UnlockedPokemons
            .Where(item => item.Username == username)
            .Select(item => item.PokemonId)
            .ToListAsync();
        var missingIds = PokemonDatabase.All.Keys
            .Except(existingIds)
            .Select(id => new UnlockedPokemon { Username = username, PokemonId = id })
            .ToList();

        if (missingIds.Count > 0)
        {
            _db.UnlockedPokemons.AddRange(missingIds);
            await _db.SaveChangesAsync();
        }

        return Success($"{username} 계정에 도감 포켓몬 {PokemonDatabase.All.Count}종을 해금했습니다.");
    }

    public async Task<AdminOperationResult> ResetUnlocksToStartersAsync(string username)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.UnlockedPokemons
            .Where(item => item.Username == username)
            .ExecuteDeleteAsync();
        _db.UnlockedPokemons.AddRange(
            StarterIds.Select(id => new UnlockedPokemon { Username = username, PokemonId = id }));
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Success($"{username} 계정의 해금을 스타터 3종으로 초기화했습니다.");
    }

    public async Task<AdminOperationResult> ResetRunAsync(string username)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        var run = await GetOrCreateRunAsync(username);
        run.CurrentScore = 0;
        run.LoadoutsJson = "[]";
        await _db.SaveChangesAsync();
        return Success($"{username} 계정의 현재 런을 초기화했습니다.");
    }

    public async Task<AdminOperationResult> SetScoresAsync(string username, int currentScore, int highScore)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        var run = await GetOrCreateRunAsync(username);
        run.CurrentScore = Math.Max(0, currentScore);
        run.HighScore = Math.Max(0, highScore);
        await _db.SaveChangesAsync();
        return Success($"{username} 계정의 현재 점수와 최고 기록을 저장했습니다.");
    }

    public async Task<AdminOperationResult> SetLegendaryProgressAsync(string username, int progressPercent)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        var run = await GetOrCreateRunAsync(username);
        run.LegendaryProgressPercent = Math.Clamp(
            progressPercent,
            0,
            LegendaryProgression.MaxProgressPercent);
        await _db.SaveChangesAsync();
        return Success($"{username} 계정의 전설 진행도를 {run.LegendaryProgressPercent}%로 저장했습니다.");
    }

    public async Task<AdminOperationResult> ClearLegendaryHistoryAsync(string username)
    {
        if (await GetCurrentAdminAsync() == null) return Forbidden();
        if (!await UserExistsAsync(username)) return Failure("대상 계정을 찾을 수 없습니다.");

        var run = await GetOrCreateRunAsync(username);
        run.LegendaryEncounterHistoryJson = "[]";
        await _db.SaveChangesAsync();
        return Success($"{username} 계정의 전설 출현 이력을 삭제했습니다.");
    }

    private async Task<UserAccount?> GetCurrentAdminAsync()
    {
        if (!_currentUser.IsLoggedIn) return null;
        return await _db.Users.FirstOrDefaultAsync(user =>
            user.Username == _currentUser.Username && user.IsAdmin);
    }

    private Task<bool> UserExistsAsync(string username) =>
        _db.Users.AnyAsync(user => user.Username == username);

    private async Task<PlayerRun> GetOrCreateRunAsync(string username)
    {
        var run = await _db.PlayerRuns
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(item => item.Username == username);
        if (run != null) return run;

        run = new PlayerRun { Username = username };
        _db.PlayerRuns.Add(run);
        return run;
    }

    private static AdminOperationResult Forbidden() =>
        Failure("관리자 권한이 필요합니다.");

    private static AdminOperationResult Success(string message) =>
        new(true, message);

    private static AdminOperationResult Failure(string message) =>
        new(false, message);
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
    IReadOnlyList<AdminAuditLogSnapshot> AuditLogs);

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