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
    public int Atk => StatOther(Data.BaseAtk);
    public int Def => StatOther(Data.BaseDef);
    public int SpAtk => StatOther(Data.BaseSpAtk);
    public int SpDef => StatOther(Data.BaseSpDef);
    public int Spd => StatOther(Data.BaseSpd);

    private double StageMult(string key)
    {
        int stage = StatStages[key];
        return stage >= 0 ? (2.0 + stage) / 2.0 : 2.0 / (2.0 - stage);
    }

    public int EffectiveAtk => (int)(Atk * StageMult("attack") * (Status == StatusCondition.Burn ? 0.5 : 1.0));
    public int EffectiveDef => (int)(Def * StageMult("defense"));
    public int EffectiveSpAtk => (int)(SpAtk * StageMult("special-attack"));
    public int EffectiveSpDef => (int)(SpDef * StageMult("special-defense"));
    public int EffectiveSpd => (int)(Spd * StageMult("speed") * (Status == StatusCondition.Paralysis ? 0.5 : 1.0));

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
            //방어 코드: 데이터가 갱신되어 더 이상 존재하지 않는 기술 키는 조용히 건너뜀 (예전엔 여기서 크래시가 났음)
            if (MoveDatabase.All.ContainsKey(moveName))
            {
                CurrentPP[moveName] = MoveDatabase.All[moveName].MaxPP;
            }
        }

        if (CurrentPP.Count == 0) //전부 걸러졌으면 최소 하나는 보장
        {
            var fallback = data.MoveNames.FirstOrDefault(m => MoveDatabase.All.ContainsKey(m));
            if (fallback != null)
            {
                CurrentPP[fallback] = MoveDatabase.All[fallback].MaxPP;
            }
            else if (MoveDatabase.All.ContainsKey("tackle"))
            {
                CurrentPP["tackle"] = MoveDatabase.All["tackle"].MaxPP;
            }
        }
    }

    public bool TryUseMove(string moveName)
    {
        if (CurrentPP[moveName] <= 0) return false;
        CurrentPP[moveName]--;
        return true;
    }

    public void ResetOnSwitchOut()
    {
        StatStages = new() { ["attack"] = 0, ["defense"] = 0, ["special-attack"] = 0, ["special-defense"] = 0, ["speed"] = 0 };
        IsConfused = false;
        ConfusionTurnsRemaining = 0;
    }

    public void ChangeStage(string stat, int delta)
    {
        if (!StatStages.ContainsKey(stat)) return;
        StatStages[stat] = Math.Clamp(StatStages[stat] + delta, -6, 6);
    }

    public void ApplyAilment(string ailmentName)
    {
        if (Status != StatusCondition.None) return;
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
        if (IsConfused) return;
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

        int finalDamage = (int)(rawDamage * multiplier);
        bool wasFullHp = CurrentHp == MaxHp;
        SurvivedByEndure = false;

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
}
