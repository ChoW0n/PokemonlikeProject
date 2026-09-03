namespace PokemonBattle.Models;

/// <summary>
/// The mutable arena state owned by one battle session.
/// </summary>
public sealed class BattleEnvironment
{
    internal string Weather = BattleWeather.Clear;
    internal int WeatherTurnsRemaining;

    internal string Field = BattleField.None;
    internal int FieldTurnsRemaining;
    internal bool TrickRoomActive;
    internal int TrickRoomTurnsRemaining;
    internal bool GravityActive;
    internal int GravityTurnsRemaining;

    internal BattleEnvironment Clone() => new()
    {
        Weather = Weather,
        WeatherTurnsRemaining = WeatherTurnsRemaining,
        Field = Field,
        FieldTurnsRemaining = FieldTurnsRemaining,
        TrickRoomActive = TrickRoomActive,
        TrickRoomTurnsRemaining = TrickRoomTurnsRemaining,
        GravityActive = GravityActive,
        GravityTurnsRemaining = GravityTurnsRemaining
    };
}

/// <summary>
/// Provides the environment currently being evaluated by battle rules.
/// BattleEngine activates its own instance before every public operation.
/// </summary>
internal static class BattleEnvironmentContext
{
    private static readonly AsyncLocal<BattleEnvironment?> current = new();
    private static BattleEnvironment fallback = new();

    internal static BattleEnvironment Active => current.Value ?? fallback;

    internal static void Activate(BattleEnvironment environment)
    {
        fallback = environment;
        current.Value = environment;
    }
}