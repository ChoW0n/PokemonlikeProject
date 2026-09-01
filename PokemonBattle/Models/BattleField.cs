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

    private sealed class FieldState
    {
        public string Current = None;
        public int TurnsRemaining;
        public bool TrickRoomActive;
        public int TrickRoomTurnsRemaining;
        public bool GravityActive;
        public int GravityTurnsRemaining;
    }

    private static readonly AsyncLocal<FieldState?> state = new();
    private static FieldState CurrentState => state.Value ??= new FieldState();

    public static string Current
    {
        get => CurrentState.Current;
        set
        {
            CurrentState.Current = value;
            CurrentState.TurnsRemaining = 0;
        }
    }

    public static int TurnsRemaining => CurrentState.TurnsRemaining;
    public static bool TrickRoomActive => CurrentState.TrickRoomActive;
    public static int TrickRoomTurnsRemaining => CurrentState.TrickRoomTurnsRemaining;
    public static bool GravityActive => CurrentState.GravityActive;
    public static int GravityTurnsRemaining => CurrentState.GravityTurnsRemaining;

    public static void Reset()
    {
        state.Value = new FieldState();
    }

    public static void Set(string field, int turns = 0)
    {
        CurrentState.Current = field;
        CurrentState.TurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        bool expired = false;
        if (CurrentState.TurnsRemaining > 0 && --CurrentState.TurnsRemaining == 0)
        {
            CurrentState.Current = None;
            expired = true;
        }
        if (CurrentState.TrickRoomTurnsRemaining > 0 && --CurrentState.TrickRoomTurnsRemaining == 0)
            CurrentState.TrickRoomActive = false;
        if (CurrentState.GravityTurnsRemaining > 0 && --CurrentState.GravityTurnsRemaining == 0)
            CurrentState.GravityActive = false;
        return expired;
    }

    public static void SetTrickRoom(int turns = 5)
    {
        CurrentState.TrickRoomActive = true;
        CurrentState.TrickRoomTurnsRemaining = Math.Max(1, turns);
    }

    public static void SetGravity(int turns = 5)
    {
        CurrentState.GravityActive = true;
        CurrentState.GravityTurnsRemaining = Math.Max(1, turns);
    }

    public static void ClearTrickRoom()
    {
        CurrentState.TrickRoomActive = false;
        CurrentState.TrickRoomTurnsRemaining = 0;
    }

    public static void ClearGravity()
    {
        CurrentState.GravityActive = false;
        CurrentState.GravityTurnsRemaining = 0;
    }
}