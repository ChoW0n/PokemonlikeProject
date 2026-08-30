using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class RunStore
{
    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions LoadoutJsonOptions = new()
    {
        IncludeFields = true
    };

    public RunStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(int score, List<PokemonLoadout> loadouts, int legendaryProgressPercent)> Load(string username)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        if (run == null) return (0, new List<PokemonLoadout>(), 0);

        var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(
            run.LoadoutsJson,
            LoadoutJsonOptions) ?? new List<PokemonLoadout>();
        return (run.CurrentScore, loadouts, Math.Clamp(run.LegendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent));
    }

    public async Task Save(string username, int score, List<PokemonLoadout> loadouts, int legendaryProgressPercent)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        string json = JsonSerializer.Serialize(loadouts, LoadoutJsonOptions);
        int safeProgress = Math.Clamp(legendaryProgressPercent, 0, LegendaryProgression.MaxProgressPercent);

        if (run == null)
        {
            _db.PlayerRuns.Add(new PlayerRun
            {
                Username = username,
                CurrentScore = score,
                LoadoutsJson = json,
                LegendaryProgressPercent = safeProgress
            });
        }
        else
        {
            run.CurrentScore = score;
            run.LoadoutsJson = json;
            run.LegendaryProgressPercent = safeProgress;
        }

        await _db.SaveChangesAsync();
    }
}