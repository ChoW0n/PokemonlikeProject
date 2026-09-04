using Microsoft.EntityFrameworkCore;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public class PokemonMasteryRegressionTests
{
    [Theory]
    [InlineData(0, 0, 0, 5)]
    [InlineData(4, 0, 0, 1)]
    [InlineData(5, 1, 1, 10)]
    [InlineData(14, 1, 1, 1)]
    [InlineData(15, 2, 2, 15)]
    [InlineData(29, 2, 2, 1)]
    [InlineData(30, 3, 3, 0)]
    [InlineData(100, 3, 3, 0)]
    public void MasteryTiersUseStableThresholds(
        int wins,
        int expectedTier,
        int expectedBonusPercent,
        int expectedWinsToNextTier)
    {
        Assert.Equal(expectedTier, PokemonMasteryRules.GetTier(wins));
        Assert.Equal(expectedBonusPercent, PokemonMasteryRules.GetBonusPercent(wins));
        Assert.Equal(expectedWinsToNextTier, PokemonMasteryRules.GetWinsToNextTier(wins));
    }

    [Fact]
    public void MasteryBonusRaisesOnlyTheConstructedPokemonStats()
    {
        var baseline = new Pokemon(
            PokemonDatabase.All[6],
            new List<string> { "scratch" },
            level: 50,
            masteryBonusPercent: 0);
        var mastered = new Pokemon(
            PokemonDatabase.All[6],
            new List<string> { "scratch" },
            level: 50,
            masteryBonusPercent: 3);
        var enemy = new Pokemon(
            PokemonDatabase.All[6],
            new List<string> { "scratch" },
            level: 50);

        Assert.True(mastered.MaxHp > baseline.MaxHp);
        Assert.True(mastered.Atk > baseline.Atk);
        Assert.True(mastered.Def > baseline.Def);
        Assert.True(mastered.SpAtk > baseline.SpAtk);
        Assert.True(mastered.SpDef > baseline.SpDef);
        Assert.True(mastered.Spd > baseline.Spd);
        Assert.Equal(baseline.Atk, enemy.Atk);
        Assert.Equal(baseline.MaxHp, enemy.MaxHp);
    }

    [Fact]
    public async Task MasteryContributionsPersistAndIncrementAcrossFreshDbContexts()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateMasteryTable(schema);
            const string username = "mastery-persistence";

            await using (var db = CreateDbContext(schema))
            {
                var store = new PokemonMasteryStore(db);
                await store.RecordVictoryContributionsAsync(
                    username,
                    new[] { 1, 1, 4, 9999 });
                Assert.Equal(
                    new[] { 1, 1 },
                    (await store.LoadAsync(username))
                        .OrderBy(entry => entry.Key)
                        .Select(entry => entry.Value));
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var store = new PokemonMasteryStore(freshDb);
                await store.RecordVictoryContributionsAsync(username, new[] { 1 });
            }

            await using (var verifyDb = CreateDbContext(schema))
            {
                var restored = await new PokemonMasteryStore(verifyDb).LoadAsync(username);
                Assert.Equal(2, restored[1]);
                Assert.Equal(1, restored[4]);
                Assert.DoesNotContain(9999, restored.Keys);
            }
        });
    }

    [Fact]
    public async Task WinningRoundRecordsOneContributionForEachTeamPokemon()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateGameStateTables(schema);
            await CreateMasteryTable(schema);
            const string username = "mastery-win-round";
            var currentUser = new CurrentUserService();
            currentUser.SignIn(username, isAdmin: false);

            await using (var db = CreateDbContext(schema))
            {
                var state = new GameState(
                    new InMemoryScoreStore(),
                    new InMemoryPresetStore(),
                    new UnlockService(db, currentUser),
                    new RunStore(db),
                    currentUser,
                    new SkillRatingService(db),
                    mastery: new PokemonMasteryStore(db));

                await state.LoadRunForCurrentUser();
                await state.SetPlayerLoadouts(new List<PokemonLoadout>
                {
                    new() { PokemonId = 1, ChosenMoveNames = new List<string> { "tackle" } },
                    new() { PokemonId = 4, ChosenMoveNames = new List<string> { "scratch" } }
                });
                await state.WinRound();

                Assert.Equal(1, state.PokemonMasteryWins[1]);
                Assert.Equal(1, state.PokemonMasteryWins[4]);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new PokemonMasteryStore(freshDb).LoadAsync(username);
                Assert.Equal(1, restored[1]);
                Assert.Equal(1, restored[4]);
            }
        });
    }

    private static AppDbContext CreateDbContext(string? schema = null)
    {
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be set to run PostgreSQL mastery regression tests.");
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

    private static async Task CreateGameStateTables(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "PlayerRuns" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CurrentScore" INTEGER NOT NULL,
                "HighScore" INTEGER NOT NULL DEFAULT 0,
                "LoadoutsJson" TEXT NOT NULL,
                "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0,
                "LegendaryEncounterHistoryJson" TEXT NOT NULL DEFAULT '[]',
                "DifficultyAdjustment" INTEGER NOT NULL DEFAULT 0,
                "RoundPerformancesJson" TEXT NOT NULL DEFAULT '[]',
                "RunMetaStateJson" TEXT NOT NULL DEFAULT '{{}}'
            );
            CREATE TABLE "PlayerSkillRatings" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "Rating" DOUBLE PRECISION NOT NULL DEFAULT 1000,
                "CompletedRuns" INTEGER NOT NULL DEFAULT 0,
                "PeakRating" DOUBLE PRECISION NOT NULL DEFAULT 1000,
                "PeakRound" INTEGER NOT NULL DEFAULT 0,
                "PeakAchievedAtUtc" TIMESTAMPTZ NULL,
                "UpdatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE UNIQUE INDEX "IX_PlayerSkillRatings_Username"
                ON "PlayerSkillRatings" ("Username");
            """);
    }

    private static async Task CreateMasteryTable(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "PokemonMasteries" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "PokemonId" INTEGER NOT NULL,
                "VictoryContributions" INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX "IX_PokemonMasteries_Username_PokemonId"
                ON "PokemonMasteries" ("Username", "PokemonId");
            """);
    }

    private static async Task WithTemporarySchema(Func<string, Task> test)
    {
        var schema = $"mastery_regression_{Guid.NewGuid():N}";

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