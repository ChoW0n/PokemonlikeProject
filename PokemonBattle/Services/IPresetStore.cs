using PokemonBattle.Models;

namespace PokemonBattle.Services;

public interface IPresetStore
{
    void Save(string name, List<PokemonLoadout> team);
    List<PokemonLoadout>? Load(string name);
    List<string> ListNames();
}
