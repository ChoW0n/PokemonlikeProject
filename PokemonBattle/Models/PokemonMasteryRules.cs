namespace PokemonBattle.Models;

public static class PokemonMasteryRules
{
    public static readonly IReadOnlyList<int> TierThresholds = [5, 15, 30];

    public const int MaximumTier = 3;
    public const int BonusPercentPerTier = 1;

    public static int GetTier(int victoryContributions)
    {
        int wins = Math.Max(0, victoryContributions);
        return TierThresholds.Count(threshold => wins >= threshold);
    }

    public static int GetBonusPercent(int victoryContributions) =>
        GetTier(victoryContributions) * BonusPercentPerTier;

    public static int GetNextThreshold(int victoryContributions)
    {
        int wins = Math.Max(0, victoryContributions);
        return TierThresholds.FirstOrDefault(threshold => wins < threshold);
    }

    public static int GetWinsToNextTier(int victoryContributions)
    {
        int nextThreshold = GetNextThreshold(victoryContributions);
        return nextThreshold == 0
            ? 0
            : Math.Max(0, nextThreshold - Math.Max(0, victoryContributions));
    }

    public static string GetTierLabel(int victoryContributions)
    {
        int tier = GetTier(victoryContributions);
        return tier == 0 ? "견습" : $"숙련 {tier}단계";
    }
}