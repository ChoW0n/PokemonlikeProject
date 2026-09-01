using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class RunStore
{
    private readonly DatabaseContextExecutor _database;
    private static readonly JsonSerializerOptions LoadoutJsonOptions = new()
    {
        IncludeFields = true
    };

    [ActivatorUtilitiesConstructor]
    public RunStore(DatabaseContextExecutor database)
    {
        _database = database;
    }

    public RunStore(AppDbContext db) : this(new DatabaseContextExecutor(db))
    {
    }

    public async Task<(
        int score,
        int highScore,
        List<PokemonLoadout> loadouts,
        int legendaryProgressPercent,
        List<LegendaryEncounterHistoryEntry> legendaryEncounterHistory,
        int difficultyAdjustment,
        List<RunRoundPerformance> roundPerformances,
        RunMetaState metaState)> Load(string username)
    {
        return await _database.ExecuteAsync("run.load", async db =>
        {
            var run = await db.PlayerRuns.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Username == username);
            if (run == null)
            {
                return (
                    0,
                    0,
                    new List<PokemonLoadout>(),
                    0,
                    new List<LegendaryEncounterHistoryEntry>(),
                    0,
                    new List<RunRoundPerformance>(),
                    new RunMetaState());
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
            var metaState = JsonSerializer.Deserialize<RunMetaState>(
                run.RunMetaStateJson,
                LoadoutJsonOptions) ?? new RunMetaState();
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
                roundPerformances,
                RunMetaCatalog.Normalize(metaState));
        });
    }

    public async Task Save(
        string username,
        int score,
        int highScore,
        List<PokemonLoadout> loadouts,
        int legendaryProgressPercent,
        IReadOnlyList<LegendaryEncounterHistoryEntry>? legendaryEncounterHistory = null,
        int? difficultyAdjustment = null,
        IReadOnlyList<RunRoundPerformance>? roundPerformances = null,
        RunMetaState? metaState = null)
    {
        await _database.ExecuteAsync("run.save", async db =>
        {
            var run = await db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
            string json = JsonSerializer.Serialize(loadouts, LoadoutJsonOptions);
            string historyJson = JsonSerializer.Serialize(
                legendaryEncounterHistory ?? new List<LegendaryEncounterHistoryEntry>(),
                LoadoutJsonOptions);
            string roundPerformancesJson = JsonSerializer.Serialize(
                roundPerformances ?? new List<RunRoundPerformance>(),
                LoadoutJsonOptions);
            string metaStateJson = JsonSerializer.Serialize(
                RunMetaCatalog.Normalize(metaState),
                LoadoutJsonOptions);
            int safeHighScore = Math.Max(0, highScore);
            int safeProgress = Math.Clamp(legendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent);
            int safeDifficulty = Math.Clamp(
                difficultyAdjustment ?? 0,
                SkillRatingCalculator.MinimumDifficultyAdjustment,
                SkillRatingCalculator.MaximumDifficultyAdjustment);

            if (run == null)
            {
                db.PlayerRuns.Add(new PlayerRun
                {
                    Username = username,
                    CurrentScore = score,
                    HighScore = safeHighScore,
                    LoadoutsJson = json,
                    LegendaryProgressPercent = safeProgress,
                    LegendaryEncounterHistoryJson = historyJson,
                    DifficultyAdjustment = safeDifficulty,
                    RoundPerformancesJson = roundPerformancesJson,
                    RunMetaStateJson = metaStateJson
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
                if (metaState != null)
                {
                    run.RunMetaStateJson = metaStateJson;
                }
            }

            await db.SaveChangesAsync();
        });
    }
}