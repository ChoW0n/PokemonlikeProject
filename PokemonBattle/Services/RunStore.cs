using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class RunStore
{
    private readonly AppDbContext _db;

    public RunStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(int score, List<PokemonLoadout> loadouts)> Load(string username)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        if (run == null) return (0, new List<PokemonLoadout>());

        var loadouts = JsonSerializer.Deserialize<List<PokemonLoadout>>(run.LoadoutsJson) ?? new List<PokemonLoadout>();
        return (run.CurrentScore, loadouts);
    }

    public async Task Save(string username, int score, List<PokemonLoadout> loadouts)
    {
        var run = await _db.PlayerRuns.FirstOrDefaultAsync(r => r.Username == username);
        string json = JsonSerializer.Serialize(loadouts);

        if (run == null)
        {
            _db.PlayerRuns.Add(new PlayerRun { Username = username, CurrentScore = score, LoadoutsJson = json });
        }
        else
        {
            run.CurrentScore = score;
            run.LoadoutsJson = json;
        }

        await _db.SaveChangesAsync();
    }
}