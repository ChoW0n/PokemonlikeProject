using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class PostgresPresetStore : IPresetStore
{
    private readonly AppDbContext _db;
    private readonly CurrentUserService _currentUser;
    private static readonly JsonSerializerOptions LoadoutJsonOptions = new()
    {
        IncludeFields = true
    };

    public PostgresPresetStore(AppDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task SaveAsync(string name, List<PokemonLoadout> team)
    {
        string username = RequireUsername();
        string normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) return;

        var snapshot = TeamLoadoutRules.NormalizeUniqueItems(team)
            .Select(loadout => loadout.Clone(level: 1))
            .ToList();
        string json = JsonSerializer.Serialize(snapshot, LoadoutJsonOptions);

        // The unique key plus ON CONFLICT makes simultaneous saves a deterministic update.
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserPresets" ("Username", "Name", "LoadoutsJson", "UpdatedAt")
            VALUES ({username}, {normalizedName}, {json}, CURRENT_TIMESTAMP)
            ON CONFLICT ("Username", "Name")
            DO UPDATE SET "LoadoutsJson" = EXCLUDED."LoadoutsJson",
                          "UpdatedAt" = CURRENT_TIMESTAMP;
            """);
    }

    public async Task<List<PokemonLoadout>?> LoadAsync(string name)
    {
        string username = RequireUsername();
        string normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) return null;

        var preset = await _db.UserPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Username == username && item.Name == normalizedName);
        if (preset == null) return null;

        var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(
            preset.LoadoutsJson,
            LoadoutJsonOptions) ?? new List<PokemonLoadout>();

        return TeamLoadoutRules.NormalizeUniqueItems(loadouts)
            .Select(loadout => loadout.Clone(level: 1))
            .ToList();
    }

    public async Task<List<string>> ListNamesAsync()
    {
        string username = RequireUsername();
        return await _db.UserPresets
            .AsNoTracking()
            .Where(item => item.Username == username)
            .OrderBy(item => item.Name)
            .Select(item => item.Name)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string name)
    {
        string username = RequireUsername();
        string normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) return false;

        return await _db.UserPresets
            .Where(item => item.Username == username && item.Name == normalizedName)
            .ExecuteDeleteAsync() > 0;
    }

    private string RequireUsername() =>
        _currentUser.Username is { Length: > 0 } username
            ? username
            : throw new InvalidOperationException("로그인한 사용자만 프리셋을 사용할 수 있습니다.");

    private static string NormalizeName(string name) => name.Trim();
}