namespace PokemonBattle.Models;

public class Pokemon
{
    private static int nextActorNumber;
    private readonly PokemonData originalData;
    private readonly string[] originalMoveKeys;
    private readonly string originalAbility;
    private Dictionary<string, int>? preTransformPP;
    private PokemonType? typeOverride1;
    private PokemonType? typeOverride2;
    private string heldItem = "없음";
    private int runMaxHpPenaltyPercent;

    public PokemonData Data;
    public string ActorId { get; }
    public int Level;
    public int CurrentHp;
    public bool IsFainted;
    public PokemonGender Gender { get; set; }
    public double LastMultiplier;
    public bool SurvivedByEndure;
    public Dictionary<string, int> CurrentPP = new();
    public string SelectedAbility = "";
    public string HeldItem
    {
        get => heldItem;
        set
        {
            if (heldItem != "없음" && value == "없음") HasLostHeldItem = true;
            heldItem = value;
        }
    }
    public string? ChoiceLockedMove { get; private set; }
    public string? DisabledMoveKey { get; private set; }
    public int DisabledTurnsRemaining { get; private set; }
    public bool FlashFireActive { get; private set; }
    public int TurnsOnField { get; private set; }
    public int ReflectTurnsRemaining { get; private set; }
    public int LightScreenTurnsRemaining { get; private set; }
    public int AuroraVeilTurnsRemaining { get; private set; }
    public bool IsAlternateForm { get; private set; }
    public bool HasConsumedBerry { get; private set; }
    public bool HasPickedUpItem { get; private set; }
    public bool HasLostHeldItem { get; private set; }
    public bool HasHoneyGathered { get; private set; }
    public bool IsIllusionActive { get; private set; }
    public bool WasIllusionBroken { get; private set; }
    public PokemonType CurrentType1 => typeOverride1
        ?? (SelectedAbility == "멀티타입"
            ? GetPlateType(HeldItem) ?? Data.Type1
            : Data.Type1);
    public PokemonType? CurrentType2 => typeOverride1 != null
        ? typeOverride2
        : (SelectedAbility == "멀티타입" && GetPlateType(HeldItem) != null ? null : Data.Type2);
    public string TypeDisplay => CurrentType2 == null
        ? CurrentType1.ToString()
        : $"{CurrentType1}/{CurrentType2}";
    public bool HasType(PokemonType type) => CurrentType1 == type || CurrentType2 == type;
    public bool IsSteelType => HasType(PokemonType.Steel);
    public bool CanBeForcedSwitched => SelectedAbility != "흡반";
    private string? ConsumedBerryName { get; set; }
    public bool LastHitWasCritical { get; private set; }
    public bool IsProtected { get; private set; }
    public int ProtectionStreak { get; private set; }
    public string? ActiveProtectionMoveKey { get; private set; }
    public string? RampageMoveKey { get; private set; }
    public int RampageTurnsRemaining { get; private set; }
    public string? PendingMoveKey { get; private set; }
    public string? PendingDelayedAttackKey { get; private set; }
    public int PendingDelayedAttackTurns { get; private set; }
    public Pokemon? PendingDelayedTarget { get; private set; }
    public bool MustRecharge { get; private set; }
    public bool IsSemiInvulnerable { get; private set; }
    public bool WasDamagedThisTurn { get; private set; }
    public bool LastDamageTakenThisTurn => WasDamagedThisTurn;
    public int LastDamageTakenAmountThisTurn { get; private set; }
    public bool LastDamageTakenWasSpecialThisTurn { get; private set; }
    public Pokemon? LastDamageSourceThisTurn { get; private set; }
    public bool LastHitBlockedBySubstitute { get; private set; }
    public int SubstituteHp { get; private set; }
    public bool HasSubstitute => SubstituteHp > 0;
    public bool IsTransformed { get; private set; }
    public bool IsBadlyPoisoned { get; private set; }
    public int ToxicTurns { get; private set; }
    public bool LeechSeeded { get; private set; }
    public Pokemon? LeechSeedSource { get; private set; }
    public bool Ingrained { get; private set; }
    public string? BindingMoveKey { get; private set; }
    public int BindingTurnsRemaining { get; private set; }
    public int YawnTurnsRemaining { get; private set; }
    public int PerishTurnsRemaining { get; private set; }
    public int HealBlockTurnsRemaining { get; private set; }
    public int TauntTurnsRemaining { get; private set; }
    public int TormentTurnsRemaining { get; private set; }
    public int ThroatChopTurnsRemaining { get; private set; }
    public int EmbargoTurnsRemaining { get; private set; }
    public string? EncoreMoveKey { get; private set; }
    public int EncoreTurnsRemaining { get; private set; }
    public HashSet<string> ImprisonedMoveKeys { get; } = new();
    public bool IsInfatuated { get; private set; }
    public bool UproarActive { get; private set; }
    public int UproarTurnsRemaining { get; private set; }
    public bool NightmareActive { get; private set; }
    public bool ChargeBoostActive { get; private set; }
    public bool TypeImmunityRevealed { get; private set; }
    public string? LastMoveKey { get; private set; }
    public HashSet<string> UsedMoveKeys { get; } = new();
    public int StockpileCount { get; private set; }
    public bool RageActive { get; private set; }

    public string FormKey
    {
        get
        {
            if (Data.EnglishName == "castform" && IsAlternateForm)
            {
                return CurrentType1 switch
                {
                    PokemonType.Fire => "sunny",
                    PokemonType.Water => "rainy",
                    PokemonType.Ice => "snowy",
                    _ => "default"
                };
            }
            return IsAlternateForm ? "alternate" : "default";
        }
    }

    public StatusCondition Status = StatusCondition.None;
    public int SleepTurnsRemaining;
    public bool IsConfused;
    public int ConfusionTurnsRemaining;
    public bool Flinched;
    public Dictionary<string, int> StatStages = new()
    {
        ["attack"] = 0, ["defense"] = 0, ["special-attack"] = 0, ["special-defense"] = 0,
        ["speed"] = 0, ["accuracy"] = 0, ["evasion"] = 0
    };

    private static readonly Random rng = new Random();

    private int StatHp(int baseStat) => (2 * baseStat + 31) * Level / 100 + Level + 10;
    private int StatOther(int baseStat) => (2 * baseStat + 31) * Level / 100 + 5;

    public int MaxHp => Math.Max(1,
        StatHp(IsTransformed || IsIllusionActive ? originalData.BaseHp : Data.BaseHp)
        * (100 - runMaxHpPenaltyPercent) / 100);
    public bool ResistsStatusStatPenalties { get; private set; }

    public void ApplyRunModifiers(int maxHpPenaltyPercent, bool resistsStatusStatPenalties)
    {
        runMaxHpPenaltyPercent = Math.Clamp(maxHpPenaltyPercent, 0, 90);
        ResistsStatusStatPenalties = resistsStatusStatPenalties;
        CurrentHp = Math.Min(CurrentHp, MaxHp);
    }

    public double EffectiveWeight => GetEffectiveWeight();

    public double GetEffectiveWeight(Pokemon? opponent = null) =>
        (HasActiveAbility("라이트메탈", opponent) ? 0.5 : 1.0) * Data.BaseHp;

    public bool HasActiveHeldItem(Pokemon? opponent = null) =>
        HeldItem != "없음"
        && EmbargoTurnsRemaining <= 0
        && !HasActiveAbility("서투름", opponent);

    public bool HasActiveAbility(string ability, Pokemon? opponent = null) =>
        SelectedAbility == ability
        && !IsAbilitySuppressedBy(opponent)
        && (ability == "화학변화가스"
            || opponent == null
            || opponent.IsFainted
            || opponent.SelectedAbility != "화학변화가스");

