using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class PokemonMasteryStore
{
    private readonly DatabaseContextExecutor _database;

    [ActivatorUtilitiesConstructor]
    public PokemonMasteryStore(DatabaseContextExecutor database)
    {
        _database = database;
    }

    public PokemonMasteryStore(AppDbContext db)
        : this(new DatabaseContextExecutor(db))
    {
    }

    public async Task<Dictionary<int, int>> LoadAsync(string username)
    {
        return await _database.ExecuteAsync("mastery.load", async db =>
            await db.PokemonMasteries
                .AsNoTracking()
                .Where(progress => progress.Username == username)
                .Where(progress => progress.VictoryContributions > 0)
                .ToDictionaryAsync(
                    progress => progress.PokemonId,
                    progress => Math.Max(0, progress.VictoryContributions)));
    }

    public async Task RecordVictoryContributionsAsync(
        string username,
        IEnumerable<int> pokemonIds)
    {
        var ids = pokemonIds
            .Where(PokemonDatabase.All.ContainsKey)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return;

        await _database.ExecuteAsync("mastery.record-victory", async db =>
        {
            foreach (int pokemonId in ids)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "PokemonMasteries"
                        ("Username", "PokemonId", "VictoryContributions")
                    VALUES ({username}, {pokemonId}, 1)
                    ON CONFLICT ("Username", "PokemonId")
                    DO UPDATE SET
                        "VictoryContributions" =
                            "PokemonMasteries"."VictoryContributions" + 1;
                    """);
            }
        });
    }

    public static void ApplyVictoryContributions(
        IDictionary<int, int> masteryWins,
        IEnumerable<int> pokemonIds)
    {
        foreach (int pokemonId in pokemonIds
            .Where(PokemonDatabase.All.ContainsKey)
            .Distinct())
        {
            masteryWins[pokemonId] =
                masteryWins.TryGetValue(pokemonId, out var currentWins)
                    ? currentWins + 1
                    : 1;
        }
    }
}