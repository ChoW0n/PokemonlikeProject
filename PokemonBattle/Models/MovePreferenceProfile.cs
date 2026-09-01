namespace PokemonBattle.Models;

public sealed class MovePreferenceProfile
{
    public Dictionary<string, int> MoveCounts { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> CategoryCounts { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> TypeCounts { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> TacticalCounts { get; set; } = new(StringComparer.Ordinal);
}

public static class MovePreferenceRules
{
    public static void Record(MovePreferenceProfile profile, string moveKey)
    {
        if (!MoveDatabase.All.TryGetValue(moveKey, out var move)) return;

        Increment(profile.MoveCounts, moveKey);
        Increment(profile.CategoryCounts, move.IsStatus ? "status" : move.IsSpecial ? "special" : "physical");
        Increment(profile.TypeCounts, move.Type.ToString());

        if (move.Priority > 0) Increment(profile.TacticalCounts, "priority");
        if (move.StatChanges.Count > 0) Increment(profile.TacticalCounts, "rank-up");
        if (!string.Equals(move.AilmentName, "none", StringComparison.Ordinal))
            Increment(profile.TacticalCounts, "status-effect");
        if (MoveRuleMetadata.IsProtectionMove(moveKey))
            Increment(profile.TacticalCounts, "protection");
        if (!move.IsStatus) Increment(profile.TacticalCounts, "damage");
    }

    public static int CountFor(MovePreferenceProfile profile, string moveKey) =>
        profile.MoveCounts.TryGetValue(moveKey, out var count) ? count : 0;

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
    }
}