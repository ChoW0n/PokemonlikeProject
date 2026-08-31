using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class RunStore
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions LoadoutJsonOptions = new()
    {
        IncludeFields = true
    };

    public RunStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(
        int score,
        int highScore,
        List<PokemonLoadout> loadouts,
        int legendaryProgressPercent,
        List<LegendaryEncounterHistoryEntry> legendaryEncounterHistory,
        int difficultyAdjustment,
        List<RunRoundPerformance> roundPerformances)> Load(string username)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        if (run == null)
        {
            return (
                0,
                0,
                new List<PokemonLoadout>(),
                0,
                new List<LegendaryEncounterHistoryEntry>(),
                0,
                new List<RunRoundPerformance>());
        }

        var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(
            run.LoadoutsJson,
            LoadoutJsonOptions) ?? new List<PokemonLoadout>();
        var legendaryEncounterHistory = JsonSerializer.Deserialize<List<LegendaryEncounterHistoryEntry>>(
            run.LegendaryEncounterHistoryJson,
            LoadoutJsonOptions) ?? new List<LegendaryEncounterHistoryEntry>();
        var roundPerformances = JsonSerializer.Deserialize<List<RunRoundPerformance>>(
            run.RoundPerformancesJson,
            LoadoutJsonOptions) ?? new List<RunRoundPerformance>();
        return (
            run.CurrentScore,
            Math.Max(0, run.HighScore),
            loadouts,
            Math.Clamp(run.LegendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent),
            legendaryEncounterHistory,
            Math.Clamp(
                run.DifficultyAdjustment,
                SkillRatingCalculator.MinimumDifficultyAdjustment,
                SkillRatingCalculator.MaximumDifficultyAdjustment),
            roundPerformances);
    }

    public async Task Save(
        string username,
        int score,
        int highScore,
        List<PokemonLoadout> loadouts,
        int legendaryProgressPercent,
        IReadOnlyList<LegendaryEncounterHistoryEntry>? legendaryEncounterHistory = null,
        int? difficultyAdjustment = null,
        IReadOnlyList<RunRoundPerformance>? roundPerformances = null)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        string json = JsonSerializer.Serialize(loadouts, LoadoutJsonOptions);
        string historyJson = JsonSerializer.Serialize(
            legendaryEncounterHistory ?? new List<LegendaryEncounterHistoryEntry>(),
            LoadoutJsonOptions);
        string roundPerformancesJson = JsonSerializer.Serialize(
            roundPerformances ?? new List<RunRoundPerformance>(),
            LoadoutJsonOptions);
        int safeHighScore = Math.Max(0, highScore);
        int safeProgress = Math.Clamp(legendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent);
        int safeDifficulty = Math.Clamp(
            difficultyAdjustment ?? 0,
            SkillRatingCalculator.MinimumDifficultyAdjustment,
            SkillRatingCalculator.MaximumDifficultyAdjustment);

        if (run == null)
        {
            _db.PlayerRuns.Add(new PlayerRun
            {
                Username = username,
                CurrentScore = score,
                HighScore = safeHighScore,
                LoadoutsJson = json,
                LegendaryProgressPercent = safeProgress,
                LegendaryEncounterHistoryJson = historyJson,
                DifficultyAdjustment = safeDifficulty,
                RoundPerformancesJson = roundPerformancesJson
            });
        }
        else
        {
            run.CurrentScore = score;
            run.HighScore = Math.Max(run.HighScore, safeHighScore);
            run.LoadoutsJson = json;
            run.LegendaryProgressPercent = safeProgress;
            if (legendaryEncounterHistory != null)
            {
                run.LegendaryEncounterHistoryJson = historyJson;
            }
            if (difficultyAdjustment != null)
            {
                run.DifficultyAdjustment = safeDifficulty;
            }
            if (roundPerformances != null)
            {
                run.RoundPerformancesJson = roundPerformancesJson;
            }
        }

        await _db.SaveChangesAsync();
    }
}