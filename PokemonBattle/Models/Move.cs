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
    Rampage,
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
    HazardRemoval,
    Substitute,
    TrickRoom,
    Gravity,
    Counter,
    MirrorCoat,
    ItemSwap,
    HazardPlacement
}

public enum ProtectionEffect
{
    Block,
    Endure,
    KingsShield,
    BanefulBunker,
    SpikyShield,
    Obstruct
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
        "spiky-shield", "obstruct"
    };

    private static readonly HashSet<string> RampageMoves = new()
    {
        "outrage", "petal-dance", "thrash"
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

    private static readonly HashSet<string> HazardPlacementMoves = new()
    {
        "stealth-rock", "spikes", "toxic-spikes", "sticky-web"
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

    private static readonly HashSet<string> WindMoves = new()
    {
        "gust", "twister", "hurricane", "razor-wind", "silver-wind", "icy-wind",
        "tailwind", "whirlwind", "fairy-wind", "sand-attack", "heat-wave"
    };

    private static readonly HashSet<string> SlicingMoves = new()
    {
        "razor-leaf", "razor-wind", "slash", "night-slash", "psycho-cut",
        "leaf-blade", "air-cutter", "air-slash", "aqua-cutter", "x-scissor",
        "sacred-sword", "secret-sword", "solar-blade", "kowtow-cleave",
        "ceaseless-edge", "mighty-cleave"
    };

    /// <summary>
    /// Every catalog entry has a concrete rule. StandardDamage is intentional for
    /// ordinary attacks; it is not an unknown/fallback state.
    /// </summary>
    public static MoveRule GetRule(string moveKey, Move move)
    {
        if (ProtectMoves.Contains(moveKey.Trim())) return new(MoveRuleKind.Protect);
        if (RampageMoves.Contains(moveKey)) return new(MoveRuleKind.Rampage);
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
        if (moveKey == "substitute") return new(MoveRuleKind.Substitute);
        if (moveKey == "trick-room") return new(MoveRuleKind.TrickRoom, 5);
        if (moveKey == "gravity") return new(MoveRuleKind.Gravity, 5);
        if (moveKey == "counter") return new(MoveRuleKind.Counter);
        if (moveKey == "mirror-coat") return new(MoveRuleKind.MirrorCoat);
        if (moveKey is "trick" or "switcheroo") return new(MoveRuleKind.ItemSwap);
        if (HazardPlacementMoves.Contains(moveKey)) return new(MoveRuleKind.HazardPlacement);
        return move.IsStatus ? new(MoveRuleKind.Status) : new(MoveRuleKind.StandardDamage);
    }

    public static bool IsChargeMove(string moveKey) => ChargeMoves.Contains(moveKey);
    public static bool IsRampageMove(string moveKey) => RampageMoves.Contains(moveKey);
    public static bool IsDelayedDamageMove(string moveKey) => DelayedDamageMoves.Contains(moveKey);
    public static bool RequiresRecharge(string moveKey) => RechargeMoves.Contains(moveKey);
    public static bool IsProtectionMove(string moveKey) => ProtectMoves.Contains(moveKey.Trim());
    public static ProtectionEffect GetProtectionEffect(string moveKey) => moveKey switch
    {
        "endure" => ProtectionEffect.Endure,
        "kings-shield" => ProtectionEffect.KingsShield,
        "baneful-bunker" => ProtectionEffect.BanefulBunker,
        "spiky-shield" => ProtectionEffect.SpikyShield,
        "obstruct" => ProtectionEffect.Obstruct,
        _ => ProtectionEffect.Block
    };
    public static bool IsBindingMove(string moveKey) => BindingMoves.Contains(moveKey);
    public static bool IsForcedSwitchMove(string moveKey) => ForcedSwitchMoves.Contains(moveKey);
    public static bool IsWindMove(string moveKey) => WindMoves.Contains(moveKey);
    public static bool IsSlicingMove(string moveKey) => SlicingMoves.Contains(moveKey);

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

    public static PokemonType ResolveMoveType(
        string moveKey,
        Move move,
        Pokemon? attacker = null,
        Pokemon? defender = null)
    {
        PokemonType resolvedType = move.Type;
        if (moveKey == "judgment" && attacker != null)
        {
            resolvedType = Pokemon.GetPlateType(attacker.HeldItem) ?? move.Type;
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
        if (moveKey == "weather-ball" && !BattleWeather.AreEffectsSuppressed(attacker, defender))
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
        if (attacker.HasActiveAbility("노말스킨", defender)) return PokemonType.Normal;
        if (resolvedType == PokemonType.Normal && attacker.HasActiveAbility("프리즈스킨", defender))
            return PokemonType.Ice;
        if (resolvedType == PokemonType.Normal && attacker.HasActiveAbility("페어리스킨", defender))
            return PokemonType.Fairy;
        return resolvedType;
    }

    public static double AuraMultiplier(
        PokemonType attackType,
        Pokemon? attacker,
        Pokemon? defender)
    {
        if (attackType is not (PokemonType.Fairy or PokemonType.Dark)) return 1.0;

        string aura = attackType == PokemonType.Fairy ? "페어리오라" : "다크오라";
        bool auraActive = attacker?.HasActiveAbility(aura, defender) == true
            || defender?.HasActiveAbility(aura, attacker) == true;
        if (!auraActive) return 1.0;

        bool auraBroken = attacker?.HasActiveAbility("오라브레이크", defender) == true
            || defender?.HasActiveAbility("오라브레이크", attacker) == true;
        return auraBroken ? 0.75 : 4.0 / 3.0;
    }

    private static bool HasActiveAbility(Pokemon? pokemon, string ability) =>
        pokemon != null && !pokemon.IsFainted && pokemon.SelectedAbility == ability;

    public static double EffectivePower(string moveKey, Move move)
        => EffectivePowerBase(moveKey, move, weatherSuppressed: false);

    private static double EffectivePowerBase(string moveKey, Move move, bool weatherSuppressed)
    {
        double power = move.Power;
        if (!weatherSuppressed && moveKey == "weather-ball" && BattleWeather.Current != BattleWeather.Clear)
        {
            power *= 2.0;
        }
        else if (!weatherSuppressed && moveKey == "solar-beam"
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
        Pokemon defender,
        bool? attackerMovedFirst = null)
    {
        double power = EffectivePowerBase(
            moveKey,
            move,
            BattleWeather.AreEffectsSuppressed(attacker, defender));
        switch (moveKey)
        {
            case "facade" when attacker.Status != StatusCondition.None:
            case "hex" when defender.Status != StatusCondition.None:
            case "venoshock" when defender.Status is StatusCondition.Poison:
                power *= 2;
                break;
            case "brine" when defender.CurrentHp <= defender.MaxHp / 2:
            case "assurance" when defender.LastDamageTakenThisTurn:
            case "payback" when attackerMovedFirst == false:
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
                power = Math.Max(40, 120 * (double)Math.Max(1, attacker.EffectiveSpdAgainst(defender))
                    / Math.Max(1, defender.EffectiveSpdAgainst(attacker)));
                break;
            case "gyro-ball":
                power = Math.Min(150, 25 * (double)Math.Max(1, defender.EffectiveSpdAgainst(attacker))
                    / Math.Max(1, attacker.EffectiveSpdAgainst(defender)));
                break;
            case "crush-grip":
            case "wring-out":
                power = Math.Max(1, 120 * (double)defender.CurrentHp / Math.Max(1, defender.MaxHp));
                break;
            case "grass-knot":
            case "low-kick":
                double weight = defender.GetEffectiveWeight(attacker);
                power = weight >= 120 ? 120
                    : weight >= 100 ? 100
                    : weight >= 80 ? 80
                    : weight >= 60 ? 60
                    : weight >= 40 ? 40 : 20;
                break;
        }
        return Math.Max(1, power);
    }

    public static PokemonType? SecondaryAttackType(string moveKey) =>
        moveKey == "flying-press" ? PokemonType.Flying : null;

    public static double EffectiveAccuracy(
        string moveKey,
        Move move,
        Pokemon? attacker = null,
        Pokemon? defender = null)
    {
        double accuracy = move.Accuracy;
        bool weatherSuppressed = BattleWeather.AreEffectsSuppressed(attacker, defender);
        if (!weatherSuppressed && moveKey is ("thunder" or "hurricane"))
        {
            if (BattleWeather.Current == BattleWeather.Rain) accuracy = 100;
            else if (BattleWeather.Current == BattleWeather.Sun) accuracy = 50;
        }
        else if (!weatherSuppressed && moveKey == "blizzard")
        {
            if (BattleWeather.Current == BattleWeather.Hail) accuracy = 100;
            else if (BattleWeather.Current == BattleWeather.Sun) accuracy = 50;
        }

        if (attacker != null)
        {
            if (attacker.HasActiveAbility("의욕", defender) && !move.IsStatus && !move.IsSpecial) accuracy *= 0.8;
            if (attacker.HasActiveAbility("복안", defender)) accuracy *= 1.3;
            if (attacker.HasActiveAbility("승리의별", defender)) accuracy *= 1.1;
            if (attacker.HasActiveHeldItem(defender) && attacker.HeldItem == "광각렌즈") accuracy *= 1.1;
            bool attackerUnaware = attacker.HasActiveAbility("천진", defender);
            if (!attackerUnaware)
                accuracy *= AccuracyStageMultiplier(attacker.StatStages["accuracy"]);
        }

        if (defender != null)
        {
            if (!weatherSuppressed
                && !defender.IsAbilitySuppressedBy(attacker)
                && defender.HasActiveAbility("모래숨기", attacker)
                && BattleWeather.Current == BattleWeather.Sand) accuracy *= 0.8;
            if (!weatherSuppressed
                && !defender.IsAbilitySuppressedBy(attacker)
                && defender.HasActiveAbility("눈숨기", attacker)
                && BattleWeather.Current == BattleWeather.Hail) accuracy *= 0.8;
            if (!defender.IsAbilitySuppressedBy(attacker)
                && defender.HasActiveAbility("갈지자걸음", attacker) && defender.IsConfused) accuracy *= 0.5;
            bool defenderUnaware = defender.HasActiveAbility("천진", attacker);
            if (!defenderUnaware)
                accuracy /= AccuracyStageMultiplier(defender.StatStages["evasion"]);
            if (!defender.IsAbilitySuppressedBy(attacker)
                && defender.HasActiveAbility("미라클스킨", attacker)
                && move.IsStatus
                && TargetsOpponent(move)
                && !move.AlwaysHits)
            {
                accuracy *= 0.5;
            }
            if (defender.HasActiveHeldItem(attacker) && defender.HeldItem == "반짝가루")
                accuracy *= 0.9;
        }

        return accuracy;
    }

    private static double AccuracyStageMultiplier(int stage) =>
        stage >= 0 ? (3.0 + stage) / 3.0 : 3.0 / (3.0 - stage);

    private static bool TargetsOpponent(Move move)
    {
        if (!move.IsStatus) return true;
        if (move.AilmentName != "none") return true;
        return move.StatChanges.Any(change => !change.TargetsSelf);
    }

    public static int RecoveryAmount(
        string moveKey,
        Move move,
        int maxHp,
        Pokemon? user = null,
        Pokemon? opponent = null)
    {
        if (moveKey is not ("synthesis" or "morning-sun" or "moonlight"))
        {
            return maxHp * move.HealingPercent / 100;
        }

        if (BattleWeather.AreEffectsSuppressed(user, opponent))
        {
            return Math.Max(1, maxHp / 2);
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
