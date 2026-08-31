using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class AdminDashboardService
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true
    };

    public AdminDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardSnapshot> LoadAsync()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(user => user.Username).ToListAsync();
        var runs = await _db.PlayerRuns.AsNoTracking().ToListAsync();
        var presets = await _db.UserPresets.AsNoTracking().ToListAsync();
        var unlocks = await _db.UnlockedPokemons.AsNoTracking().ToListAsync();
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
            issues);
    }
}

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
    IReadOnlyList<string> IntegrityIssues);

public sealed record AdminUserSnapshot(
    string Username,
    bool IsAdmin,
    int CurrentScore,
    int HighScore,
    int LegendaryProgressPercent,
    int UnlockedCount,
    int PresetCount,
    int LegendaryEncounterCount);