    private double ItemStatMult(string stat)
    {
        bool isPikachu = Data.Name == "피카츄";
        bool isMarowak = Data.Name == "텅구리";
        bool isLatiosLatias = Data.Name == "라티오스" || Data.Name == "라티아스";

        if (!HasActiveHeldItem()) return 1.0;
        if (HeldItem == "전기구슬" && isPikachu && (stat == "attack" || stat == "special-attack")) return 2.0;
        if (HeldItem == "두꺼운뼈" && isMarowak && stat == "attack") return 2.0;
        if (HeldItem == "이슬의구슬" && isLatiosLatias && (stat == "special-attack" || stat == "special-defense")) return 1.5;
        if (HeldItem == "메탈파우더" && Data.EnglishName == "ditto"
            && (stat == "defense" || stat == "special-defense")) return 1.5;
        if (HeldItem == "돌격조끼" && stat == "special-defense") return 1.5;
        if (HeldItem == "진화의휘석" && Data.EvolvesToId != null
            && (stat == "defense" || stat == "special-defense")) return 1.5;
        return 1.0;
    }

    private int BaseStat(string stat)
    {
        if (Data.Name == "킬가르도" && IsAlternateForm)
        {
            return stat switch
            {
                "attack" => 140,
                "defense" => 50,
                "special-attack" => 140,
                "special-defense" => 50,
                _ => Data.BaseHp
            };
        }

        if (Data.Name == "불비달마" && IsAlternateForm)
        {
            return stat switch
            {
                "attack" => 30,
                "defense" => 105,
                "special-attack" => 140,
                "special-defense" => 105,
                "speed" => 55,
                _ => Data.BaseHp
            };
        }

        return stat switch
        {
            "attack" => Data.BaseAtk,
            "defense" => Data.BaseDef,
            "special-attack" => Data.BaseSpAtk,
            "special-defense" => Data.BaseSpDef,
            "speed" => Data.BaseSpd,
            _ => Data.BaseHp
        };
    }

    public int Atk => (int)(StatOther(BaseStat("attack")) * ItemStatMult("attack"));
    public int Def => StatOther(BaseStat("defense"));
    public int SpAtk => (int)(StatOther(BaseStat("special-attack")) * ItemStatMult("special-attack"));
    public int SpDef => (int)(StatOther(BaseStat("special-defense")) * ItemStatMult("special-defense"));
    public int Spd => StatOther(BaseStat("speed"));

    private double StageMult(string key)
    {
        int stage = StatStages[key];
        return stage >= 0 ? (2.0 + stage) / 2.0 : 2.0 / (2.0 - stage);
    }

    public int EffectiveAtk => EffectiveAtkAgainst();

    public int EffectiveAtkAgainst(Pokemon? opponent = null)
    {
        bool opponentIgnoresStages = opponent?.SelectedAbility == "천진"
            && !opponent.IsAbilitySuppressedBy(this);
        double value = Atk * (opponentIgnoresStages ? 1.0 : StageMult("attack"));
        if (Status == StatusCondition.Burn && SelectedAbility != "근성")
            value *= ResistsStatusStatPenalties ? 0.75 : 0.5;
        if (HasActiveAbility("근성", opponent) && Status != StatusCondition.None) value *= 1.5;
        if (HasActiveAbility("의욕", opponent)) value *= 1.5;
        if (HasActiveAbility("천하장사", opponent) || HasActiveAbility("순수한힘", opponent)) value *= 2.0;
        if (HasActiveAbility("무기력", opponent) && CurrentHp <= MaxHp / 2) value *= 0.5;
        if (!IsAbilitySuppressedBy(opponent)
            && !BattleWeather.AreEffectsSuppressed(this, opponent)
            && HasActiveAbility("플라워기프트", opponent) && BattleWeather.Current == "쾌청")
        {
            value *= 1.5;
        }
        if (HasActiveAbility("슬로스타트", opponent) && TurnsOnField < 5) value *= 0.5;
        return (int)value;
    }

    public int EffectiveDef => EffectiveDefAgainst();

    public int EffectiveDefAgainst(Pokemon? opponent = null)
    {
        bool opponentIgnoresStages = opponent?.SelectedAbility == "천진";
        double value = Def * (opponentIgnoresStages ? 1.0 : StageMult("defense"));
        if (!IsAbilitySuppressedBy(opponent)
            && HasActiveAbility("풀모피", opponent) && BattleField.Current == BattleField.Grassy) value *= 2.0;
        if (!IsAbilitySuppressedBy(opponent)
            && HasActiveAbility("이상한비늘", opponent) && Status != StatusCondition.None) value *= 1.5;
        return (int)value;
    }

    public int EffectiveSpAtk => EffectiveSpAtkAgainst();

    public int EffectiveSpAtkAgainst(Pokemon? opponent = null, Pokemon? ally = null)
    {
        bool opponentIgnoresStages = opponent?.SelectedAbility == "천진";
        double value = SpAtk * (opponentIgnoresStages ? 1.0 : StageMult("special-attack"));
        if (HasPlusMinusPartner(ally) && HasActiveAbility(SelectedAbility, opponent)) value *= 1.5;
        if (!BattleWeather.AreEffectsSuppressed(this, opponent)
            && HasActiveAbility("선파워", opponent) && BattleWeather.Current == "쾌청") value *= 1.5;
        if (HasActiveAbility("무기력", opponent) && CurrentHp <= MaxHp / 2) value *= 0.5;
        return (int)value;
    }

    public int EffectiveSpDef => EffectiveSpDefAgainst();

    public int EffectiveSpDefAgainst(Pokemon? opponent = null)
    {
        bool opponentIgnoresStages = opponent?.SelectedAbility == "천진";
        double value = SpDef * (opponentIgnoresStages ? 1.0 : StageMult("special-defense"));
        if (!IsAbilitySuppressedBy(opponent)
            && !BattleWeather.AreEffectsSuppressed(this, opponent)
            && HasActiveAbility("플라워기프트", opponent) && BattleWeather.Current == "쾌청")
        {
            value *= 1.5;
        }
        return (int)value;
    }

    // The current battle is 1v1, so callers intentionally pass no ally. A future
    // double-battle side can pass the actual living partner without treating the
    // opposing Pokémon as a partner.
    public bool HasPlusMinusPartner(Pokemon? ally) =>
        ally != null
        && !ally.IsFainted
        && ((SelectedAbility == "플러스" && ally.SelectedAbility == "마이너스")
            || (SelectedAbility == "마이너스" && ally.SelectedAbility == "플러스"));

    //엽록소·쓱쓱·모래헤치기·눈치우기: 날씨에서 속도 2배
    public int EffectiveSpd => EffectiveSpdAgainst();

    public int EffectiveSpdAgainst(Pokemon? opponent = null)
    {
        double spd = Spd * StageMult("speed");
        if (Status == StatusCondition.Paralysis && SelectedAbility != "속보")
            spd *= ResistsStatusStatPenalties ? 0.75 : 0.5;
        if (!BattleWeather.AreEffectsSuppressed(this, opponent))
        {
            if (HasActiveAbility("엽록소", opponent) && BattleWeather.Current == "쾌청") spd *= 2.0;
            if (HasActiveAbility("쓱쓱", opponent) && BattleWeather.Current == "비") spd *= 2.0;
            if (HasActiveAbility("모래헤치기", opponent) && BattleWeather.Current == "모래바람") spd *= 2.0;
            if (HasActiveAbility("눈치우기", opponent) && BattleWeather.Current == "싸라기눈") spd *= 2.0;
        }
        if (HasActiveAbility("속보", opponent) && Status != StatusCondition.None) spd *= 1.5;
        if (HasActiveAbility("슬로스타트", opponent) && TurnsOnField < 5) spd *= 0.5;
        if (HasActiveAbility("곡예", opponent) && HasLostHeldItem) spd *= 2.0;
        return (int)spd;
    }

