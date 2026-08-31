namespace PokemonBattle.Models;

// 현재 배틀의 날씨 상태. 필드 상태는 BattleField에서 별도로 관리한다.
public static class BattleWeather
{
    public const string Clear = "맑음";
    public const string Sun = "쾌청";
    public const string Rain = "비";
    public const string Sand = "모래바람";
    public const string Hail = "싸라기눈";

    public static string Current { get; set; } = Clear;

    public static void Reset() => Current = Clear;
}
