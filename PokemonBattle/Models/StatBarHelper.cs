namespace PokemonBattle.Models;

//능력치/기술 수치를 게이지(퍼센트+색상)로 변환하는 헬퍼
public static class StatBarHelper
{
    public static int StatPercent(int value) => Math.Clamp((int)(100.0 * value / 180), 4, 100);
    public static string StatColor(int value) =>
        value < 50 ? "gauge-vlow" : value < 80 ? "gauge-low" : value < 100 ? "gauge-mid" : value < 120 ? "gauge-high" : "gauge-vhigh";

    public static int PowerPercent(int power) => Math.Clamp((int)(100.0 * power / 150), 4, 100);
    public static string PowerColor(int power) =>
        power < 40 ? "gauge-low" : power < 70 ? "gauge-mid" : power < 100 ? "gauge-high" : "gauge-vhigh";

    public static int AccuracyPercent(int accuracy) => Math.Clamp(accuracy, 4, 100);
    public static string AccuracyColor(int accuracy) =>
        accuracy >= 95 ? "gauge-vhigh" : accuracy >= 80 ? "gauge-high" : accuracy >= 60 ? "gauge-mid" : "gauge-low";

    public static int PpPercent(int current, int max) => max == 0 ? 0 : Math.Clamp((int)(100.0 * current / max), 0, 100);
    public static string PpColor(int current, int max) =>
        max == 0 ? "gauge-low" : (double)current / max <= 0.25 ? "gauge-low" : (double)current / max <= 0.5 ? "gauge-mid" : "gauge-vhigh";
}