    public Pokemon(
        PokemonData data,
        List<string>? chosenMoves = null,
        string ability = "",
        string item = "없음",
        int level = 1,
        PokemonGender? gender = null)
    {
        originalData = data;
        originalAbility = ability;
        Data = data;
        ActorId = $"{data.EnglishName}-{Interlocked.Increment(ref nextActorNumber)}";
        Level = level;
        Gender = gender ?? InferGender(data);
        CurrentHp = MaxHp;
        IsFainted = false;
        SelectedAbility = ability;
        HeldItem = item;

        var moveList = chosenMoves != null && chosenMoves.Count > 0 ? chosenMoves : data.MoveNames.ToList();
        originalMoveKeys = moveList.ToArray();
        foreach (var moveName in moveList)
        {
            if (MoveDatabase.All.ContainsKey(moveName))
            {
                CurrentPP[moveName] = MoveDatabase.All[moveName].MaxPP;
            }
        }

        if (CurrentPP.Count == 0)
        {
            var fallback = data.MoveNames.FirstOrDefault(m => MoveDatabase.All.ContainsKey(m));
            if (fallback != null) CurrentPP[fallback] = MoveDatabase.All[fallback].MaxPP;
            else if (MoveDatabase.All.ContainsKey("tackle")) CurrentPP["tackle"] = MoveDatabase.All["tackle"].MaxPP;
        }
    }

    private static PokemonGender InferGender(PokemonData data)
    {
        string englishName = data.EnglishName.ToLowerInvariant();
        if (englishName.EndsWith("-male") || englishName.EndsWith("-m")
            || data.Name.EndsWith('♂'))
        {
            return PokemonGender.Male;
        }

        if (englishName.EndsWith("-female") || englishName.EndsWith("-f")
            || data.Name.EndsWith('♀'))
        {
            return PokemonGender.Female;
        }

        return PokemonGender.Unknown;
    }

    private static Dictionary<string, int> CreateMovePp(IEnumerable<string> moveKeys) =>
        moveKeys.Where(MoveDatabase.All.ContainsKey)
            .ToDictionary(key => key, key => MoveDatabase.All[key].MaxPP);

    private bool IsChoiceItem =>
        HasActiveHeldItem()
        && (HeldItem == "구애스카프" || HeldItem == "구애머리띠" || HeldItem == "구애안경");

    public bool CanUseMove(string moveName)
    {
        if (RampageMoveKey != null) return RampageMoveKey == moveName;
        if (!CurrentPP.TryGetValue(moveName, out var pp) || pp <= 0) return false;
        if (DisabledMoveKey == moveName) return false;
        if (ImprisonedMoveKeys.Contains(moveName)) return false;
        if (IsChoiceItem && ChoiceLockedMove != null && ChoiceLockedMove != moveName) return false;
        if (HasActiveHeldItem() && HeldItem == "돌격조끼"
            && MoveDatabase.All.TryGetValue(moveName, out var moveForItem)
            && moveForItem.IsStatus) return false;
        if (moveName == "belch" && !HasConsumedBerry) return false;
        if (moveName is "snore" or "dream-eater" && Status != StatusCondition.Sleep) return false;
        if (moveName == "fake-out" && TurnsOnField > 0) return false;
        if (moveName == "last-resort"
            && CurrentPP.Keys.Any(key => key != "last-resort" && !UsedMoveKeys.Contains(key))) return false;
        if (HealBlockTurnsRemaining > 0 && MoveDatabase.All.TryGetValue(moveName, out var blockedMove)
            && blockedMove.HealingPercent > 0) return false;
        if (TauntTurnsRemaining > 0 && MoveDatabase.All.TryGetValue(moveName, out var tauntMove)
            && tauntMove.IsStatus) return false;
        if (TormentTurnsRemaining > 0 && LastMoveKey == moveName) return false;
        if (EncoreTurnsRemaining > 0 && EncoreMoveKey != null && EncoreMoveKey != moveName) return false;
        if (ThroatChopTurnsRemaining > 0 && moveName is "uproar" or "snore" or "hyper-voice"
            or "boomburst" or "sing" or "supersonic" or "roar" or "screech") return false;
        return true;
    }

    public bool TryUseMove(string moveName)
    {
        if (!CanUseMove(moveName)) return false;
        if (RampageMoveKey == null)
        {
            CurrentPP[moveName]--;
            if (IsChoiceItem) ChoiceLockedMove ??= moveName;
        }
        return true;
    }

    public void ResetOnSwitchOut()
    {
        SelectedAbility = originalAbility;
        if (IsTransformed)
        {
            Data = originalData;
            IsTransformed = false;
            typeOverride1 = null;
            typeOverride2 = null;
            IsAlternateForm = false;
            CurrentPP = preTransformPP != null
                ? new Dictionary<string, int>(preTransformPP)
                : CreateMovePp(originalMoveKeys);
            preTransformPP = null;
            UsedMoveKeys.Clear();
        }
        else if (IsIllusionActive)
        {
            Data = originalData;
            IsIllusionActive = false;
        }
        typeOverride1 = null;
        typeOverride2 = null;

        StatStages = new()
        {
            ["attack"] = 0, ["defense"] = 0, ["special-attack"] = 0, ["special-defense"] = 0,
            ["speed"] = 0, ["accuracy"] = 0, ["evasion"] = 0
        };
        IsConfused = false;
        ConfusionTurnsRemaining = 0;
        ChoiceLockedMove = null;
        DisabledMoveKey = null;
        DisabledTurnsRemaining = 0;
        FlashFireActive = false;
        TurnsOnField = 0;
        ReflectTurnsRemaining = 0;
        LightScreenTurnsRemaining = 0;
        AuroraVeilTurnsRemaining = 0;
        IsAlternateForm = false;
        IsProtected = false;
        ProtectionStreak = 0;
        ActiveProtectionMoveKey = null;
        ClearRampage();
        PendingMoveKey = null;
        // Future Sight/Doom Desire remain pending when their user switches out.
        // The immediate charge move below is cancelled by switching.
        MustRecharge = false;
        IsSemiInvulnerable = false;
        WasDamagedThisTurn = false;
        LastDamageTakenAmountThisTurn = 0;
        LastDamageTakenWasSpecialThisTurn = false;
        LastDamageSourceThisTurn = null;
        LastHitBlockedBySubstitute = false;
        SubstituteHp = 0;
        IsBadlyPoisoned = false;
        ToxicTurns = 0;
        LeechSeeded = false;
        LeechSeedSource = null;
        Ingrained = false;
        BindingMoveKey = null;
        BindingTurnsRemaining = 0;
        YawnTurnsRemaining = 0;
        PerishTurnsRemaining = 0;
        HealBlockTurnsRemaining = 0;
        TauntTurnsRemaining = 0;
        TormentTurnsRemaining = 0;
        ThroatChopTurnsRemaining = 0;
        EmbargoTurnsRemaining = 0;
        EncoreMoveKey = null;
        EncoreTurnsRemaining = 0;
        ImprisonedMoveKeys.Clear();
        IsInfatuated = false;
        UproarActive = false;
        UproarTurnsRemaining = 0;
        NightmareActive = false;
        ChargeBoostActive = false;
        TypeImmunityRevealed = false;
        LastMoveKey = null;
        StockpileCount = 0;
        RageActive = false;
        HasHoneyGathered = false;
        IsIllusionActive = false;
        WasIllusionBroken = false;
    }

