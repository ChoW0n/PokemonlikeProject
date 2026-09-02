using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class TeamLoadoutRules
{
    public const string NoItem = "없음";

    public static string NormalizeItemName(string? itemName) =>
        string.IsNullOrWhiteSpace(itemName) ? NoItem : itemName.Trim();

    // 입력 순서를 팀 배틀 순서로 간주하고, 첫 번째 도구만 보존한다.
    public static List<PokemonLoadout> NormalizeUniqueItems(IEnumerable<PokemonLoadout> loadouts)
    {
        var usedItems = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<PokemonLoadout>();

        foreach (var loadout in loadouts)
        {
            var copy = loadout.Clone();
            copy.ChosenItem = NormalizeItemName(copy.ChosenItem);

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
            .Select(loadout => NormalizeItemName(loadout.ChosenItem))
            .Where(item => item != NoItem)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static bool CanUseItem(
        IEnumerable<PokemonLoadout> loadouts,
        int pokemonId,
        string? itemName) =>
        NormalizeItemName(itemName) == NoItem
        || !UsedItems(loadouts, pokemonId).Contains(NormalizeItemName(itemName));

    public static bool HasDuplicateItems(IEnumerable<PokemonLoadout> loadouts)
    {
        var usedItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in loadouts.Select(loadout => NormalizeItemName(loadout.ChosenItem)))
        {
            if (item == NoItem) continue;
            if (!usedItems.Add(item)) return true;
        }

        return false;
    }
}