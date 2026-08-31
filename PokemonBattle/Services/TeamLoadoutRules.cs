using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class TeamLoadoutRules
{
    public const string NoItem = "없음";

    // 입력 순서를 팀 배틀 순서로 간주하고, 첫 번째 도구만 보존한다.
    public static List<PokemonLoadout> NormalizeUniqueItems(IEnumerable<PokemonLoadout> loadouts)
    {
        var usedItems = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<PokemonLoadout>();

        foreach (var loadout in loadouts)
        {
            var copy = loadout.Clone();
            copy.ChosenItem = string.IsNullOrWhiteSpace(copy.ChosenItem)
                ? NoItem
                : copy.ChosenItem.Trim();

            if (copy.ChosenItem != NoItem && !usedItems.Add(copy.ChosenItem))
            {
                copy.ChosenItem = NoItem;
            }

            normalized.Add(copy);
        }

        return normalized;
    }

    public static HashSet<string> UsedItems(
        IEnumerable<PokemonLoadout> loadouts,
        int? excludingPokemonId = null)
    {
        return loadouts
            .Where(loadout => excludingPokemonId == null || loadout.PokemonId != excludingPokemonId)
            .Select(loadout => loadout.ChosenItem)
            .Where(item => !string.IsNullOrWhiteSpace(item) && item != NoItem)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static bool CanUseItem(
        IEnumerable<PokemonLoadout> loadouts,
        int pokemonId,
        string? itemName) =>
        string.IsNullOrWhiteSpace(itemName)
        || itemName == NoItem
        || !UsedItems(loadouts, pokemonId).Contains(itemName);

    public static bool HasDuplicateItems(IEnumerable<PokemonLoadout> loadouts)
    {
        var usedItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in loadouts.Select(loadout => loadout.ChosenItem))
        {
            if (string.IsNullOrWhiteSpace(item) || item == NoItem) continue;
            if (!usedItems.Add(item)) return true;
        }

        return false;
    }
}