    public bool CanChangeStage(
        string stat,
        int delta,
        bool causedByOpponent = false,
        Pokemon? opponent = null)
    {
        if (!StatStages.ContainsKey(stat) || delta == 0) return false;
        bool abilitySuppressed = IsAbilitySuppressedBy(opponent);
        if (causedByOpponent && delta < 0 && HasActiveHeldItem() && HeldItem == "클리어아뮬렛")
            return false;
        if (causedByOpponent && delta < 0 && !abilitySuppressed
            && stat == "accuracy" && SelectedAbility == "날카로운눈")
            return false;
        if (causedByOpponent && delta < 0 && !abilitySuppressed
            && (SelectedAbility is "클리어바디" or "하얀연기")) return false;
        if (causedByOpponent && delta < 0 && !abilitySuppressed
            && SelectedAbility == "괴력집게" && stat == "attack") return false;
        if (causedByOpponent && delta < 0 && !abilitySuppressed
            && SelectedAbility == "부풀린가슴" && stat == "defense") return false;
        if (causedByOpponent && delta < 0 && !abilitySuppressed
            && SelectedAbility == "플라워베일"
            && HasType(PokemonType.Grass)) return false;
        return true;
    }

    public void ChangeStage(
        string stat,
        int delta,
        bool causedByOpponent = false,
        Pokemon? opponent = null)
    {
        if (!CanChangeStage(stat, delta, causedByOpponent, opponent)) return;
        if (!IsAbilitySuppressedBy(opponent) && SelectedAbility == "심술꾸러기") delta = -delta;
        if (!IsAbilitySuppressedBy(opponent) && SelectedAbility == "단순") delta *= 2;
        StatStages[stat] = Math.Clamp(StatStages[stat] + delta, -6, 6);
    }

    public bool TryRestoreWithWhiteHerb()
    {
        if (!HasActiveHeldItem() || HeldItem != "하얀허브"
            || !StatStages.Values.Any(stage => stage < 0))
        {
            return false;
        }

        foreach (string stageKey in StatStages.Keys.ToArray())
        {
            if (StatStages[stageKey] < 0) StatStages[stageKey] = 0;
        }

        HeldItem = "없음";
        return true;
    }

    public void AdvanceTurn()
    {
        TurnsOnField++;
        IsProtected = false;
        ActiveProtectionMoveKey = null;
        Flinched = false;
        WasDamagedThisTurn = false;
        if (DisabledTurnsRemaining > 0 && --DisabledTurnsRemaining == 0) DisabledMoveKey = null;
        if (HealBlockTurnsRemaining > 0) HealBlockTurnsRemaining--;
        if (TauntTurnsRemaining > 0) TauntTurnsRemaining--;
        if (TormentTurnsRemaining > 0) TormentTurnsRemaining--;
        if (ThroatChopTurnsRemaining > 0) ThroatChopTurnsRemaining--;
        if (EmbargoTurnsRemaining > 0) EmbargoTurnsRemaining--;
        if (EncoreTurnsRemaining > 0 && --EncoreTurnsRemaining == 0) EncoreMoveKey = null;
        if (BindingTurnsRemaining > 0 && --BindingTurnsRemaining == 0) BindingMoveKey = null;
        if (YawnTurnsRemaining > 0) YawnTurnsRemaining--;
        if (UproarTurnsRemaining > 0 && --UproarTurnsRemaining == 0) UproarActive = false;
        if (PendingDelayedAttackKey != null && PendingDelayedAttackTurns > 0)
            PendingDelayedAttackTurns--;
        if (ReflectTurnsRemaining > 0) ReflectTurnsRemaining--;
        if (LightScreenTurnsRemaining > 0) LightScreenTurnsRemaining--;
        if (AuroraVeilTurnsRemaining > 0) AuroraVeilTurnsRemaining--;
        LastDamageTakenAmountThisTurn = 0;
        LastDamageTakenWasSpecialThisTurn = false;
        LastDamageSourceThisTurn = null;
        LastHitBlockedBySubstitute = false;
    }

    public void ResetFieldCounter() => TurnsOnField = 0;

    public bool ShouldSkipTurn => SelectedAbility == "게으름" && TurnsOnField % 2 == 1;

    public string? TriggerStatDropAbility(Pokemon? opponent = null)
    {
        if (IsAbilitySuppressedBy(opponent)) return null;
        if (SelectedAbility == "오기")
        {
            ChangeStage("attack", 2);
            return $"{Data.Name}의 오기로 공격이 크게 올랐다!";
        }
        if (SelectedAbility == "승기")
        {
            ChangeStage("special-attack", 2);
            return $"{Data.Name}의 승기로 특공이 크게 올랐다!";
        }
        return null;
    }

    public bool IsImmuneToAilment(string ailmentName, Pokemon? opponent = null)
    {
        if (ailmentName == "toxic") ailmentName = "poison";
        if (ailmentName == "sleep" && UproarActive) return true;
        if (BattleField.Current is BattleField.Electric or BattleField.Misty)
        {
            if (ailmentName is "sleep" or "poison" or "paralysis" or "burn" or "freeze")
            {
                return true;
            }
        }
        if (!IsAbilitySuppressedBy(opponent))
        {
            if (ailmentName == "sleep" && HasActiveAbility("불면", opponent)) return true;
            if (ailmentName == "sleep" && HasActiveAbility("의기양양", opponent)) return true;
            if (ailmentName == "sleep" && HasActiveAbility("스위트베일", opponent)) return true;
            if (ailmentName == "poison" && HasActiveAbility("면역", opponent)) return true;
            if (ailmentName == "paralysis" && HasActiveAbility("유연", opponent)) return true;
            if (ailmentName == "burn" && HasActiveAbility("수의베일", opponent)) return true;
            if (ailmentName == "freeze" && HasActiveAbility("마그마의무장", opponent)) return true;
        }
        if (!BattleWeather.AreEffectsSuppressed(this, opponent)
            && BattleWeather.Current == "쾌청" && HasActiveAbility("리프가드", opponent)) return true;
        return false;
    }
    public bool IsImmuneToConfusion(Pokemon? opponent = null) =>
        HasActiveAbility("마이페이스", opponent)
        || BattleField.Current == BattleField.Misty;

    public bool IsImmuneToMentalEffect(string effectName, Pokemon? opponent = null) =>
        (HasActiveAbility("둔감", opponent)
            && (effectName is "infatuation" or "attract" or "taunt"))
        || (HasActiveAbility("아로마베일", opponent)
            && (effectName is "disable" or "encore" or "heal-block" or "taunt" or "torment"));

    public bool IsAbilitySuppressedBy(Pokemon? attacker) =>
        attacker != null
        && !ReferenceEquals(this, attacker)
        && !attacker.IsFainted
        && (attacker.SelectedAbility is "틀깨기" or "터보블레이즈" or "테라볼티지"
            or "화학변화가스");

    public bool HasSameKnownGenderAs(Pokemon? other) =>
        other != null
        && Gender != PokemonGender.Unknown
        && Gender == other.Gender;

    public bool HasOppositeKnownGenderTo(Pokemon? other) =>
        other != null
        && Gender != PokemonGender.Unknown
        && other.Gender != PokemonGender.Unknown
        && Gender != other.Gender;

