using Microsoft.EntityFrameworkCore;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public class ProgressionRegressionTests
{
    [Fact]
    public void PresetStoreKeepsIndependentLevelOneSnapshot()
    {
        var store = new InMemoryPresetStore();
        var original = new PokemonLoadout
        {
            PokemonId = 1,
            ChosenMoveNames = new List<string> { "tackle" },
            ChosenAbility = "심록",
            ChosenItem = "기합의띠",
            Level = 8
        };

        store.Save("팀", new List<PokemonLoadout> { original });
        original.Level = 20;
        original.ChosenMoveNames.Add("growl");

        var loaded = Assert.Single(store.Load("팀")!);
        Assert.Equal(1, loaded.Level);
        Assert.Equal(new[] { "tackle" }, loaded.ChosenMoveNames);

        loaded.ChosenMoveNames.Add("vine-whip");
        var loadedAgain = Assert.Single(store.Load("팀")!);
        Assert.Equal(new[] { "tackle" }, loadedAgain.ChosenMoveNames);
    }

    [Fact]
    public void PresetKeepsCurrentRunLevelOnlyForExistingPokemon()
    {
        var preset = new[]
        {
            new PokemonLoadout { PokemonId = 1, Level = 1 },
            new PokemonLoadout { PokemonId = 4, Level = 1 }
        };
        var currentRun = new[]
        {
            new PokemonLoadout { PokemonId = 1, Level = 12 }
        };

        var merged = PresetLoadoutMapper.ApplyCurrentRunLevels(preset, currentRun);

        Assert.Equal(12, merged.Single(loadout => loadout.PokemonId == 1).Level);
        Assert.Equal(1, merged.Single(loadout => loadout.PokemonId == 4).Level);
    }

    [Fact]
    public void LegendaryProgressClampsAtOneHundred()
    {
        Assert.Equal(100, LegendaryProgression.AddProgress(96, 20));
        Assert.Equal(0, LegendaryProgression.AddProgress(0, -10));
        Assert.True(LegendaryProgression.IsUnlocked(100));
        Assert.False(LegendaryProgression.IsUnlocked(99));
    }

    [Fact]
    public void LegendaryPoolOpensOnlyAfterUnlock()
    {
        var excluded = new HashSet<int>();
        var lockedTeam = Enumerable.Range(0, 20)
            .Select(_ => EnemyTeamProvider.GetRandomTeam(6, 721, false, excluded, legendaryUnlocked: false))
            .SelectMany(team => team)
            .ToList();

        Assert.DoesNotContain(lockedTeam, entry => EnemyTeamProvider.IsLegendary(entry.Key));

        var unlockedTeam = Enumerable.Range(0, 100)
            .Select(_ => EnemyTeamProvider.GetRandomTeam(6, 721, false, excluded, legendaryUnlocked: true))
            .SelectMany(team => team)
            .ToList();

        Assert.Contains(unlockedTeam, entry => EnemyTeamProvider.IsLegendary(entry.Key));
    }

    [Fact]
    public async Task RunStorePersistsLegendaryProgressAcrossFreshDbContext()
    {
        await WithTemporarySchema(async schema =>
        {
            const string username = "progress-regression-persistence";
            await CreatePlayerRunsTable(schema);
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle" },
                    ChosenAbility = "심록",
                    ChosenItem = "기합의띠",
                    Level = 7
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                await new RunStore(db).Save(username, 23, loadouts, 68);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(23, restored.score);
                Assert.Equal(68, restored.legendaryProgressPercent);
                var restoredLoadout = Assert.Single(restored.loadouts);
                Assert.Equal(1, restoredLoadout.PokemonId);
                Assert.Equal(7, restoredLoadout.Level);
                Assert.Equal(new[] { "tackle" }, restoredLoadout.ChosenMoveNames);
            }
        });
    }

    [Fact]
    public async Task ResetForNewRunKeepsLegendaryProgressWhileClearingRunState()
    {
        await WithTemporarySchema(async schema =>
        {
            const string username = "progress-regression-new-run";
            await CreatePlayerRunsTable(schema);
            await using (var seedDb = CreateDbContext(schema))
            {
                await new RunStore(seedDb).Save(
                    username,
                    41,
                    new List<PokemonLoadout> { new() { PokemonId = 4, Level = 12 } },
                    82);
            }

            var currentUser = new CurrentUserService();
            currentUser.SignIn(username, isAdmin: false);

            await using (var db = CreateDbContext(schema))
            {
                var state = new GameState(
                    new InMemoryScoreStore(),
                    new InMemoryPresetStore(),
                    new UnlockService(db, currentUser),
                    new RunStore(db),
                    currentUser);

                await state.LoadRunForCurrentUser();
                Assert.Equal(41, state.CurrentScore);
                Assert.Equal(new[] { 4 }, state.PlayerTeamIds);
                Assert.Equal(82, state.LegendaryProgressPercent);

                await state.ResetForNewRun();

                Assert.Equal(0, state.CurrentScore);
                Assert.Empty(state.PlayerTeamIds);
                Assert.Empty(state.PlayerLoadouts);
                Assert.Equal(82, state.LegendaryProgressPercent);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(0, restored.score);
                Assert.Empty(restored.loadouts);
                Assert.Equal(82, restored.legendaryProgressPercent);
            }
        });
    }

    [Fact]
    public async Task LegacyPlayerRunGetsZeroProgressWhenMigrationAddsColumn()
    {
        await WithTemporarySchema(async schema =>
        {
            const string username = "progress-regression-legacy";
            await using (var db = CreateDbContext(schema))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE "PlayerRuns" (
                        "Id" SERIAL PRIMARY KEY,
                        "Username" TEXT NOT NULL,
                        "CurrentScore" INTEGER NOT NULL,
                        "LoadoutsJson" TEXT NOT NULL
                    );
                    """);

                await db.Database.ExecuteSqlRawAsync("""
                    INSERT INTO "PlayerRuns" ("Username", "CurrentScore", "LoadoutsJson")
                    VALUES ({0}, 19, '[]');
                    """, username);

                // This is the same idempotent migration used during application startup.
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0;
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0;
                    """);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(19, restored.score);
                Assert.Empty(restored.loadouts);
                Assert.Equal(0, restored.legendaryProgressPercent);
            }
        });
    }

    private static AppDbContext CreateDbContext(string? schema = null)
    {
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be set to run PostgreSQL progression regression tests.");
        }

        var uri = new Uri(rawConnectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Disable
        };

        if (schema != null)
        {
            builder.SearchPath = schema;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task CreatePlayerRunsTable(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "PlayerRuns" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CurrentScore" INTEGER NOT NULL,
                "LoadoutsJson" TEXT NOT NULL,
                "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    private static async Task WithTemporarySchema(Func<string, Task> test)
    {
        var schema = $"progress_regression_{Guid.NewGuid():N}";

        await using (var db = CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE SCHEMA " + QuoteIdentifier(schema) + ";");
        }

        try
        {
            await test(schema);
        }
        finally
        {
            await using var db = CreateDbContext();
            await db.Database.ExecuteSqlRawAsync(
                "DROP SCHEMA IF EXISTS " + QuoteIdentifier(schema) + " CASCADE;");
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
