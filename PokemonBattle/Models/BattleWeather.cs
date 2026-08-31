namespace PokemonBattle.Models;

// 현재 배틀의 날씨 상태. 필드 상태는 BattleField에서 별도로 관리한다.
public static class BattleWeather
{
    public const string Clear = "맑음";
    public const string Sun = "쾌청";
    public const string Rain = "비";
    public const string Sand = "모래바람";
    public const string Hail = "싸라기눈";

    private static string current = Clear;
    public static string Current
    {
        get => current;
        set
        {
            current = value;
            TurnsRemaining = 0;
        }
    }

    public static int TurnsRemaining { get; private set; }

    public static void Reset()
    {
        current = Clear;
        TurnsRemaining = 0;
    }

    public static void Set(string weather, int turns = 0)
    {
        current = weather;
        TurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        if (TurnsRemaining <= 0) return false;
        TurnsRemaining--;
        if (TurnsRemaining > 0) return false;

        current = Clear;
        return true;
    }
}
