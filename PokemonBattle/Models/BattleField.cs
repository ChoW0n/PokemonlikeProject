namespace PokemonBattle.Models;

/// <summary>
/// The terrain currently affecting the battle arena.
/// Terrain is kept separate from weather because both can be active at once.
/// </summary>
public static class BattleField
{
    public const string None = "없음";
    public const string Grassy = "그래스필드";
    public const string Electric = "일렉트릭필드";
    public const string Psychic = "사이코필드";
    public const string Misty = "미스트필드";

    private static string current = None;
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
        current = None;
        TurnsRemaining = 0;
    }

    public static void Set(string field, int turns = 0)
    {
        current = field;
        TurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        if (TurnsRemaining <= 0) return false;
        TurnsRemaining--;
        if (TurnsRemaining > 0) return false;

        current = None;
        return true;
    }
}