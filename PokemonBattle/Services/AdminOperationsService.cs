using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class AdminOperationsService
{
    private readonly AppDbContext _db;
    private readonly CurrentUserService _currentUser;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    public AdminOperationsService(AppDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AdminActionResult> ResetPasswordAsync(string targetUsername, string newPassword)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
        {
            return Failure("새 비밀번호는 4자 이상 입력해주세요.");
        }

        target.PasswordHash = PasswordHasher.Hash(newPassword);
        return await SaveWithAuditAsync("비밀번호 재설정", target.Username, "관리자가 새 비밀번호를 설정함");
    }

    public async Task<AdminActionResult> SetAdminRoleAsync(string targetUsername, bool isAdmin)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: false);
        if (target == null)
        {
            return Failure("자기 자신의 권한은 이 화면에서 변경할 수 없습니다.");
        }

        if (target.IsAdmin == isAdmin)
        {
            return Failure(isAdmin ? "이미 관리자 계정입니다." : "이미 일반 사용자 계정입니다.");
        }

        if (!isAdmin && await _db.Users.CountAsync(user => user.IsAdmin) <= 1)
        {
            return Failure("마지막 관리자 계정은 강등할 수 없습니다.");
        }

        target.IsAdmin = isAdmin;
        return await SaveWithAuditAsync(
            isAdmin ? "관리자 권한 부여" : "관리자 권한 회수",
            target.Username,
            isAdmin ? "일반 계정을 관리자로 변경함" : "관리자 권한을 일반 사용자로 변경함");
    }

    public async Task<AdminActionResult> DeleteUserAsync(string targetUsername)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: false);
        if (target == null)
        {
            return Failure("자기 자신은 삭제할 수 없습니다.");
        }

        if (target.IsAdmin && await _db.Users.CountAsync(user => user.IsAdmin) <= 1)
        {
            return Failure("마지막 관리자 계정은 삭제할 수 없습니다.");
        }

        string username = target.Username;
        _db.PlayerRuns.RemoveRange(_db.PlayerRuns.Where(run => run.Username == username));
        _db.UserPresets.RemoveRange(_db.UserPresets.Where(preset => preset.Username == username));
        _db.UnlockedPokemons.RemoveRange(_db.UnlockedPokemons.Where(unlock => unlock.Username == username));
        _db.Users.Remove(target);

        return await SaveWithAuditAsync("계정 삭제", username, "계정과 연결된 런·프리셋·해금을 함께 삭제함");
    }

    public async Task<AdminActionResult> ResetRunAsync(string targetUsername)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        _db.PlayerRuns.RemoveRange(_db.PlayerRuns.Where(run => run.Username == target.Username));
        return await SaveWithAuditAsync("런 초기화", target.Username, "현재 점수·팀·전설 진행·이력을 초기화함");
    }

    public async Task<AdminActionResult> ResetProgressAsync(string targetUsername)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        string username = target.Username;
        _db.PlayerRuns.RemoveRange(_db.PlayerRuns.Where(run => run.Username == username));
        _db.UserPresets.RemoveRange(_db.UserPresets.Where(preset => preset.Username == username));
        _db.UnlockedPokemons.RemoveRange(_db.UnlockedPokemons.Where(unlock => unlock.Username == username));
        return await SaveWithAuditAsync("전체 진행 초기화", username, "런·프리셋·포켓몬 해금을 모두 초기화함");
    }

    public async Task<AdminActionResult> GrantAllPokemonAsync(string targetUsername)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        string username = target.Username;
        var owned = await _db.UnlockedPokemons
            .Where(unlock => unlock.Username == username)
            .Select(unlock => unlock.PokemonId)
            .ToHashSetAsync();

        foreach (int pokemonId in PokemonDatabase.All.Keys)
        {
            if (!owned.Contains(pokemonId))
            {
                _db.UnlockedPokemons.Add(new UnlockedPokemon
                {
                    Username = username,
                    PokemonId = pokemonId
                });
            }
        }

        return await SaveWithAuditAsync("전체 포켓몬 해금", username, $"{PokemonDatabase.All.Count}종 해금 상태로 설정함");
    }

    public async Task<AdminActionResult> SetLegendaryProgressAsync(string targetUsername, int progressPercent)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        int safeProgress = Math.Clamp(progressPercent, 0, LegendaryProgression.MaxProgressPercent);
        var run = await GetOrCreateRunAsync(target.Username);
        run.LegendaryProgressPercent = safeProgress;
        return await SaveWithAuditAsync("전설 진행률 변경", target.Username, $"{safeProgress}%로 설정함");
    }

    public async Task<AdminActionResult> SetScoresAsync(string targetUsername, int currentScore, int highScore)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

        var run = await GetOrCreateRunAsync(target.Username);
        run.CurrentScore = Math.Max(0, currentScore);
        run.HighScore = Math.Max(run.CurrentScore, Math.Max(0, highScore));
        return await SaveWithAuditAsync("점수 변경", target.Username, $"현재 {run.CurrentScore}점 / 최고 {run.HighScore}점으로 설정함");
    }

    public async Task<AdminActionResult> InjectTestTeamAsync(string targetUsername)
    {
        var target = await FindTargetAsync(targetUsername, allowSelf: true);
        if (target == null)
        {
            return Failure("대상 계정을 찾을 수 없거나 작업 권한이 없습니다.");
        }

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

        var run = await GetOrCreateRunAsync(target.Username);
        run.LoadoutsJson = JsonSerializer.Serialize(team, JsonOptions);
        return await SaveWithAuditAsync("테스트 팀 주입", target.Username, "대표 포켓몬 6마리를 레벨 100으로 설정함");
    }

    private async Task<PlayerRun> GetOrCreateRunAsync(string username)
    {
        var run = await _db.PlayerRuns
            .OrderByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate => candidate.Username == username);
        if (run != null)
        {
            return run;
        }

        run = new PlayerRun
        {
            Username = username,
            LoadoutsJson = "[]",
            LegendaryEncounterHistoryJson = "[]"
        };
        _db.PlayerRuns.Add(run);
        return run;
    }

    private async Task<UserAccount?> FindTargetAsync(string targetUsername, bool allowSelf)
    {
        if (!_currentUser.IsAdmin || string.IsNullOrWhiteSpace(targetUsername))
        {
            return null;
        }

        var target = await _db.Users.FirstOrDefaultAsync(user => user.Username == targetUsername.Trim());
        if (target == null)
        {
            return null;
        }

        return allowSelf || !string.Equals(target.Username, _currentUser.Username, StringComparison.Ordinal)
            ? target
            : null;
    }

    private async Task<AdminActionResult> SaveWithAuditAsync(string action, string targetUsername, string details)
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUsername = _currentUser.Username ?? "unknown",
            Action = action,
            TargetUsername = targetUsername,
            Details = details,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();
        return new AdminActionResult(true, $"{action} 완료: {targetUsername}");
    }

    private static AdminActionResult Failure(string message) => new(false, message);
}

public sealed record AdminActionResult(bool Success, string Message);