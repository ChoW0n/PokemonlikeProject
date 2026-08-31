namespace PokemonBattle.Models;

public class Pokemon
{
    public PokemonData Data;
    public int Level;
    public int CurrentHp;
    public bool IsFainted;
    public double LastMultiplier;
    public bool SurvivedByEndure;
    public Dictionary<string, int> CurrentPP = new();
    public string SelectedAbility = "";
    public string HeldItem = "없음";
    public string? ChoiceLockedMove { get; private set; }
    public string? DisabledMoveKey { get; private set; }
    public int DisabledTurnsRemaining { get; private set; }
    public bool FlashFireActive { get; private set; }
    public int TurnsOnField { get; private set; }
    public bool IsAlternateForm { get; private set; }
    public bool HasConsumedBerry { get; private set; }
    public bool LastHitWasCritical { get; private set; }
    public bool IsProtected { get; private set; }

    public StatusCondition Status = StatusCondition.None;
    public int SleepTurnsRemaining;
    public bool IsConfused;
    public int ConfusionTurnsRemaining;
    public bool Flinched;
    public Dictionary<string, int> StatStages = new()
    {
        ["attack"] = 0, ["defense"] = 0, ["special-attack"] = 0, ["special-defense"] = 0, ["speed"] = 0
    };

    private static readonly Random rng = new Random();

    private int StatHp(int baseStat) => (2 * baseStat + 31) * Level / 100 + Level + 10;
    private int StatOther(int baseStat) => (2 * baseStat + 31) * Level / 100 + 5;

    public int MaxHp => StatHp(Data.BaseHp);

