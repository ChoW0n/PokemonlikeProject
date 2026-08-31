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

public enum MoveRuleKind
{
    StandardDamage,
    Status,
    Protect,
    Charge,
    DelayedDamage,
    Recharge,
    Binding,
    LeechSeed,
    Yawn,
    PerishSong,
    Disable,
    MoveRestriction,
    ForcedSwitch,
    SelfDestruct,
    VariablePower,
    VariableType,
    SpecialDefenseCalculation,
    DualTypeDamage,
    HazardRemoval
}

public sealed record MoveRule(MoveRuleKind Kind, int Duration = 0, double PowerMultiplier = 1.0);

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

    private static readonly HashSet<string> ChargeMoves = new()
    {
        "solar-beam", "skull-bash", "razor-wind", "sky-attack", "bounce", "dive",
        "fly", "sky-drop", "phantom-force", "shadow-force", "geomancy"
    };

    private static readonly HashSet<string> DelayedDamageMoves = new()
    {
        "future-sight", "doom-desire"
    };

    private static readonly HashSet<string> RechargeMoves = new()
    {
        "giga-impact", "hyper-beam", "rock-wrecker", "roar-of-time",
        "blast-burn", "frenzy-plant", "hydro-cannon", "meteor-assault"
    };

    private static readonly HashSet<string> ProtectMoves = new()
    {
        "protect", "detect", "endure", "kings-shield", "baneful-bunker",
        "spiky-shield", "obstruct", "silk-guard"
    };

    private static readonly HashSet<string> BindingMoves = new()
    {
        "bind", "clamp", "fire-spin", "magma-storm", "sand-tomb", "wrap",
        "whirlpool", "infestation", "snap-trap", "thunder-cage"
    };

    private static readonly HashSet<string> MoveRestrictionMoves = new()
    {
        "taunt", "torment", "throat-chop", "embargo", "heal-block", "imprison",
        "disable", "encore", "attract"
    };

    private static readonly HashSet<string> ForcedSwitchMoves = new()
    {
        "roar", "whirlwind", "dragon-tail", "circle-throw", "u-turn", "volt-switch",
        "parting-shot", "baton-pass", "teleport"
    };

    private static readonly HashSet<string> VariablePowerMoves = new()
    {
        "assurance", "avalanche", "brine", "crush-grip", "electro-ball", "facade",
        "flail", "fling", "grass-knot", "gyro-ball", "hex", "low-kick", "payback",
        "punishment", "reversal", "revenge", "stored-power", "power-trip",
        "venoshock", "water-spout", "eruption", "wring-out"
    };

    private static readonly HashSet<string> SelfDestructMoves = new()
    {
        "self-destruct", "explosion", "misty-explosion"
    };

    /// <summary>
    /// Every catalog entry has a concrete rule. StandardDamage is intentional for
    /// ordinary attacks; it is not an unknown/fallback state.
    /// </summary>
    public static MoveRule GetRule(string moveKey, Move move)
    {
        if (ProtectMoves.Contains(moveKey.Trim())) return new(MoveRuleKind.Protect);
        if (ChargeMoves.Contains(moveKey)) return new(MoveRuleKind.Charge);
        if (DelayedDamageMoves.Contains(moveKey)) return new(MoveRuleKind.DelayedDamage, 2);
        if (RechargeMoves.Contains(moveKey)) return new(MoveRuleKind.Recharge);
        if (BindingMoves.Contains(moveKey)) return new(MoveRuleKind.Binding, 4);
        if (moveKey == "leech-seed") return new(MoveRuleKind.LeechSeed);
        if (moveKey == "yawn") return new(MoveRuleKind.Yawn, 1);
        if (moveKey == "perish-song") return new(MoveRuleKind.PerishSong, 3);
        if (moveKey is "disable") return new(MoveRuleKind.Disable, 5);
        if (MoveRestrictionMoves.Contains(moveKey)) return new(MoveRuleKind.MoveRestriction, 5);
        if (ForcedSwitchMoves.Contains(moveKey)) return new(MoveRuleKind.ForcedSwitch);
        if (SelfDestructMoves.Contains(moveKey)) return new(MoveRuleKind.SelfDestruct);
        if (VariablePowerMoves.Contains(moveKey)) return new(MoveRuleKind.VariablePower);
        if (moveKey is "judgment" or "techno-blast" or "natural-gift")
            return new(MoveRuleKind.VariableType);
        if (moveKey is "secret-sword" or "psystrike" or "psyshock")
            return new(MoveRuleKind.SpecialDefenseCalculation);
        if (moveKey == "flying-press") return new(MoveRuleKind.DualTypeDamage);
        if (moveKey == "rapid-spin") return new(MoveRuleKind.HazardRemoval);
        return move.IsStatus ? new(MoveRuleKind.Status) : new(MoveRuleKind.StandardDamage);
    }

    public static bool IsChargeMove(string moveKey) => ChargeMoves.Contains(moveKey);
    public static bool IsDelayedDamageMove(string moveKey) => DelayedDamageMoves.Contains(moveKey);
    public static bool RequiresRecharge(string moveKey) => RechargeMoves.Contains(moveKey);
    public static bool IsProtectionMove(string moveKey) => ProtectMoves.Contains(moveKey.Trim());
    public static bool IsBindingMove(string moveKey) => BindingMoves.Contains(moveKey);
    public static bool IsForcedSwitchMove(string moveKey) => ForcedSwitchMoves.Contains(moveKey);

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

    public static PokemonType ResolveMoveType(string moveKey, Move move, Pokemon? attacker = null)
    {
        PokemonType resolvedType = move.Type;
        if (moveKey == "judgment" && attacker != null)
        {
            resolvedType = attacker.HeldItem switch
            {
                "불꽃플레이트" => PokemonType.Fire,
                "물방울플레이트" => PokemonType.Water,
                "전기플레이트" => PokemonType.Electric,
                "초원플레이트" => PokemonType.Grass,
                "고드름플레이트" => PokemonType.Ice,
                "주먹플레이트" => PokemonType.Fighting,
                "독플레이트" => PokemonType.Poison,
                "대지플레이트" => PokemonType.Ground,
                "푸른하늘플레이트" => PokemonType.Flying,
                "이상한플레이트" => PokemonType.Psychic,
                "비늘플레이트" => PokemonType.Bug,
                "암석플레이트" => PokemonType.Rock,
                "원령플레이트" => PokemonType.Ghost,
                "용의플레이트" => PokemonType.Dragon,
                "공포플레이트" => PokemonType.Dark,
                "강철플레이트" => PokemonType.Steel,
                "정령플레이트" => PokemonType.Fairy,
                _ => move.Type
            };
        }
        if (moveKey == "techno-blast" && attacker != null)
        {
            resolvedType = attacker.HeldItem switch
            {
                "불꽃카세트" => PokemonType.Fire,
                "아쿠아카세트" => PokemonType.Water,
                "번개카세트" => PokemonType.Electric,
                "프리즈카세트" => PokemonType.Ice,
                _ => move.Type
            };
        }
        if (moveKey == "weather-ball")
        {
            resolvedType = BattleWeather.Current switch
            {
                BattleWeather.Sun => PokemonType.Fire,
                BattleWeather.Rain => PokemonType.Water,
                BattleWeather.Sand => PokemonType.Rock,
                BattleWeather.Hail => PokemonType.Ice,
                _ => PokemonType.Normal
            };
        }
        if (attacker == null) return resolvedType;
        if (attacker.SelectedAbility == "노말스킨") return PokemonType.Normal;
        if (resolvedType == PokemonType.Normal && attacker.SelectedAbility == "프리즈스킨")
            return PokemonType.Ice;
        if (resolvedType == PokemonType.Normal && attacker.SelectedAbility == "페어리스킨")
            return PokemonType.Fairy;
        return resolvedType;
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

    public static double EffectivePower(
        string moveKey,
        Move move,
        Pokemon attacker,
        Pokemon defender)
    {
        double power = EffectivePower(moveKey, move);
        switch (moveKey)
        {
            case "facade" when attacker.Status != StatusCondition.None:
            case "hex" when defender.Status != StatusCondition.None || defender.IsConfused:
            case "venoshock" when defender.Status is StatusCondition.Poison:
                power *= 2;
                break;
            case "brine" when defender.CurrentHp <= defender.MaxHp / 2:
            case "assurance" when defender.LastDamageTakenThisTurn:
            case "payback" when defender.LastDamageTakenThisTurn:
            case "revenge" when attacker.WasDamagedThisTurn:
            case "avalanche" when attacker.WasDamagedThisTurn:
                power *= 2;
                break;
            case "stored-power":
                power = 20 + 20 * attacker.StatStages.Values.Where(v => v > 0).Sum();
                break;
            case "power-trip":
                power = 20 + 20 * attacker.StatStages.Values.Where(v => v > 0).Sum();
                break;
            case "punishment":
                power = Math.Min(200, 60 + 20 * defender.StatStages.Values.Where(v => v > 0).Sum());
                break;
            case "flail":
            case "reversal":
                power = attacker.CurrentHp * 200.0 / Math.Max(1, attacker.MaxHp);
                power = Math.Max(20, 200 - power);
                break;
            case "water-spout":
            case "eruption":
                power *= (double)attacker.CurrentHp / Math.Max(1, attacker.MaxHp);
                break;
            case "electro-ball":
                power = Math.Max(40, 120 * (double)Math.Max(1, attacker.EffectiveSpd)
                    / Math.Max(1, defender.EffectiveSpd));
                break;
            case "gyro-ball":
                power = Math.Min(150, 25 * (double)Math.Max(1, defender.EffectiveSpd)
                    / Math.Max(1, attacker.EffectiveSpd));
                break;
            case "crush-grip":
            case "wring-out":
                power = Math.Max(1, 120 * (double)defender.CurrentHp / Math.Max(1, defender.MaxHp));
                break;
            case "grass-knot":
            case "low-kick":
                // Weight is not part of the current 1v1 data model. Base HP is
                // a stable proxy that still gives light/heavy species distinct
                // deterministic tiers without inventing a second database.
                power = defender.Data.BaseHp >= 120 ? 100
                    : defender.Data.BaseHp >= 90 ? 80
                    : defender.Data.BaseHp >= 60 ? 60
                    : defender.Data.BaseHp >= 30 ? 40 : 20;
                break;
        }
        return Math.Max(1, power);
    }

    public static PokemonType? SecondaryAttackType(string moveKey) =>
        moveKey == "flying-press" ? PokemonType.Flying : null;

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
