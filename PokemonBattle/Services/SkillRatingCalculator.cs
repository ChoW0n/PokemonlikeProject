using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class SkillRatingCalculator
{
    public const double DefaultRating = 1000;
    public const double EmaAlpha = 0.25;
    public const int ExpectedClearedRounds = 5;
    public const double ExpectedTurnsPerRound = 10;
    public const int MinimumDifficultyAdjustment = -3;
    public const int MaximumDifficultyAdjustment = 5;

    public static double CalculatePerformanceScore(RunPerformanceSummary summary)
    {
        double clearScore = Math.Clamp(
            summary.ClearedRounds / (double)ExpectedClearedRounds,
            0,
            1);
        double hpScore = Math.Clamp(summary.AverageHpRatio, 0, 1);
        double efficiencyScore = summary.AverageTurns <= 0
            ? 1
            : Math.Clamp(
                ExpectedTurnsPerRound / summary.AverageTurns,
                0,
                1);
        double outcomeScore = summary.Won ? 1 : 0;

        //클리어 수를 중심으로 보되, 생존력·턴 효율과 승패도 함께 반영한다.
        double score = 0.45 * clearScore
            + 0.20 * hpScore
            + 0.15 * efficiencyScore
            + 0.20 * outcomeScore;

        //패배한 런은 직전 라운드까지의 성과가 좋아도 성공 런보다 낮게 평가한다.
        return Math.Clamp(summary.Won ? score : score * 0.75, 0, 1);
    }

    public static double UpdateRating(double currentRating, RunPerformanceSummary summary)
    {
        double safeCurrent = Math.Clamp(currentRating, 400, 2000);
        double targetRating = 700 + CalculatePerformanceScore(summary) * 600;
        return Math.Clamp(
            safeCurrent * (1 - EmaAlpha) + targetRating * EmaAlpha,
            400,
            2000);
    }

    public static int CalculateDifficultyAdjustment(double rating)
    {
        int adjustment = (int)Math.Floor((rating - DefaultRating) / 100);
        return Math.Clamp(
            adjustment,
            MinimumDifficultyAdjustment,
            MaximumDifficultyAdjustment);
    }
}