namespace PokemonBattle.Models;

public static class StarterCatalog
{
    public static IReadOnlyList<int> PokemonIds { get; } = Array.AsReadOnly(new[]
    {
        1, 4, 7,
        152, 155, 158,
        252, 255, 258,
        387, 390, 393,
        495, 498, 501,
        650, 653, 656
    });
}