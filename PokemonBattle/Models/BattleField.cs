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

    public static string Current { get; set; } = None;

    public static void Reset() => Current = None;
}