    public void ApplyAilment(string ailmentName, Random? random = null, Pokemon? opponent = null)
    {
        if (Status != StatusCondition.None) return;
        if (IsImmuneToAilment(ailmentName, opponent)) return;

        Status = ailmentName switch
        {
            "paralysis" => StatusCondition.Paralysis,
            "poison" => StatusCondition.Poison,
            "burn" => StatusCondition.Burn,
            "sleep" => StatusCondition.Sleep,
            "freeze" => StatusCondition.Freeze,
            "toxic" => StatusCondition.Poison,
            _ => StatusCondition.None
        };
        if (Status == StatusCondition.Poison && ailmentName == "toxic")
        {
            IsBadlyPoisoned = true;
            ToxicTurns = 1;
        }
        if (Status == StatusCondition.Sleep) SleepTurnsRemaining = (random ?? rng).Next(1, 4);
    }

    public void ApplyConfusion(Random? random = null, Pokemon? opponent = null)
    {
        if (IsConfused || IsImmuneToConfusion(opponent)) return;
        IsConfused = true;
        ConfusionTurnsRemaining = (random ?? rng).Next(1, 5);
    }

    public (bool canAct, string? message) CheckActionPrevention(Random? random = null)
    {
        random ??= rng;
        if (IsConfused)
        {
            ConfusionTurnsRemaining--;
            if (ConfusionTurnsRemaining <= 0)
            {
                IsConfused = false;
            }
            else if (random.Next(100) < 33)
            {
                int selfDamage = Math.Max(1, (int)(((2.0 * Level / 5 + 2) * 40 * ((double)Atk / Math.Max(Def, 1))) / 50) + 2);
                CurrentHp = Math.Max(0, CurrentHp - selfDamage);
                if (CurrentHp == 0) MarkFainted();
                return (false, $"{Data.Name}은(는) 혼란해서 자기 자신을 공격했다!");
            }
        }

        if (IsInfatuated && random.Next(100) < 50)
            return (false, $"{Data.Name}은(는) 헤롱헤롱해서 움직일 수 없다!");

        switch (Status)
        {
            case StatusCondition.Sleep:
                SleepTurnsRemaining -= SelectedAbility == "일찍기상" ? 2 : 1;
                if (SleepTurnsRemaining <= 0)
                {
                    Status = StatusCondition.None;
                    return (true, $"{Data.Name}이(가) 잠에서 깼다!");
                }
                return (false, $"{Data.Name}은(는) 잠들어 있다...");

            case StatusCondition.Freeze:
                if (random.Next(100) < 20)
                {
                    Status = StatusCondition.None;
                    return (true, $"{Data.Name}의 얼음이 녹았다!");
                }
                return (false, $"{Data.Name}은(는) 얼어붙어 움직일 수 없다!");

            case StatusCondition.Paralysis:
                if (random.Next(100) < 25)
                {
                    return (false, $"{Data.Name}은(는) 몸이 저려서 움직일 수 없다!");
                }
                return (true, null);

            default:
                return (true, null);
        }
    }

    public string? ApplyEndOfTurnStatusDamage()
    {
        if (IsFainted) return null;
        if (SelectedAbility == "매직가드") return null;

        if (Status == StatusCondition.Poison && SelectedAbility == "포이즌힐")
        {
            int heal = Math.Max(1, MaxHp / 8);
            int before = CurrentHp;
            CurrentHp = Math.Min(MaxHp, CurrentHp + heal);
            return CurrentHp > before ? $"{Data.Name}은(는) 포이즌힐로 HP를 회복했다!" : null;
        }

        if (Status == StatusCondition.Burn)
        {
            int dmg = Math.Max(1, MaxHp / 16);
            CurrentHp = Math.Max(0, CurrentHp - dmg);
            if (CurrentHp == 0) MarkFainted();
            return $"{Data.Name}은(는) 화상으로 데미지를 입었다!";
        }
        if (Status == StatusCondition.Poison)
        {
            int dmg = IsBadlyPoisoned
                ? Math.Max(1, MaxHp * Math.Min(16, Math.Max(1, ToxicTurns++)) / 16)
                : Math.Max(1, MaxHp / 8);
            CurrentHp = Math.Max(0, CurrentHp - dmg);
            if (CurrentHp == 0) MarkFainted();
            return $"{Data.Name}은(는) 독으로 데미지를 입었다!";
        }
        return null;
    }

    public (bool absorbed, string? message) TryAbsorb(PokemonType attackType, string? moveKey = null)
    {
        if (HasActiveAbility("바람타기") && moveKey != null
            && MoveRuleMetadata.IsWindMove(moveKey))
        {
            ChangeStage("attack", 1);
            return (true, $"{Data.Name}은(는) 바람타기로 공격이 올랐다!");
        }
        if (SelectedAbility == "저수" && attackType == PokemonType.Water)
        {
            int heal = MaxHp / 4;
            CurrentHp = Math.Min(MaxHp, CurrentHp + heal);
            return (true, $"{Data.Name}은(는) 저수로 HP를 회복했다!");
        }
        if (SelectedAbility == "축전" && attackType == PokemonType.Electric)
        {
            int heal = MaxHp / 4;
            CurrentHp = Math.Min(MaxHp, CurrentHp + heal);
            return (true, $"{Data.Name}은(는) 축전으로 HP를 회복했다!");
        }
        if (SelectedAbility == "피뢰침" && attackType == PokemonType.Electric)
        {
            ChangeStage("special-attack", 1);
            return (true, $"{Data.Name}은(는) 피뢰침으로 특수공격이 올랐다!");
        }
        if (SelectedAbility == "마중물" && attackType == PokemonType.Water)
        {
            ChangeStage("special-attack", 1);
            return (true, $"{Data.Name}은(는) 마중물로 특수공격이 올랐다!");
        }
        if (SelectedAbility == "전기엔진" && attackType == PokemonType.Electric)
        {
            ChangeStage("speed", 1);
            return (true, $"{Data.Name}은(는) 전기엔진으로 속도가 올라갔다!");
        }
        if (SelectedAbility == "초식" && attackType == PokemonType.Grass)
        {
            ChangeStage("attack", 1);
            return (true, $"{Data.Name}은(는) 초식으로 공격이 올랐다!");
        }
        if (SelectedAbility == "건조피부" && attackType == PokemonType.Water)
        {
            int heal = Math.Max(1, MaxHp / 4);
            CurrentHp = Math.Min(MaxHp, CurrentHp + heal);
            return (true, $"{Data.Name}은(는) 건조피부로 HP를 회복했다!");
        }
        if (SelectedAbility == "타오르는불꽃" && attackType == PokemonType.Fire)
        {
            FlashFireActive = true;
            return (true, $"{Data.Name}은(는) 타오르는불꽃으로 불꽃 기술을 무효화했다!");
        }
        return (false, null);
    }

