using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class AdminOperationsService
{
    private readonly DatabaseContextExecutor _database;
    private readonly CurrentUserService _currentUser;
    private static readonly int[] StarterIds = { 1, 4, 7 };
    [ActivatorUtilitiesConstructor]
    public AdminOperationsService(
        DatabaseContextExecutor database,
        CurrentUserService currentUser)
    {
        _database = database;
        _currentUser = currentUser;
    }

    public AdminOperationsService(AppDbContext db, CurrentUserService currentUser)
        : this(new DatabaseContextExecutor(db), currentUser)
    {
    }

    public async Task<AdminActionResult> ResetPasswordAsync(string targetUsername, string newPassword) =>
        await _database.ExecuteAsync("admin.reset-password", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return Failure("새 비밀번호는 4자 이상 입력해주세요.");

            target.PasswordHash = PasswordHasher.Hash(newPassword);
            return await SaveWithAuditAsync(
                db, "비밀번호 재설정", target.Username, "관리자가 새 비밀번호를 설정함");
        });

    public async Task<AdminActionResult> SetAdminRoleAsync(string targetUsername, bool isAdmin) =>
        await _database.ExecuteAsync("admin.set-role", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: false);
            if (target == null) return Failure("자기 자신의 권한은 이 화면에서 변경할 수 없습니다.");
            if (target.IsAdmin == isAdmin)
                return Failure(isAdmin ? "이미 관리자 계정입니다." : "이미 일반 사용자 계정입니다.");
            if (!isAdmin && await db.Users.CountAsync(user => user.IsAdmin) <= 1)
                return Failure("마지막 관리자 계정은 강등할 수 없습니다.");

            target.IsAdmin = isAdmin;
            return await SaveWithAuditAsync(
                db,
                isAdmin ? "관리자 권한 부여" : "관리자 권한 회수",
                target.Username,
                isAdmin ? "일반 계정을 관리자로 변경함" : "관리자 권한을 일반 사용자로 변경함");
        });

    public async Task<AdminActionResult> DeleteUserAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.delete-user", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: false);
            if (target == null) return Failure("자기 자신은 삭제할 수 없습니다.");
            if (target.IsAdmin && await db.Users.CountAsync(user => user.IsAdmin) <= 1)
                return Failure("마지막 관리자 계정은 삭제할 수 없습니다.");

            string username = target.Username;
            db.PlayerRuns.RemoveRange(db.PlayerRuns.Where(run => run.Username == username));
            db.UserPresets.RemoveRange(db.UserPresets.Where(preset => preset.Username == username));
            db.UnlockedPokemons.RemoveRange(db.UnlockedPokemons.Where(unlock => unlock.Username == username));
            db.Users.Remove(target);
            return await SaveWithAuditAsync(
                db, "계정 삭제", username, "계정과 연결된 런·프리셋·해금을 함께 삭제함");
        });

    public async Task<AdminActionResult> ResetRunAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.reset-run", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            db.PlayerRuns.RemoveRange(db.PlayerRuns.Where(run => run.Username == target.Username));
            return await SaveWithAuditAsync(
                db, "런 초기화", target.Username, "현재 점수·팀·전설 진행·이력을 초기화함");
        });

    public async Task<AdminActionResult> ResetProgressAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.reset-progress", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            string username = target.Username;
            db.PlayerRuns.RemoveRange(db.PlayerRuns.Where(run => run.Username == username));
            db.UserPresets.RemoveRange(db.UserPresets.Where(preset => preset.Username == username));
            db.UnlockedPokemons.RemoveRange(db.UnlockedPokemons.Where(unlock => unlock.Username == username));
            return await SaveWithAuditAsync(
                db, "전체 진행 초기화", username, "런·프리셋·포켓몬 해금을 모두 초기화함");
        });

    public async Task<AdminActionResult> GrantAllPokemonAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.grant-all-pokemon", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            string username = target.Username;
            var owned = (await db.UnlockedPokemons
                .Where(unlock => unlock.Username == username)
                .Select(unlock => unlock.PokemonId)
                .ToListAsync()).ToHashSet();
            foreach (int pokemonId in PokemonDatabase.All.Keys)
            {
                if (!owned.Contains(pokemonId))
                {
                    db.UnlockedPokemons.Add(new UnlockedPokemon
                    {
                        Username = username,
                        PokemonId = pokemonId
                    });
                }
            }

            return await SaveWithAuditAsync(
                db, "전체 포켓몬 해금", username, $"{PokemonDatabase.All.Count}종 해금 상태로 설정함");
        });

    public async Task<AdminActionResult> ResetUnlocksToStartersAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.reset-unlocks", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            string username = target.Username;
            var unlocks = await db.UnlockedPokemons
                .Where(unlock => unlock.Username == username)
                .ToListAsync();
            db.UnlockedPokemons.RemoveRange(
                unlocks.Where(unlock => !StarterIds.Contains(unlock.PokemonId)));

            var existingIds = unlocks.Select(unlock => unlock.PokemonId).ToHashSet();
            foreach (int starterId in StarterIds)
            {
                if (!existingIds.Contains(starterId) && PokemonDatabase.All.ContainsKey(starterId))
                {
                    db.UnlockedPokemons.Add(new UnlockedPokemon
                    {
                        Username = username,
                        PokemonId = starterId
                    });
                }
            }

            return await SaveWithAuditAsync(
                db, "스타터 해금 복원", username, "스타터만 남기도록 도감을 초기화함");
        });

    public async Task<AdminActionResult> SetLegendaryProgressAsync(
        string targetUsername,
        int progressPercent) =>
        await _database.ExecuteAsync("admin.set-legendary-progress", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            int safeProgress = Math.Clamp(progressPercent, 0, LegendaryProgression.MaxProgressPercent);
            var run = await GetOrCreateRunAsync(db, target.Username);
            run.LegendaryProgressPercent = safeProgress;
            return await SaveWithAuditAsync(
                db, "전설 진행률 변경", target.Username, $"{safeProgress}%로 설정함");
        });

    public async Task<AdminActionResult> ClearLegendaryHistoryAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.clear-legendary-history", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            var run = await GetOrCreateRunAsync(db, target.Username);
            run.LegendaryEncounterHistoryJson = "[]";
            return await SaveWithAuditAsync(
                db, "전설 출현 이력 삭제", target.Username, "저장된 전설 출현 이력을 모두 삭제함");
        });

    public async Task<AdminActionResult> SetScoresAsync(
        string targetUsername,
        int currentScore,
        int highScore) =>
        await _database.ExecuteAsync("admin.set-scores", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            var run = await GetOrCreateRunAsync(db, target.Username);
            run.CurrentScore = Math.Max(0, currentScore);
            run.HighScore = Math.Max(run.CurrentScore, Math.Max(0, highScore));
            return await SaveWithAuditAsync(
                db,
                "점수 변경",
                target.Username,
                $"현재 {run.CurrentScore}점 / 최고 {run.HighScore}점으로 설정함");
        });

    public async Task<AdminActionResult> InjectTestTeamAsync(string targetUsername) =>
        await _database.ExecuteAsync("admin.inject-test-team", async db =>
        {
            var target = await FindTargetAsync(db, targetUsername, allowSelf: true);
            if (target == null) return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");

            var testIds = new[] { 1, 4, 7, 25, 133, 150 };
            var team = testIds
                .Where(PokemonDatabase.All.ContainsKey)
                .Select(id =>
                {
                    var data = PokemonDatabase.All[id];
                    return new PokemonLoadout
                    {
                        PokemonId = id,
                        ChosenMoveNames = data.MoveNames.Take(4).ToList(),
                        ChosenAbility = data.AbilityNames.FirstOrDefault() ?? "",
                        ChosenItem = TeamLoadoutRules.NoItem,
                        Level = 100
                    };
                })
                .ToList();

            var run = await GetOrCreateRunAsync(db, target.Username);
            run.LoadoutsJson = LoadoutJson.Serialize(team);
            return await SaveWithAuditAsync(
                db, "테스트 팀 주입", target.Username, "대표 포켓몬 6마리를 레벨 100으로 설정함");
        });

    private static async Task<PlayerRun> GetOrCreateRunAsync(
        AppDbContext db,
        string username)
    {
        var run = await db.PlayerRuns
            .OrderByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate => candidate.Username == username);
        if (run != null) return run;

        run = new PlayerRun
        {
            Username = username,
            LoadoutsJson = "[]",
            LegendaryEncounterHistoryJson = "[]"
        };
        db.PlayerRuns.Add(run);
        return run;
    }

    private async Task<UserAccount?> FindTargetAsync(
        AppDbContext db,
        string targetUsername,
        bool allowSelf)
    {
        if (!_currentUser.IsLoggedIn
            || string.IsNullOrWhiteSpace(targetUsername)
            || !await db.Users.AnyAsync(user =>
                user.Username == _currentUser.Username && user.IsAdmin))
        {
            return null;
        }

        var target = await db.Users.FirstOrDefaultAsync(user => user.Username == targetUsername.Trim());
        if (target == null) return null;

        return allowSelf || !string.Equals(
            target.Username,
            _currentUser.Username,
            StringComparison.Ordinal)
            ? target
            : null;
    }

    private async Task<AdminActionResult> SaveWithAuditAsync(
        AppDbContext db,
        string action,
        string targetUsername,
        string details)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUsername = _currentUser.Username ?? "unknown",
            Action = action,
            TargetUsername = targetUsername,
            Details = details,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return new AdminActionResult(true, $"{action} 완료: {targetUsername}");
    }

    private static AdminActionResult Failure(string message) => new(false, message);
}

public sealed record AdminActionResult(bool Success, string Message);