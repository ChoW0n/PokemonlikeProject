using PokemonBattle.Models;

namespace PokemonBattle.Services;

public interface IPresetStore
{
    Task SaveAsync(string name, List<PokemonLoadout> team);
    Task<List<PokemonLoadout>?> LoadAsync(string name);
    Task<List<string>> ListNamesAsync();
    Task<bool> DeleteAsync(string name);
}
