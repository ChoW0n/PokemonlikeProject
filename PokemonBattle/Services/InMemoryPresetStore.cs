using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class InMemoryPresetStore : IPresetStore
{
    private readonly Dictionary<string, Dictionary<string, List<PokemonLoadout>>> _presets = new();
    private readonly CurrentUserService? _currentUser;

    public InMemoryPresetStore(CurrentUserService? currentUser = null)
    {
        _currentUser = currentUser;
    }

    // 기존 단위 테스트와 개발용 호출을 위한 기본 사용자 래퍼.
    public void Save(string name, List<PokemonLoadout> team)
    {
        SaveForUser(DefaultUsername, name, team);
    }

    public List<PokemonLoadout>? Load(string name)
    {
        return LoadForUser(DefaultUsername, name);
    }

    public List<string> ListNames()
    {
        return ListNamesForUser(DefaultUsername);
    }

    public Task SaveAsync(string name, List<PokemonLoadout> team)
    {
        SaveForUser(CurrentUsername, name, team);
        return Task.CompletedTask;
    }

    public Task<List<PokemonLoadout>?> LoadAsync(string name) =>
        Task.FromResult(LoadForUser(CurrentUsername, name));

    public Task<List<string>> ListNamesAsync() =>
        Task.FromResult(ListNamesForUser(CurrentUsername));

    public Task<bool> DeleteAsync(string name)
    {
        var deleted = _presets.TryGetValue(CurrentUsername, out var userPresets)
            && userPresets.Remove(NormalizeName(name));
        return Task.FromResult(deleted);
    }

    private string CurrentUsername =>
        _currentUser?.Username is { Length: > 0 } username ? username : DefaultUsername;

    private const string DefaultUsername = "__test__";

    private void SaveForUser(string username, string name, List<PokemonLoadout> team)
    {
        string normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) return;

        if (!_presets.TryGetValue(username, out var userPresets))
        {
            userPresets = new Dictionary<string, List<PokemonLoadout>>();
            _presets[username] = userPresets;
        }

        userPresets[normalizedName] = TeamLoadoutRules.NormalizeUniqueItems(team)
            .Select(loadout => loadout.Clone(level: 1))
            .ToList();
    }

    private List<PokemonLoadout>? LoadForUser(string username, string name)
    {
        return _presets.TryGetValue(username, out var userPresets)
            && userPresets.TryGetValue(NormalizeName(name), out var team)
            ? team.Select(loadout => loadout.Clone(level: 1)).ToList()
            : null;
    }

    private List<string> ListNamesForUser(string username) =>
        _presets.TryGetValue(username, out var userPresets)
            ? userPresets.Keys.OrderBy(name => name).ToList()
            : new List<string>();

    private static string NormalizeName(string name) => name.Trim();
}
