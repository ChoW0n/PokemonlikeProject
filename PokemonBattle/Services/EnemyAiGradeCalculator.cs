namespace PokemonBattle.Services;

public static class EnemyAiGradeCalculator
{
    public static int Calculate(int round, double rating)
    {
        int safeRound = Math.Max(round, 1);
        double clampedRating = double.IsFinite(rating)
            ? Math.Clamp(rating, 400, 2000)
            : 1000;
        double difficultyAdjustment = Math.Clamp(
            (clampedRating - 1000) / 200,
            -3,
            5);

        // 라운드와 레이팅 압력을 합쳐 AI 등급을 계산한다.
        double roundPressure = Math.Clamp((safeRound - 1) / 14.0, 0, 1) * 0.75;
        double ratingPressure = Math.Clamp(
            (difficultyAdjustment + 3) / 8,
            0,
            1) * 0.25;
        double pressure = Math.Clamp(roundPressure + ratingPressure, 0, 1);

        return pressure < 0.3
            ? 0
            : pressure < 0.55
                ? 1
                : pressure < 0.8
                    ? 2
                    : 3;
    }
}