namespace PokemonBattle.Models;

// 현재 배틀의 날씨 상태. 필드 상태는 BattleField에서 별도로 관리한다.
public static class BattleWeather
{
    public const string Clear = "맑음";
    public const string Sun = "쾌청";
    public const string Rain = "비";
    public const string Sand = "모래바람";
    public const string Hail = "싸라기눈";

    private sealed class WeatherState
    {
        public string Current = Clear;
        public int TurnsRemaining;
    }

    private static readonly AsyncLocal<WeatherState?> state = new();
    private static WeatherState CurrentState => state.Value ??= new WeatherState();

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

    public static bool AreEffectsSuppressed(Pokemon? first, Pokemon? second) =>
        HasWeatherNullifier(first, second) || HasWeatherNullifier(second, first);

    public static void Reset()
    {
        state.Value = new WeatherState();
    }

    public static void Set(string weather, int turns = 0)
    {
        CurrentState.Current = weather;
        CurrentState.TurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        if (CurrentState.TurnsRemaining <= 0) return false;
        CurrentState.TurnsRemaining--;
        if (CurrentState.TurnsRemaining > 0) return false;

        CurrentState.Current = Clear;
        return true;
    }

    private static bool HasWeatherNullifier(Pokemon? pokemon, Pokemon? opponent) =>
        pokemon != null
        && !pokemon.IsFainted
        && (pokemon.HasActiveAbility("에어록", opponent)
            || pokemon.HasActiveAbility("날씨부정", opponent)
            || pokemon.HasActiveAbility("화학변화가스", opponent));
}