    public void TakeDamage(
        int rawDamage,
        PokemonType attackType,
        bool isSpecial = false,
        bool isCritical = false,
        PokemonType? secondaryAttackType = null,
        double moveEffectivenessMultiplier = 1.0,
        bool ignoresGroundImmunity = false,
        Pokemon? attacker = null)
    {
        double multiplier = TypeMultiplier(attackType, CurrentType1, attacker);
        if (CurrentType2 != null)
        {
            multiplier *= TypeMultiplier(attackType, CurrentType2.Value, attacker);
        }
        if (secondaryAttackType != null)
        {
            multiplier *= TypeMultiplier(secondaryAttackType.Value, CurrentType1, attacker);
            if (CurrentType2 != null)
                multiplier *= TypeMultiplier(secondaryAttackType.Value, CurrentType2.Value, attacker);
        }
        multiplier *= moveEffectivenessMultiplier;

        if (IsImmuneToMoveType(attackType, attacker)) multiplier = 0;
        if (ignoresGroundImmunity && attackType == PokemonType.Ground
            && HasType(PokemonType.Flying))
        {
            multiplier = CurrentType1 == PokemonType.Flying
                ? 1
                : TypeMultiplier(attackType, CurrentType1, attacker);
            if (CurrentType2 != null && CurrentType2 != PokemonType.Flying)
                multiplier *= TypeMultiplier(attackType, CurrentType2.Value, attacker);
        }
        if (TypeImmunityRevealed && multiplier == 0
            && attackType is PokemonType.Normal or PokemonType.Fighting or PokemonType.Psychic)
        {
            // Foresight/Odor Sleuth/Miracle Eye remove the relevant type
            // immunity, while preserving ordinary resistances.
            multiplier = TypeChart.GetMultiplier(attackType, CurrentType1);
            if (multiplier == 0) multiplier = 1;
            if (CurrentType2 != null)
            {
                double second = TypeMultiplier(attackType, CurrentType2.Value, attacker);
                multiplier *= second == 0 ? 1 : second;
            }
        }

        bool wasFullHp = CurrentHp == MaxHp;
        SurvivedByEndure = false;
        LastHitWasCritical = isCritical;
        LastHitBlockedBySubstitute = false;

        double dmgMultiplier = 1.0;
        bool abilitySuppressed = IsAbilitySuppressedBy(attacker);
        if (!abilitySuppressed && (SelectedAbility is "하드록" or "필터") && multiplier >= 2.0) dmgMultiplier *= 0.75;
        if (!abilitySuppressed && SelectedAbility == "멀티스케일" && wasFullHp) dmgMultiplier *= 0.5;
        if (!abilitySuppressed && SelectedAbility == "두꺼운지방" && attackType is PokemonType.Fire or PokemonType.Ice) dmgMultiplier *= 0.5;
        if (!abilitySuppressed && SelectedAbility == "내열" && attackType == PokemonType.Fire) dmgMultiplier *= 0.5;
        if (!abilitySuppressed && SelectedAbility == "퍼코트" && !isSpecial) dmgMultiplier *= 0.5;
        if (!abilitySuppressed && SelectedAbility == "건조피부" && attackType == PokemonType.Fire) dmgMultiplier *= 1.25;
        if (!isCritical && attacker?.SelectedAbility != "틈새포착" && HasActiveScreen(isSpecial))
            dmgMultiplier *= 0.5;
        if (isCritical) dmgMultiplier *= 1.5;
        if (!abilitySuppressed
            && SelectedAbility == "불가사의부적" && multiplier > 0 && multiplier < 2.0) multiplier = 0;
        LastMultiplier = multiplier;

        int finalDamage = (int)(rawDamage * multiplier * dmgMultiplier);
        if (finalDamage > 0 && SubstituteHp > 0)
        {
            SubstituteHp = Math.Max(0, SubstituteHp - finalDamage);
            finalDamage = 0;
            LastHitBlockedBySubstitute = true;
        }

        CurrentHp -= finalDamage;
        if (finalDamage > 0)
        {
            WasDamagedThisTurn = true;
            LastDamageTakenAmountThisTurn = finalDamage;
            LastDamageTakenWasSpecialThisTurn = isSpecial;
            LastDamageSourceThisTurn = attacker;
            if (HeldItem == "풍선" && HasActiveHeldItem(attacker))
                HeldItem = "없음";
        }
        if (finalDamage > 0) BreakIllusion();
        if (finalDamage > 0 && RageActive) ChangeStage("attack", 1);
        if (CurrentHp <= 0)
        {
            bool sturdySave = ((!abilitySuppressed && SelectedAbility == "옹골참")
                || HeldItem == "기합의띠") && wasFullHp;
            bool focusBandSave = HeldItem == "기합의머리띠" && rng.Next(100) < 10;
            bool endureSave = ActiveProtectionMoveKey == "endure";

            if (sturdySave || focusBandSave || endureSave)
            {
                CurrentHp = 1;
                SurvivedByEndure = true;
            }
            else
            {
                CurrentHp = 0;
                MarkFainted();
            }
        }
    }

    public string? TriggerCriticalHitAbility(Pokemon? opponent = null)
    {
        if (!LastHitWasCritical || IsFainted
            || IsAbilitySuppressedBy(opponent)
            || SelectedAbility != "분노의경혈") return null;

        StatStages["attack"] = 6;
        return $"{Data.Name}의 분노의경혈로 공격이 최고까지 올랐다!";
    }

    public bool IsCriticalImmune(Pokemon? attacker = null) =>
        !IsAbilitySuppressedBy(attacker)
        && SelectedAbility is "조가비갑옷" or "전투무장";

    public bool UpdateFormForMove(string moveKey, bool isStatus)
    {
        if (SelectedAbility != "배틀스위치" || Data.Name != "킬가르도") return false;

        if (isStatus && !MoveRuleMetadata.ChangesToShieldForm(moveKey)) return false;
        bool shouldBeBladeForm = !isStatus;
        if (IsAlternateForm == shouldBeBladeForm) return false;
        IsAlternateForm = shouldBeBladeForm;
        return true;
    }

    public void ActivateProtection()
    {
        IsProtected = true;
        ActiveProtectionMoveKey = "protect";
    }

    public bool TryActivateProtection(Random random)
        => TryActivateProtection("protect", random);

    public bool TryActivateProtection(string moveKey, Random random)
    {
        // Consecutive protection uses rapidly become unreliable. The first
        // attempt always succeeds; the following one succeeds 1/2, then 1/4.
        bool success = ProtectionStreak == 0 || random.Next(1 << Math.Min(ProtectionStreak, 4)) == 0;
        if (success)
        {
            ProtectionStreak++;
            ActiveProtectionMoveKey = moveKey;
            IsProtected = moveKey != "endure";
        }
        else
        {
            ProtectionStreak = 0;
            ActiveProtectionMoveKey = null;
            IsProtected = false;
        }
        return success;
    }

    public void ResetProtectionStreak() => ProtectionStreak = 0;

    public void StartRampage(string moveKey, int totalTurns)
    {
        RampageMoveKey = moveKey;
        RampageTurnsRemaining = Math.Max(1, totalTurns - 1);
    }

    public bool AdvanceRampageTurn()
    {
        if (RampageMoveKey == null) return false;
        RampageTurnsRemaining = Math.Max(0, RampageTurnsRemaining - 1);
        return RampageTurnsRemaining == 0;
    }

    public void ClearRampage()
    {
        RampageMoveKey = null;
        RampageTurnsRemaining = 0;
    }

    public void SetPendingMove(string moveKey, bool semiInvulnerable = false)
    {
        PendingMoveKey = moveKey;
        IsSemiInvulnerable = semiInvulnerable;
    }

    public string? ConsumePendingMove()
    {
        string? key = PendingMoveKey;
        PendingMoveKey = null;
        IsSemiInvulnerable = false;
        return key;
    }

    public void SetPendingDelayedAttack(string moveKey, Pokemon target, int turns)
    {
        PendingDelayedAttackKey = moveKey;
        PendingDelayedTarget = target;
        PendingDelayedAttackTurns = turns;
    }

    public string? ConsumePendingDelayedAttack(out Pokemon? target)
    {
        target = PendingDelayedTarget;
        string? key = PendingDelayedAttackTurns <= 0 ? PendingDelayedAttackKey : null;
        if (key != null)
        {
            PendingDelayedAttackKey = null;
            PendingDelayedAttackTurns = 0;
            PendingDelayedTarget = null;
        }
        return key;
    }

