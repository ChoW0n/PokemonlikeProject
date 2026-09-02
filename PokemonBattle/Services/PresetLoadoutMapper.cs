using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class PresetLoadoutMapper
{
    public static List<PokemonLoadout> ApplyCurrentRunLevels(
        IEnumerable<PokemonLoadout> preset,
        IEnumerable<PokemonLoadout> currentRun)
    {
        var currentLevels = currentRun
            .GroupBy(loadout => loadout.PokemonId)
            .ToDictionary(group => group.Key, group => group.First().Level);

        return TeamLoadoutRules.NormalizeUniqueItems(
            preset.Select(loadout => loadout.Clone(
                currentLevels.TryGetValue(loadout.PokemonId, out var currentLevel)
                    ? currentLevel
                    : 1)));
    }
}