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
    public bool FlashFireActive { get; private set; }

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

    public int Atk => (int)(StatOther(Data.BaseAtk) * ItemStatMult("attack"));
    public int Def => StatOther(Data.BaseDef);
    public int SpAtk => (int)(StatOther(Data.BaseSpAtk) * ItemStatMult("special-attack"));
    public int SpDef => (int)(StatOther(Data.BaseSpDef) * ItemStatMult("special-defense"));
    public int Spd => StatOther(Data.BaseSpd);

    private double StageMult(string key)
    {
        int stage = StatStages[key];
        return stage >= 0 ? (2.0 + stage) / 2.0 : 2.0 / (2.0 - stage);
    }

    public int EffectiveAtk => (int)(Atk * StageMult("attack") * (Status == StatusCondition.Burn ? 0.5 : 1.0) * (SelectedAbility == "의욕" ? 1.5 : 1.0));
    public int EffectiveDef => (int)(Def * StageMult("defense"));
    public int EffectiveSpAtk => (int)(SpAtk * StageMult("special-attack"));
    public int EffectiveSpDef => (int)(SpDef * StageMult("special-defense"));

    //엽록소: 쾌청 날씨에서 속도 2배
    public int EffectiveSpd
    {
        get
        {
            double spd = Spd * StageMult("speed") * (Status == StatusCondition.Paralysis ? 0.5 : 1.0);
            if (SelectedAbility == "엽록소" && BattleWeather.Current == "쾌청") spd *= 2.0;
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
        if (IsChoiceItem && ChoiceLockedMove != null && ChoiceLockedMove != moveName) return false;
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
        FlashFireActive = false;
    }

    public void ChangeStage(string stat, int delta)
    {
        if (!StatStages.ContainsKey(stat)) return;
        StatStages[stat] = Math.Clamp(StatStages[stat] + delta, -6, 6);
    }

    public bool IsImmuneToAilment(string ailmentName)
    {
        if (ailmentName == "sleep" && (SelectedAbility == "불면" || SelectedAbility == "의기양양")) return true;
        return false;
    }
    public bool IsImmuneToConfusion() => SelectedAbility == "마이페이스";

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
                SleepTurnsRemaining--;
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
        if (SelectedAbility == "타오르는불꽃" && attackType == PokemonType.Fire)
        {
            FlashFireActive = true;
            return (true, $"{Data.Name}은(는) 타오르는불꽃으로 불꽃 기술을 무효화했다!");
        }
        return (false, null);
    }

    public void TakeDamage(int rawDamage, PokemonType attackType)
    {
        double multiplier = TypeChart.GetMultiplier(attackType, Data.Type1);
        if (Data.Type2 != null)
        {
            multiplier *= TypeChart.GetMultiplier(attackType, Data.Type2.Value);
        }

        if (SelectedAbility == "부유" && attackType == PokemonType.Ground) multiplier = 0;
        if (SelectedAbility == "피뢰침" && attackType == PokemonType.Electric) multiplier = 0;

        LastMultiplier = multiplier;

        bool wasFullHp = CurrentHp == MaxHp;
        SurvivedByEndure = false;

        double dmgMultiplier = 1.0;
        if (SelectedAbility == "하드록" && multiplier >= 2.0) dmgMultiplier *= 0.75;
        if (SelectedAbility == "멀티스케일" && wasFullHp) dmgMultiplier *= 0.5;

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

    //철가시: 물리 접촉기로 나를 때린 공격자가 자기 최대HP의 1/8만큼 반사 데미지를 입음
    public int? TryReflectDamage(bool moveMakesContact)
    {
        if (SelectedAbility == "철가시" && moveMakesContact && !IsFainted)
        {
            return Math.Max(1, MaxHp / 8);
        }
        return null;
    }
}
