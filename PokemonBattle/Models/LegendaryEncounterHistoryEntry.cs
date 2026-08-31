namespace PokemonBattle.Models;

public class LegendaryEncounterHistoryEntry
{
    public int CycleNumber { get; set; }
    public int Stage { get; set; }
    public List<int> PokemonIds { get; set; } = new();
    public DateTimeOffset EncounteredAtUtc { get; set; }
}