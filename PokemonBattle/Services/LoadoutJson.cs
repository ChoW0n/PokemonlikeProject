using System.Text.Json;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public static class LoadoutJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true
    };

    public static string Serialize(IEnumerable<PokemonLoadout> loadouts) =>
        JsonSerializer.Serialize(
            TeamLoadoutRules.NormalizeUniqueItems(loadouts),
            Options);

    public static List<PokemonLoadout> Deserialize(string json) =>
        TeamLoadoutRules.NormalizeUniqueItems(
            JsonSerializer.Deserialize<List<PokemonLoadout>>(json, Options)
            ?? new List<PokemonLoadout>());

    public static List<PokemonLoadout> ClearChosenItems(IEnumerable<PokemonLoadout> loadouts) =>
        TeamLoadoutRules.NormalizeUniqueItems(loadouts)
            .Select(loadout =>
            {
                loadout.ChosenItem = TeamLoadoutRules.NoItem;
                return loadout;
            })
            .ToList();
}