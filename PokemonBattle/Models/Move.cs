namespace PokemonBattle.Models;

public class StatChangeEntry
{
    public string Stat = "";
    public int Change;
    public bool TargetsSelf;
}

public class Move
{
    public string Name;
    public int Power;
    public PokemonType Type;
    public int MaxPP;
    public int Accuracy;
    public bool AlwaysHits;
    public int Priority;
    public bool IsStatus;
    public bool IsSpecial;
    public string AilmentName;
    public int AilmentChance;
    public int FlinchChance;
    public List<StatChangeEntry> StatChanges = new();
    public int StatChangeChance;
    public string Description;
    public int HealingPercent;
    public int DrainPercent;
    public int MinHits;
    public int MaxHits;

    public Move(string name, int power, PokemonType type, int maxPp, int accuracy, bool alwaysHits, int priority, bool isStatus, bool isSpecial, string ailmentName, int ailmentChance, int flinchChance, List<StatChangeEntry> statChanges, int statChangeChance, string description, int healingPercent, int drainPercent, int minHits, int maxHits)
    {
        Name = name; Power = power; Type = type; MaxPP = maxPp; Accuracy = accuracy;
        AlwaysHits = alwaysHits; Priority = priority; IsStatus = isStatus; IsSpecial = isSpecial;
        AilmentName = ailmentName; AilmentChance = ailmentChance; FlinchChance = flinchChance;
        StatChanges = statChanges; StatChangeChance = statChangeChance; Description = description;
        HealingPercent = healingPercent; DrainPercent = drainPercent;
        MinHits = minHits; MaxHits = maxHits;
    }
}

public static class MoveRuleMetadata
{
    // PokeAPI's move payload does not expose the main-series contact flag.
    // Keep the smaller exception list here so generated move constructors stay stable.
    private static readonly HashSet<string> NonContactPhysicalMoves = new()
    {
        "razor-leaf", "seed-bomb", "petal-blizzard", "poison-sting", "twineedle",
        "pin-missile", "gunk-shot", "earthquake", "sand-tomb", "bulldoze", "pay-day",
        "psycho-cut", "self-destruct", "explosion", "rock-slide", "rock-blast",
        "stone-edge", "smack-down", "magnet-bomb", "ice-shard", "icicle-spear",
        "spike-cannon", "rock-tomb", "barrage", "bullet-seed", "egg-bomb", "bone-club",
        "bonemerang", "bone-rush", "sacred-fire", "leafage", "precipice-blades",
        "attack-order", "rock-wrecker", "aqua-cutter", "thousand-arrows",
        "thousand-waves", "lands-wrath", "diamond-storm", "fusion-bolt", "secret-power"
    };

    private static readonly HashSet<string> HighCriticalRateMoves = new()
    {
        "razor-leaf", "slash", "shadow-claw", "razor-wind", "sky-attack", "drill-run",
        "air-cutter", "cross-poison", "night-slash", "karate-chop", "cross-chop",
        "psycho-cut", "leaf-blade", "stone-edge", "crabhammer", "blaze-kick",
        "poison-tail", "aeroblast", "attack-order", "spacial-rend"
    };

    private static readonly HashSet<string> GuaranteedCriticalMoves = new()
    {
        "storm-throw", "frost-breath"
    };

    private static readonly HashSet<string> ProtectionBypassingMoves = new()
    {
        "feint", "phantom-force", "shadow-force", "hyperspace-hole"
    };

    public static bool MakesContact(string moveKey, Move move) =>
        !move.IsStatus && !move.IsSpecial && move.Power > 0 && !NonContactPhysicalMoves.Contains(moveKey);

    public static string? WeatherForMove(string moveKey) => moveKey switch
    {
        "sunny-day" => BattleWeather.Sun,
        "rain-dance" => BattleWeather.Rain,
        "sandstorm" => BattleWeather.Sand,
        "hail" => BattleWeather.Hail,
        _ => null
    };

    public static string? FieldForMove(string moveKey) => moveKey switch
    {
        "grassy-terrain" => BattleField.Grassy,
        "electric-terrain" => BattleField.Electric,
        "psychic-terrain" => BattleField.Psychic,
        "misty-terrain" => BattleField.Misty,
        _ => null
    };

    public static bool IsGroundShakingMove(string moveKey) => moveKey is
        "earthquake" or "bulldoze" or "magnitude";

    public static PokemonType ResolveMoveType(string moveKey, Move move)
    {
        if (moveKey != "weather-ball") return move.Type;

        return BattleWeather.Current switch
        {
            BattleWeather.Sun => PokemonType.Fire,
            BattleWeather.Rain => PokemonType.Water,
            BattleWeather.Sand => PokemonType.Rock,
            BattleWeather.Hail => PokemonType.Ice,
            _ => PokemonType.Normal
        };
    }

    public static double EffectivePower(string moveKey, Move move)
    {
        double power = move.Power;
        if (moveKey == "weather-ball" && BattleWeather.Current != BattleWeather.Clear)
        {
            power *= 2.0;
        }
        else if (moveKey == "solar-beam"
            && BattleWeather.Current is BattleWeather.Rain or BattleWeather.Sand or BattleWeather.Hail)
        {
            power *= 0.5;
        }

        return power;
    }

    public static double EffectiveAccuracy(string moveKey, Move move)
    {
        double accuracy = move.Accuracy;
        if (moveKey is "thunder" or "hurricane")
        {
            if (BattleWeather.Current == BattleWeather.Rain) return 100;
            if (BattleWeather.Current == BattleWeather.Sun) return 50;
        }
        else if (moveKey == "blizzard")
        {
            if (BattleWeather.Current == BattleWeather.Hail) return 100;
            if (BattleWeather.Current == BattleWeather.Sun) return 50;
        }

        return accuracy;
    }

    public static int RecoveryAmount(string moveKey, Move move, int maxHp)
    {
        if (moveKey is not ("synthesis" or "morning-sun" or "moonlight"))
        {
            return maxHp * move.HealingPercent / 100;
        }

        return BattleWeather.Current switch
        {
            BattleWeather.Sun => maxHp * 2 / 3,
            BattleWeather.Rain or BattleWeather.Sand or BattleWeather.Hail => Math.Max(1, maxHp / 4),
            _ => Math.Max(1, maxHp / 2)
        };
    }

    public static bool HasHighCriticalRate(string moveKey) => HighCriticalRateMoves.Contains(moveKey);

    public static bool GuaranteesCriticalHit(string moveKey) => GuaranteedCriticalMoves.Contains(moveKey);

    public static bool ChangesToShieldForm(string moveKey) => moveKey == "kings-shield";

    public static bool BypassesProtection(string moveKey) => ProtectionBypassingMoves.Contains(moveKey);
}