    private double ItemStatMult(string stat)
    {
        bool isPikachu = Data.Name == "피카츄";
        bool isMarowak = Data.Name == "텅구리";
        bool isLatiosLatias = Data.Name == "라티오스" || Data.Name == "라티아스";

        if (HeldItem == "전기구슬" && isPikachu && (stat == "attack" || stat == "special-attack")) return 2.0;
        if (HeldItem == "두꺼운뼈" && isMarowak && stat == "attack") return 2.0;
        if (HeldItem == "이슬의구슬" && isLatiosLatias && (stat == "special-attack" || stat == "special-defense")) return 1.5;
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

    public int EffectiveAtk
    {
        get
        {
            double value = Atk * StageMult("attack");
            if (Status == StatusCondition.Burn && SelectedAbility != "근성") value *= 0.5;
            if (SelectedAbility == "근성" && Status != StatusCondition.None) value *= 1.5;
            if (SelectedAbility is "의욕") value *= 1.5;
            if (SelectedAbility is "천하장사" or "순수한힘") value *= 2.0;
            if (SelectedAbility == "슬로스타트" && TurnsOnField < 5) value *= 0.5;
            return (int)value;
        }
    }

    public int EffectiveDef
    {
        get
        {
            double value = Def * StageMult("defense");
            if (SelectedAbility == "풀모피" && BattleField.Current == BattleField.Grassy) value *= 2.0;
            if (SelectedAbility == "이상한비늘" && Status != StatusCondition.None) value *= 1.5;
            return (int)value;
        }
    }

    public int EffectiveSpAtk
    {
        get
        {
            double value = SpAtk * StageMult("special-attack");
            if (SelectedAbility is "플러스" or "마이너스") value *= 1.5;
            if (SelectedAbility == "선파워" && BattleWeather.Current == "쾌청") value *= 1.5;
            return (int)value;
        }
    }
    public int EffectiveSpDef => (int)(SpDef * StageMult("special-defense"));

    //엽록소: 쾌청 날씨에서 속도 2배
    public int EffectiveSpd
    {
        get
        {
            double spd = Spd * StageMult("speed");
            if (Status == StatusCondition.Paralysis && SelectedAbility != "속보") spd *= 0.5;
            if (SelectedAbility == "엽록소" && BattleWeather.Current == "쾌청") spd *= 2.0;
            if (SelectedAbility is "쓱쓱" && BattleWeather.Current == "비") spd *= 2.0;
            if (SelectedAbility is "모래헤치기" && BattleWeather.Current == "모래바람") spd *= 2.0;
            if (SelectedAbility is "눈치우기" && BattleWeather.Current == "싸라기눈") spd *= 2.0;
            if (SelectedAbility == "속보" && Status != StatusCondition.None) spd *= 1.5;
            if (SelectedAbility == "슬로스타트" && TurnsOnField < 5) spd *= 0.5;
            return (int)spd;
        }
    }

    public Pokemon(PokemonData data, List<string>? chosenMoves = null, string ability = "", string item = "없음", int level = 1)
    {
        Data = data;
        Level = level;
        CurrentHp = MaxHp;
        IsFainted = false;
        SelectedAbility = ability;
        HeldItem = item;

        var moveList = chosenMoves != null && chosenMoves.Count > 0 ? chosenMoves : data.MoveNames.ToList();
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

    private bool IsChoiceItem =>
        HeldItem == "구애스카프" || HeldItem == "구애머리띠" || HeldItem == "구애안경";

    public bool CanUseMove(string moveName)
    {
        if (!CurrentPP.TryGetValue(moveName, out var pp) || pp <= 0) return false;
        if (DisabledMoveKey == moveName) return false;
        if (IsChoiceItem && ChoiceLockedMove != null && ChoiceLockedMove != moveName) return false;
        if (moveName == "belch" && !HasConsumedBerry) return false;
        return true;
    }

    public bool TryUseMove(string moveName)
    {
        if (!CanUseMove(moveName)) return false;
        CurrentPP[moveName]--;
        if (IsChoiceItem) ChoiceLockedMove ??= moveName;
        return true;
    }

    public void ResetOnSwitchOut()
    {
        StatStages = new() { ["attack"] = 0, ["defense"] = 0, ["special-attack"] = 0, ["special-defense"] = 0, ["speed"] = 0 };
        IsConfused = false;
        ConfusionTurnsRemaining = 0;
        ChoiceLockedMove = null;
        DisabledMoveKey = null;
        DisabledTurnsRemaining = 0;
        FlashFireActive = false;
        TurnsOnField = 0;
        IsAlternateForm = false;
        IsProtected = false;
    }

    public bool CanChangeStage(string stat, int delta, bool causedByOpponent = false)
    {
        if (!StatStages.ContainsKey(stat) || delta == 0) return false;
        if (causedByOpponent && delta < 0 && (SelectedAbility is "클리어바디" or "하얀연기")) return false;
        if (causedByOpponent && delta < 0 && SelectedAbility == "괴력집게" && stat == "attack") return false;
        if (causedByOpponent && delta < 0 && SelectedAbility == "부풀린가슴" && stat == "defense") return false;
        return true;
    }

    public void ChangeStage(string stat, int delta, bool causedByOpponent = false)
    {
        if (!CanChangeStage(stat, delta, causedByOpponent)) return;
        if (SelectedAbility == "심술꾸러기") delta = -delta;
        if (SelectedAbility == "단순") delta *= 2;
        StatStages[stat] = Math.Clamp(StatStages[stat] + delta, -6, 6);
    }

    public void AdvanceTurn()
    {
        TurnsOnField++;
        IsProtected = false;
        if (DisabledTurnsRemaining > 0 && --DisabledTurnsRemaining == 0) DisabledMoveKey = null;
    }

    public void ResetFieldCounter() => TurnsOnField = 0;

    public bool ShouldSkipTurn => SelectedAbility == "게으름" && TurnsOnField % 2 == 1;

    public string? TriggerStatDropAbility()
    {
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

    public bool IsImmuneToAilment(string ailmentName)
    {
        if (BattleField.Current is BattleField.Electric or BattleField.Misty)
        {
            if (ailmentName is "sleep" or "poison" or "paralysis" or "burn" or "freeze")
            {
                return true;
            }
        }
        if (ailmentName == "sleep" && (SelectedAbility is "불면" or "의기양양")) return true;
        if (ailmentName == "poison" && SelectedAbility == "면역") return true;
        if (ailmentName == "paralysis" && SelectedAbility == "유연") return true;
        if (ailmentName == "burn" && SelectedAbility == "수의베일") return true;
        if (ailmentName == "freeze" && SelectedAbility == "마그마의무장") return true;
        if (BattleWeather.Current == "쾌청" && SelectedAbility == "리프가드") return true;
        return false;
    }
    public bool IsImmuneToConfusion() =>
        SelectedAbility == "마이페이스" || BattleField.Current == BattleField.Misty;

    public void ApplyAilment(string ailmentName)
    {
        if (Status != StatusCondition.None) return;
        if (IsImmuneToAilment(ailmentName)) return;

        Status = ailmentName switch
        {
            "paralysis" => StatusCondition.Paralysis,
            "poison" => StatusCondition.Poison,
            "burn" => StatusCondition.Burn,
            "sleep" => StatusCondition.Sleep,
            "freeze" => StatusCondition.Freeze,
            _ => StatusCondition.None
        };
        if (Status == StatusCondition.Sleep) SleepTurnsRemaining = rng.Next(1, 4);
    }

    public void ApplyConfusion()
    {
        if (IsConfused || IsImmuneToConfusion()) return;
        IsConfused = true;
        ConfusionTurnsRemaining = rng.Next(1, 5);
    }

    public (bool canAct, string? message) CheckActionPrevention()
    {
        if (IsConfused)
        {
            ConfusionTurnsRemaining--;
            if (ConfusionTurnsRemaining <= 0)
            {
                IsConfused = false;
            }
            else if (rng.Next(100) < 33)
            {
                int selfDamage = Math.Max(1, (int)(((2.0 * Level / 5 + 2) * 40 * ((double)Atk / Math.Max(Def, 1))) / 50) + 2);
                CurrentHp = Math.Max(0, CurrentHp - selfDamage);
                if (CurrentHp == 0) IsFainted = true;
                return (false, $"{Data.Name}은(는) 혼란해서 자기 자신을 공격했다!");
            }
        }

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
                if (rng.Next(100) < 20)
                {
                    Status = StatusCondition.None;
                    return (true, $"{Data.Name}의 얼음이 녹았다!");
                }
                return (false, $"{Data.Name}은(는) 얼어붙어 움직일 수 없다!");

            case StatusCondition.Paralysis:
                if (rng.Next(100) < 25)
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
            if (CurrentHp == 0) IsFainted = true;
            return $"{Data.Name}은(는) 화상으로 데미지를 입었다!";
        }
        if (Status == StatusCondition.Poison)
        {
            int dmg = Math.Max(1, MaxHp / 8);
            CurrentHp = Math.Max(0, CurrentHp - dmg);
            if (CurrentHp == 0) IsFainted = true;
            return $"{Data.Name}은(는) 독으로 데미지를 입었다!";
        }
        return null;
    }

    public (bool absorbed, string? message) TryAbsorb(PokemonType attackType)
    {
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

    public void TakeDamage(int rawDamage, PokemonType attackType, bool isSpecial = false, bool isCritical = false)
    {
        double multiplier = TypeChart.GetMultiplier(attackType, Data.Type1);
        if (Data.Type2 != null)
        {
            multiplier *= TypeChart.GetMultiplier(attackType, Data.Type2.Value);
        }

        if (IsImmuneToMoveType(attackType)) multiplier = 0;

        bool wasFullHp = CurrentHp == MaxHp;
        SurvivedByEndure = false;
        LastHitWasCritical = isCritical;

        double dmgMultiplier = 1.0;
        if ((SelectedAbility is "하드록" or "필터") && multiplier >= 2.0) dmgMultiplier *= 0.75;
        if (SelectedAbility == "멀티스케일" && wasFullHp) dmgMultiplier *= 0.5;
        if (SelectedAbility == "두꺼운지방" && attackType is PokemonType.Fire or PokemonType.Ice) dmgMultiplier *= 0.5;
        if (SelectedAbility == "내열" && attackType == PokemonType.Fire) dmgMultiplier *= 0.5;
        if (SelectedAbility == "퍼코트" && !isSpecial) dmgMultiplier *= 0.5;
        if (SelectedAbility == "건조피부" && attackType == PokemonType.Fire) dmgMultiplier *= 1.25;
        if (isCritical) dmgMultiplier *= 1.5;
        if (SelectedAbility == "불가사의부적" && multiplier > 0 && multiplier < 2.0) multiplier = 0;
        LastMultiplier = multiplier;

        int finalDamage = (int)(rawDamage * multiplier * dmgMultiplier);

        CurrentHp -= finalDamage;
        if (CurrentHp <= 0)
        {
            bool sturdySave = (SelectedAbility == "옹골참" || HeldItem == "기합의띠") && wasFullHp;
            bool focusBandSave = HeldItem == "기합의머리띠" && rng.Next(100) < 10;

            if (sturdySave || focusBandSave)
            {
                CurrentHp = 1;
                SurvivedByEndure = true;
            }
            else
            {
                CurrentHp = 0;
                IsFainted = true;
            }
        }
    }

    public string? TriggerCriticalHitAbility()
    {
        if (!LastHitWasCritical || IsFainted || SelectedAbility != "분노의경혈") return null;

        StatStages["attack"] = 6;
        return $"{Data.Name}의 분노의경혈로 공격이 최고까지 올랐다!";
    }

    public bool IsCriticalImmune() => SelectedAbility is "조가비갑옷" or "전투무장";

    public bool UpdateFormForMove(string moveKey, bool isStatus)
    {
        if (SelectedAbility != "배틀스위치" || Data.Name != "킬가르도") return false;

        if (isStatus && !MoveRuleMetadata.ChangesToShieldForm(moveKey)) return false;
        bool shouldBeBladeForm = !isStatus;
        if (IsAlternateForm == shouldBeBladeForm) return false;
        IsAlternateForm = shouldBeBladeForm;
        return true;
    }

    public void ActivateProtection() => IsProtected = true;

    public bool UpdateFormAtEndOfTurn()
    {
        if (SelectedAbility != "달마모드" || Data.Name != "불비달마") return false;

        bool shouldBeAlternateForm = CurrentHp <= MaxHp / 2;
        if (IsAlternateForm == shouldBeAlternateForm) return false;
        IsAlternateForm = shouldBeAlternateForm;
        return true;
    }

    public static bool IsBerry(string itemName) =>
        itemName is "오랭열매" or "자뭉열매" or "무화열매" or "리샘열매";

    public bool TryConsumeBerry(out string? message)
    {
        message = null;
        if (!IsBerry(HeldItem)) return false;

        bool shouldEat = HeldItem switch
        {
            "리샘열매" => Status != StatusCondition.None,
            "무화열매" => CurrentHp <= MaxHp / (SelectedAbility == "먹보" ? 2 : 4),
            _ => CurrentHp <= MaxHp / 2
        };
        if (!shouldEat) return false;

        string berry = HeldItem;
        HeldItem = "없음";
        HasConsumedBerry = true;
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

    //철가시: 물리 접촉기로 나를 때린 공격자가 자기 최대HP의 1/8만큼 반사 데미지를 입음
    public int? TryReflectDamage(bool moveMakesContact)
    {
        if (moveMakesContact && !IsFainted && (SelectedAbility is "철가시" or "까칠한피부"))
        {
            return Math.Max(1, MaxHp / 8);
        }
        return null;
    }

    public bool IsImmuneToMoveType(PokemonType attackType)
    {
        return (SelectedAbility == "부유" && attackType == PokemonType.Ground)
            || (SelectedAbility is "피뢰침" or "축전" or "전기엔진" && attackType == PokemonType.Electric)
            || (SelectedAbility is "저수" or "건조피부" && attackType == PokemonType.Water)
            || (SelectedAbility == "마중물" && attackType == PokemonType.Water)
            || (SelectedAbility == "타오르는불꽃" && attackType == PokemonType.Fire)
            || (SelectedAbility == "초식" && attackType == PokemonType.Grass);
    }

    public PokemonType ResolveMoveType(Move move)
    {
        if (SelectedAbility == "노말스킨") return PokemonType.Normal;
        if (move.Type == PokemonType.Normal && SelectedAbility == "프리즈스킨") return PokemonType.Ice;
        if (move.Type == PokemonType.Normal && SelectedAbility == "페어리스킨") return PokemonType.Fairy;
        return move.Name == "웨더볼"
            ? MoveRuleMetadata.ResolveMoveType("weather-ball", move)
            : move.Type;
    }

    public void DisableMove(string moveName)
    {
        DisabledMoveKey = moveName;
        DisabledTurnsRemaining = 5;
    }

    public void ClearPrimaryStatus() => Status = StatusCondition.None;
}
