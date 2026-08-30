using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class InMemoryPresetStore : IPresetStore
{
    private readonly Dictionary<string, List<PokemonLoadout>> _presets = new();

    public void Save(string name, List<PokemonLoadout> team)
    {
        //프리셋은 런 중 포켓몬 객체와 분리된 구성 스냅샷이어야 한다.
        //레벨은 현재 런의 진행도이므로 저장하지 않고 새로 불러올 때 1부터 시작한다.
        _presets[name] = team.Select(loadout => loadout.Clone(level: 1)).ToList();
    }

    public List<PokemonLoadout>? Load(string name)
    {
        return _presets.TryGetValue(name, out var team)
            ? team.Select(loadout => loadout.Clone(level: 1)).ToList()
            : null;
    }

    public List<string> ListNames()
    {
        return _presets.Keys.ToList();
    }
}
