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
    public string Description; //PokeAPI에서 가져온 공식 기술 설명

    public Move(string name, int power, PokemonType type, int maxPp, int accuracy, bool alwaysHits, int priority, bool isStatus, bool isSpecial, string ailmentName, int ailmentChance, int flinchChance, List<StatChangeEntry> statChanges, int statChangeChance, string description)
    {
        Name = name; Power = power; Type = type; MaxPP = maxPp; Accuracy = accuracy;
        AlwaysHits = alwaysHits; Priority = priority; IsStatus = isStatus; IsSpecial = isSpecial;
        AilmentName = ailmentName; AilmentChance = ailmentChance; FlinchChance = flinchChance;
        StatChanges = statChanges; StatChangeChance = statChangeChance; Description = description;
    }
}
