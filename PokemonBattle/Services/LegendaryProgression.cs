using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class LegendaryProgression
{
    public const int MaxProgressPercent = 100;

    public readonly record struct EncounterConsumption(
        int ProgressPercent,
        bool WasConsumed);

    public static bool IsUnlocked(int progressPercent) =>
        progressPercent >= MaxProgressPercent;

    public static int CalculateReward(int stage, IReadOnlyCollection<PokemonLoadout> enemyLoadouts)
    {
        int safeStage = Math.Max(1, stage);
        int teamDifficulty = Math.Max(0, enemyLoadouts.Count - 1);
        int stageDifficulty = 1 + (safeStage - 1) / 3;

        var baseStatTotals = enemyLoadouts
            .Where(loadout => PokemonDatabase.All.ContainsKey(loadout.PokemonId))
            .Select(loadout =>
            {
                var data = PokemonDatabase.All[loadout.PokemonId];
                return data.BaseHp + data.BaseAtk + data.BaseDef
                    + data.BaseSpAtk + data.BaseSpDef + data.BaseSpd;
            })
            .ToList();

        double averageBaseStats = baseStatTotals.Count == 0 ? 360 : baseStatTotals.Average();
        int statDifficulty = Math.Clamp((int)Math.Ceiling(averageBaseStats / 120.0), 1, 6);

        //스테이지, 상대 팀 규모, 평균 종족값이 높을수록 보상이 커진다.
        return Math.Clamp(stageDifficulty + teamDifficulty + statDifficulty, 1, 25);
    }

    public static int AddProgress(int currentProgressPercent, int rewardPercent) =>
        Math.Clamp(currentProgressPercent + Math.Max(0, rewardPercent), 0, MaxProgressPercent);

    public static EncounterConsumption ConsumeEncounter(
        int currentProgressPercent,
        bool containsLegendary,
        bool alreadyConsumed)
    {
        int safeProgress = Math.Clamp(currentProgressPercent, 0, MaxProgressPercent);

        if (!containsLegendary || !IsUnlocked(safeProgress) || alreadyConsumed)
        {
            return new EncounterConsumption(safeProgress, false);
        }

        return new EncounterConsumption(0, true);
    }
}