    public void SetMustRecharge() => MustRecharge = true;
    public void ClearRecharge() => MustRecharge = false;
    public void MarkMoveUsed(string moveKey)
    {
        LastMoveKey = moveKey;
        UsedMoveKeys.Add(moveKey);
    }

    public void MarkLeechSeeded(Pokemon? source = null)
    {
        LeechSeeded = true;
        LeechSeedSource = source;
    }
    public void ClearLeechSeed()
    {
        LeechSeeded = false;
        LeechSeedSource = null;
    }
    public void SetIngrained() => Ingrained = true;
    public void SetBinding(string moveKey, int turns)
    {
        BindingMoveKey = moveKey;
        BindingTurnsRemaining = turns;
    }
    public void SetYawn() => YawnTurnsRemaining = 2;
    public void SetPerish(int turns) => PerishTurnsRemaining = turns;
    public void SetHealBlock(int turns) => HealBlockTurnsRemaining = turns;
    public void SetTaunt(int turns) => TauntTurnsRemaining = turns;
    public void SetTorment(int turns) => TormentTurnsRemaining = turns;
    public void SetThroatChop(int turns) => ThroatChopTurnsRemaining = turns;
    public void SetEmbargo(int turns) => EmbargoTurnsRemaining = turns;
    public void SetEncore(string moveKey, int turns)
    {
        EncoreMoveKey = moveKey;
        EncoreTurnsRemaining = turns;
    }
    public void AddImprisonedMoves(IEnumerable<string> moveKeys) =>
        ImprisonedMoveKeys.UnionWith(moveKeys);
    public void SetInfatuated() => IsInfatuated = true;
    public void SetUproar()
    {
        UproarActive = true;
        UproarTurnsRemaining = 3;
    }
    public void SetNightmare() => NightmareActive = true;
    public void SetChargeBoost() => ChargeBoostActive = true;
    public void ClearChargeBoost() => ChargeBoostActive = false;
    public void RevealTypeImmunity() => TypeImmunityRevealed = true;
    public void SetReflect(int turns) => ReflectTurnsRemaining = Math.Max(0, turns);
    public void SetLightScreen(int turns) => LightScreenTurnsRemaining = Math.Max(0, turns);
    public void SetAuroraVeil(int turns) => AuroraVeilTurnsRemaining = Math.Max(0, turns);
    public void ClearScreens()
    {
        ReflectTurnsRemaining = 0;
        LightScreenTurnsRemaining = 0;
        AuroraVeilTurnsRemaining = 0;
    }
    public bool HasActiveScreen(bool isSpecial) =>
        ReflectTurnsRemaining > 0 && !isSpecial
        || LightScreenTurnsRemaining > 0 && isSpecial
        || AuroraVeilTurnsRemaining > 0;
    public bool TryStockpile()
    {
        if (StockpileCount >= 3) return false;
        StockpileCount++;
        return true;
    }
    public int ConsumeStockpile()
    {
        int count = StockpileCount;
        StockpileCount = 0;
        return count;
    }
    public void SetRage() => RageActive = true;
    public void ClearBinding()
    {
        BindingMoveKey = null;
        BindingTurnsRemaining = 0;
    }
    public void MarkFainted()
    {
        CurrentHp = 0;
        IsFainted = true;
        ClearRampage();
        SubstituteHp = 0;
    }

    public bool TryCreateSubstitute()
    {
        if (IsFainted || HasSubstitute) return false;
        int cost = Math.Max(1, MaxHp / 4);
        if (CurrentHp <= cost) return false;
        CurrentHp -= cost;
        SubstituteHp = cost;
        return true;
    }

    public void ClearSubstitute() => SubstituteHp = 0;

    public void ApplyDirectDamage(int damage, Pokemon? source = null, bool isSpecial = false)
    {
        LastHitBlockedBySubstitute = false;
        int actualDamage = Math.Max(0, damage);
        if (actualDamage <= 0 || IsFainted) return;
        CurrentHp = Math.Max(0, CurrentHp - actualDamage);
        WasDamagedThisTurn = true;
        LastDamageTakenAmountThisTurn = actualDamage;
        LastDamageTakenWasSpecialThisTurn = isSpecial;
        LastDamageSourceThisTurn = source;
        if (CurrentHp == 0) MarkFainted();
    }

    public void ClearStatStages()
    {
        foreach (var stat in StatStages.Keys.ToList()) StatStages[stat] = 0;
    }

    public bool UpdateFormAtEndOfTurn()
    {
        if (SelectedAbility != "달마모드" || Data.Name != "불비달마") return false;

        bool shouldBeAlternateForm = CurrentHp <= MaxHp / 2;
        if (IsAlternateForm == shouldBeAlternateForm) return false;
        IsAlternateForm = shouldBeAlternateForm;
        return true;
    }

