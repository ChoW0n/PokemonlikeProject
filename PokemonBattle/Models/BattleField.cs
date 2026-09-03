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

    private static BattleEnvironment CurrentEnvironment => BattleEnvironmentContext.Active;

    public static string Current
    {
        get => CurrentEnvironment.Field;
        set
        {
            CurrentEnvironment.Field = value;
            CurrentEnvironment.FieldTurnsRemaining = 0;
        }
    }

    public static int TurnsRemaining => CurrentEnvironment.FieldTurnsRemaining;
    public static bool TrickRoomActive => CurrentEnvironment.TrickRoomActive;
    public static int TrickRoomTurnsRemaining => CurrentEnvironment.TrickRoomTurnsRemaining;
    public static bool GravityActive => CurrentEnvironment.GravityActive;
    public static int GravityTurnsRemaining => CurrentEnvironment.GravityTurnsRemaining;

    public static void Reset()
    {
        CurrentEnvironment.Field = None;
        CurrentEnvironment.FieldTurnsRemaining = 0;
        CurrentEnvironment.TrickRoomActive = false;
        CurrentEnvironment.TrickRoomTurnsRemaining = 0;
        CurrentEnvironment.GravityActive = false;
        CurrentEnvironment.GravityTurnsRemaining = 0;
    }

    public static void Set(string field, int turns = 0)
    {
        CurrentEnvironment.Field = field;
        CurrentEnvironment.FieldTurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        bool expired = false;
        if (CurrentEnvironment.FieldTurnsRemaining > 0
            && --CurrentEnvironment.FieldTurnsRemaining == 0)
        {
            CurrentEnvironment.Field = None;
            expired = true;
        }
        if (CurrentEnvironment.TrickRoomTurnsRemaining > 0
            && --CurrentEnvironment.TrickRoomTurnsRemaining == 0)
            CurrentEnvironment.TrickRoomActive = false;
        if (CurrentEnvironment.GravityTurnsRemaining > 0
            && --CurrentEnvironment.GravityTurnsRemaining == 0)
            CurrentEnvironment.GravityActive = false;
        return expired;
    }

    public static void SetTrickRoom(int turns = 5)
    {
        CurrentEnvironment.TrickRoomActive = true;
        CurrentEnvironment.TrickRoomTurnsRemaining = Math.Max(1, turns);
    }

    public static void SetGravity(int turns = 5)
    {
        CurrentEnvironment.GravityActive = true;
        CurrentEnvironment.GravityTurnsRemaining = Math.Max(1, turns);
    }

    public static void ClearTrickRoom()
    {
        CurrentEnvironment.TrickRoomActive = false;
        CurrentEnvironment.TrickRoomTurnsRemaining = 0;
    }

    public static void ClearGravity()
    {
        CurrentEnvironment.GravityActive = false;
        CurrentEnvironment.GravityTurnsRemaining = 0;
    }
}