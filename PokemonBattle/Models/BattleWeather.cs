namespace PokemonBattle.Models;

// 현재 배틀의 날씨 상태. 필드 상태는 BattleField에서 별도로 관리한다.
public static class BattleWeather
{
    public const string Clear = "맑음";
    public const string Sun = "쾌청";
    public const string Rain = "비";
    public const string Sand = "모래바람";
    public const string Hail = "싸라기눈";

    private static BattleEnvironment CurrentEnvironment => BattleEnvironmentContext.Active;

    public static string Current
    {
        get => CurrentEnvironment.Weather;
        set
        {
            CurrentEnvironment.Weather = value;
            CurrentEnvironment.WeatherTurnsRemaining = 0;
        }
    }

    public static int TurnsRemaining => CurrentEnvironment.WeatherTurnsRemaining;

    public static bool AreEffectsSuppressed(Pokemon? first, Pokemon? second) =>
        HasWeatherNullifier(first, second) || HasWeatherNullifier(second, first);

    public static void Reset()
    {
        CurrentEnvironment.Weather = Clear;
        CurrentEnvironment.WeatherTurnsRemaining = 0;
    }

    public static void Set(string weather, int turns = 0)
    {
        CurrentEnvironment.Weather = weather;
        CurrentEnvironment.WeatherTurnsRemaining = Math.Max(0, turns);
    }

    public static bool AdvanceTurn()
    {
        if (CurrentEnvironment.WeatherTurnsRemaining <= 0) return false;
        CurrentEnvironment.WeatherTurnsRemaining--;
        if (CurrentEnvironment.WeatherTurnsRemaining > 0) return false;

        CurrentEnvironment.Weather = Clear;
        return true;
    }

    private static bool HasWeatherNullifier(Pokemon? pokemon, Pokemon? opponent) =>
        pokemon != null
        && !pokemon.IsFainted
        && (pokemon.HasActiveAbility("에어록", opponent)
            || pokemon.HasActiveAbility("날씨부정", opponent)
            || pokemon.HasActiveAbility("화학변화가스", opponent));
}
