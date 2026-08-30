using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class InMemoryPresetStore : IPresetStore
{
    private readonly Dictionary<string, List<PokemonLoadout>> _presets = new();

    public void Save(string name, List<PokemonLoadout> team)
    {
        _presets[name] = team;
    }

    public List<PokemonLoadout>? Load(string name)
    {
        return _presets.TryGetValue(name, out var team) ? team : null;
    }

    public List<string> ListNames()
    {
        return _presets.Keys.ToList();
    }
}
