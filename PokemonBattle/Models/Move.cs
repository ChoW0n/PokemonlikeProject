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

    public static bool MakesContact(string moveKey, Move move) =>
        !move.IsStatus && !move.IsSpecial && move.Power > 0 && !NonContactPhysicalMoves.Contains(moveKey);
}