    public static PokemonType? GetPlateType(string itemName) => itemName switch
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
        _ => null
    };

    public bool TryChangeTypeForMove(PokemonType moveType)
    {
        if (SelectedAbility != "변환자재"
            || (CurrentType1 == moveType && CurrentType2 == null)) return false;

        typeOverride1 = moveType;
        typeOverride2 = null;
        return true;
    }

    public bool TryChangeTypeFromHit(PokemonType moveType)
    {
        if (SelectedAbility != "변색"
            || (CurrentType1 == moveType && CurrentType2 == null)) return false;

        typeOverride1 = moveType;
        typeOverride2 = null;
        return true;
    }

    public bool TryTransformInto(Pokemon target)
    {
        if (SelectedAbility != "괴짜" || IsTransformed || ReferenceEquals(this, target)) return false;

        preTransformPP = new Dictionary<string, int>(CurrentPP);
        Data = target.Data;
        IsAlternateForm = target.IsAlternateForm;
        typeOverride1 = target.CurrentType1;
        typeOverride2 = target.CurrentType2;
        IsTransformed = true;
        CurrentPP = new Dictionary<string, int>(target.CurrentPP);
        UsedMoveKeys.Clear();
        ChoiceLockedMove = null;
        return true;
    }

    public bool TryActivateIllusion(Pokemon disguise)
    {
        if (!HasActiveAbility("일루전") || IsIllusionActive || IsTransformed
            || ReferenceEquals(this, disguise) || disguise.IsFainted) return false;

        Data = disguise.Data;
        IsAlternateForm = disguise.IsAlternateForm;
        typeOverride1 = disguise.CurrentType1;
        typeOverride2 = disguise.CurrentType2;
        IsIllusionActive = true;
        WasIllusionBroken = false;
        return true;
    }

    public bool UpdateWeatherForm(Pokemon? opponent = null)
    {
        if (Data.EnglishName != "castform" || !HasActiveAbility("기분파", opponent)) return false;

        PokemonType nextType = BattleWeather.Current switch
        {
            BattleWeather.Sun => PokemonType.Fire,
            BattleWeather.Rain => PokemonType.Water,
            BattleWeather.Hail => PokemonType.Ice,
            _ => PokemonType.Normal
        };
        bool nextAlternate = nextType != PokemonType.Normal;
        if (CurrentType1 == nextType && IsAlternateForm == nextAlternate) return false;
        typeOverride1 = nextType == PokemonType.Normal ? null : nextType;
        typeOverride2 = null;
        IsAlternateForm = nextAlternate;
        return true;
    }

    public bool BreakIllusion()
    {
        if (!IsIllusionActive) return false;
        Data = originalData;
        IsAlternateForm = false;
        typeOverride1 = null;
        typeOverride2 = null;
        IsIllusionActive = false;
        WasIllusionBroken = true;
        return true;
    }

    public static bool IsBerry(string itemName) =>
        itemName is "오랭열매" or "자뭉열매" or "무화열매" or "리샘열매";

    public bool IsReadyToConsumeBerry()
    {
        if (IsFainted || EmbargoTurnsRemaining > 0 || !IsBerry(HeldItem)
            || !HasActiveHeldItem()) return false;

        return HeldItem switch
        {
            "리샘열매" => Status != StatusCondition.None,
            "무화열매" => CurrentHp <= MaxHp / (SelectedAbility == "먹보" ? 2 : 4),
            _ => CurrentHp <= MaxHp / 2
        };
    }

    public bool IsBerryConsumptionBlockedBy(Pokemon? opponent) =>
        IsReadyToConsumeBerry()
        && opponent != null
        && !opponent.IsFainted
        && opponent.SelectedAbility == "긴장감";

    public bool IsBerryEatingBlockedBy(Pokemon? opponent) =>
        opponent != null
            && !opponent.IsFainted
            && opponent.HasActiveAbility("긴장감", this);

    public bool TryConsumeBerry(out string? message)
        => TryConsumeBerry(null, out message);

    public bool TryConsumeBerry(Pokemon? opponent, out string? message)
    {
        message = null;
        if (!IsReadyToConsumeBerry() || IsBerryConsumptionBlockedBy(opponent)) return false;

        string berry = HeldItem;
        HeldItem = "없음";
        HasConsumedBerry = true;
        ConsumedBerryName = berry;
        ApplyBerryEffect(berry);
        message = $"{Data.Name}은(는) {berry}을(를) 먹었다!";
        return true;
    }

    public bool TryTakeHeldBerry(out string? berryName)
    {
        berryName = null;
        if (!IsBerry(HeldItem)) return false;

        string berry = HeldItem;
        HeldItem = "없음";
        berryName = berry;
        return true;
    }

    public void ApplyBerryEffect(string berry)
    {
        HasConsumedBerry = true;
        ConsumedBerryName = berry;
        if (berry == "오랭열매")
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + 10);
        }
        else if (berry == "자뭉열매")
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + Math.Max(1, MaxHp / 4));
        }
        else if (berry == "무화열매")
        {
            CurrentHp = Math.Min(MaxHp, CurrentHp + Math.Max(1, MaxHp / 3));
        }
        else if (berry == "리샘열매")
        {
            ClearPrimaryStatus();
        }
    }

    public bool TryHarvest(Random random, Pokemon? opponent, out string? message)
    {
        message = null;
        if (!HasActiveAbility("수확", opponent)
            || IsFainted
            || HeldItem != "없음"
            || !HasConsumedBerry
            || string.IsNullOrEmpty(ConsumedBerryName))
        {
            return false;
        }

        bool sunnyHarvest = !BattleWeather.AreEffectsSuppressed(this, opponent)
            && BattleWeather.Current == "쾌청";
        if (!sunnyHarvest && random.Next(100) >= 50) return false;

        HeldItem = ConsumedBerryName;
        message = $"{Data.Name}의 수확으로 {HeldItem}이(가) 되돌아왔다!";
        return true;
    }

    public bool TryPickUp(Random random, out string? message)
    {
        message = null;
        if (SelectedAbility != "픽업" || IsFainted || HeldItem != "없음") return false;

        // Pickup resolves once after a battle. A resolved attempt is represented by
        // HasPickedUpItem so repeated lifecycle calls cannot grant duplicate items.
        if (HasPickedUpItem) return false;
        HasPickedUpItem = true;
        if (random.Next(100) >= 10) return false;

        var availableItems = ItemDatabase.GeneralItems
            .Where(item => item.Name != "없음")
            .ToArray();
        if (availableItems.Length == 0) return false;

        HeldItem = availableItems[random.Next(availableItems.Length)].Name;
        message = $"{Data.Name}의 픽업으로 {HeldItem}을(를) 주웠다!";
        return true;
    }

    public bool TryHoneyGather(Random random, out string? message)
    {
        message = null;
        if (SelectedAbility != "꿀모으기" || IsFainted || HeldItem != "없음" || HasHoneyGathered)
            return false;

        HasHoneyGathered = true;
        if (random.Next(100) >= 10) return false;
        HeldItem = "달콤한꿀";
        message = $"{Data.Name}의 꿀모으기로 달콤한꿀을 모았다!";
        return true;
    }

    //철가시: 물리 접촉기로 나를 때린 공격자가 자기 최대HP의 1/8만큼 반사 데미지를 입음
    public int? TryReflectDamage(bool moveMakesContact, Pokemon? attacker = null)
    {
        if (moveMakesContact && !IsFainted
            && !IsAbilitySuppressedBy(attacker)
            && (HasActiveAbility("철가시", attacker) || HasActiveAbility("까칠한피부", attacker)))
        {
            return Math.Max(1, MaxHp / 8);
        }
        return null;
    }

    public bool IsImmuneToMoveType(PokemonType attackType, Pokemon? attacker = null)
    {
        if (IsAbilitySuppressedBy(attacker)) return false;
        bool gravityGrounds = BattleField.GravityActive;
        return (!gravityGrounds && HasActiveAbility("부유", attacker) && attackType == PokemonType.Ground)
            || (!gravityGrounds && HasActiveHeldItem(attacker) && HeldItem == "풍선" && attackType == PokemonType.Ground)
            || ((HasActiveAbility("피뢰침", attacker) || HasActiveAbility("축전", attacker)
                || HasActiveAbility("전기엔진", attacker)) && attackType == PokemonType.Electric)
            || ((HasActiveAbility("저수", attacker) || HasActiveAbility("건조피부", attacker))
                && attackType == PokemonType.Water)
            || (HasActiveAbility("마중물", attacker) && attackType == PokemonType.Water)
            || (HasActiveAbility("타오르는불꽃", attacker) && attackType == PokemonType.Fire)
            || (HasActiveAbility("초식", attacker) && attackType == PokemonType.Grass);
    }

    public bool IsGrounded(Pokemon? opponent = null) =>
        BattleField.GravityActive
        || (!HasType(PokemonType.Flying)
            && !HasActiveAbility("부유", opponent)
            && !(HasActiveHeldItem(opponent) && HeldItem == "풍선"));

    public bool IsImmuneToWindMove(string moveKey, Pokemon? attacker = null) =>
        HasActiveAbility("바람타기", attacker) && MoveRuleMetadata.IsWindMove(moveKey);

    private static double TypeMultiplier(
        PokemonType attackType,
        PokemonType defendType,
        Pokemon? attacker)
    {
        if (attacker?.SelectedAbility == "배짱"
            && attackType is PokemonType.Normal or PokemonType.Fighting
            && defendType == PokemonType.Ghost)
        {
            return 1.0;
        }

        return TypeChart.GetMultiplier(attackType, defendType);
    }

    public PokemonType ResolveMoveType(Move move, Pokemon? opponent = null)
    {
        string key = move.Name switch
        {
            "웨더볼" => "weather-ball",
            "심판의뭉치" => "judgment",
            "테크노버스터" => "techno-blast",
            _ => ""
        };
        return MoveRuleMetadata.ResolveMoveType(key, move, this, opponent);
    }

    public void DisableMove(string moveName)
    {
        DisabledMoveKey = moveName;
        DisabledTurnsRemaining = 5;
    }

    public void ClearPrimaryStatus()
    {
        Status = StatusCondition.None;
        IsBadlyPoisoned = false;
        ToxicTurns = 0;
        SleepTurnsRemaining = 0;
    }
}
