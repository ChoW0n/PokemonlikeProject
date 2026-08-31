using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public class AdminDashboardRegressionTests
{
    [Fact]
    public async Task AdminDebugToolsAreIsolatedAndNormalizeValues()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateTables(schema);
            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.Users.AddRange(
                    new UserAccount
                    {
                        Username = "admin",
                        PasswordHash = PasswordHasher.Hash("admin"),
                        IsAdmin = true
                    },
                    new UserAccount
                    {
                        Username = "player-a",
                        PasswordHash = PasswordHasher.Hash("a"),
                        IsAdmin = false
                    },
                    new UserAccount
                    {
                        Username = "player-b",
                        PasswordHash = PasswordHasher.Hash("b"),
                        IsAdmin = false
                    });
                seedDb.UnlockedPokemons.AddRange(
                    new UnlockedPokemon { Username = "player-a", PokemonId = 1 },
                    new UnlockedPokemon { Username = "player-b", PokemonId = 4 });
                seedDb.PlayerRuns.AddRange(
                    new PlayerRun
                    {
                        Username = "player-a",
                        CurrentScore = 12,
                        HighScore = 30,
                        LoadoutsJson = "[{\"PokemonId\":1}]",
                        LegendaryProgressPercent = 25,
                        LegendaryEncounterHistoryJson = "[{\"CycleNumber\":1,\"Stage\":10,\"PokemonIds\":[1]}]"
                    },
                    new PlayerRun
                    {
                        Username = "player-b",
                        CurrentScore = 7,
                        HighScore = 9,
                        LoadoutsJson = "[]",
                        LegendaryProgressPercent = 4,
                        LegendaryEncounterHistoryJson = "[]"
                    });
                seedDb.UserPresets.Add(new UserPreset
                {
                    Username = "player-a",
                    Name = "테스트 팀",
                    LoadoutsJson = "[]"
                });
                await seedDb.SaveChangesAsync();
            }

            var normalUser = new CurrentUserService();
            normalUser.SignIn("player-b", isAdmin: false);
            await using (var normalDb = CreateDbContext(schema))
            {
                var normalDashboard = new AdminDashboardService(normalDb, normalUser);
                Assert.False(await normalDashboard.IsCurrentUserAdminAsync());
                Assert.Null(await normalDashboard.LoadAsync());
                Assert.False((await normalDashboard.SetScoresAsync("player-a", 99, 99)).Success);
                Assert.False((await normalDashboard.UnlockAllPokemonAsync("player-a")).Success);
            }

            var adminUser = new CurrentUserService();
            adminUser.SignIn("admin", isAdmin: true);
            await using (var adminDb = CreateDbContext(schema))
            {
                var dashboard = new AdminDashboardService(adminDb, adminUser);

                var unlockResult = await dashboard.UnlockAllPokemonAsync("player-a");
                Assert.True(unlockResult.Success);
                var unlockedIds = await adminDb.UnlockedPokemons
                    .Where(item => item.Username == "player-a")
                    .Select(item => item.PokemonId)
                    .Distinct()
                    .ToListAsync();
                Assert.Equal(PokemonDatabase.All.Keys.OrderBy(id => id), unlockedIds.OrderBy(id => id));
                Assert.Equal(1, await adminDb.UnlockedPokemons.CountAsync(item => item.Username == "player-b"));

                Assert.True((await dashboard.SetScoresAsync("player-a", 42, 55)).Success);
                Assert.True((await dashboard.SetLegendaryProgressAsync("player-a", 999)).Success);
                Assert.True((await dashboard.ResetRunAsync("player-a")).Success);

                var run = await adminDb.PlayerRuns.SingleAsync(item => item.Username == "player-a");
                Assert.Equal(0, run.CurrentScore);
                Assert.Equal(55, run.HighScore);
                Assert.Equal(100, run.LegendaryProgressPercent);
                Assert.Equal("[{\"CycleNumber\":1,\"Stage\":10,\"PokemonIds\":[1]}]", run.LegendaryEncounterHistoryJson);

                Assert.True((await dashboard.ClearLegendaryHistoryAsync("player-a")).Success);
                run = await adminDb.PlayerRuns.SingleAsync(item => item.Username == "player-a");
                Assert.Equal("[]", run.LegendaryEncounterHistoryJson);

                Assert.True((await dashboard.ResetUnlocksToStartersAsync("player-a")).Success);
                unlockedIds = await adminDb.UnlockedPokemons
                    .Where(item => item.Username == "player-a")
                    .Select(item => item.PokemonId)
                    .Distinct()
                    .ToListAsync();
                Assert.Equal(new[] { 1, 4, 7 }, unlockedIds.OrderBy(id => id));
            }
        });
    }

    [Fact]
    public async Task AdminAccountProtectionAndDeleteCleanupWorkAcrossFreshContexts()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateTables(schema);
            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.Users.AddRange(
                    new UserAccount
                    {
                        Username = "admin",
                        PasswordHash = PasswordHasher.Hash("admin"),
                        IsAdmin = true
                    },
                    new UserAccount
                    {
                        Username = "player",
                        PasswordHash = PasswordHasher.Hash("player"),
                        IsAdmin = false
                    });
                seedDb.UnlockedPokemons.Add(new UnlockedPokemon { Username = "player", PokemonId = 1 });
                seedDb.PlayerRuns.Add(new PlayerRun { Username = "player", LoadoutsJson = "[]" });
                seedDb.UserPresets.Add(new UserPreset
                {
                    Username = "player",
                    Name = "삭제 대상",
                    LoadoutsJson = "[]"
                });
                await seedDb.SaveChangesAsync();
            }

            var currentUser = new CurrentUserService();
            currentUser.SignIn("admin", isAdmin: true);
            await using (var adminDb = CreateDbContext(schema))
            {
                var dashboard = new AdminDashboardService(adminDb, currentUser);
                Assert.False((await dashboard.SetAdminAsync("admin", false)).Success);
                Assert.False((await dashboard.DeleteUserAsync("admin")).Success);
                Assert.True((await dashboard.ResetPasswordAsync("player", "new-password")).Success);
                var playerAfterReset = await adminDb.Users.SingleAsync(user => user.Username == "player");
                Assert.True(PasswordHasher.Verify("new-password", playerAfterReset.PasswordHash));
                Assert.True((await dashboard.DeleteUserAsync("player")).Success);
            }

            await using var verifyDb = CreateDbContext(schema);
            Assert.Null(await verifyDb.Users.SingleOrDefaultAsync(user => user.Username == "player"));
            Assert.Empty(await verifyDb.UnlockedPokemons.Where(item => item.Username == "player").ToListAsync());
            Assert.Empty(await verifyDb.PlayerRuns.Where(item => item.Username == "player").ToListAsync());
            Assert.Empty(await verifyDb.UserPresets.Where(item => item.Username == "player").ToListAsync());
        });
    }

    private static AppDbContext CreateDbContext(string schema)
    {
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be set to run PostgreSQL admin regression tests.");
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
            SslMode = SslMode.Disable,
            SearchPath = schema
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task CreateTables(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "Users" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "IsAdmin" BOOLEAN NOT NULL
            );
            CREATE TABLE "UnlockedPokemons" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "PokemonId" INTEGER NOT NULL
            );
            CREATE TABLE "PlayerRuns" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CurrentScore" INTEGER NOT NULL,
                "HighScore" INTEGER NOT NULL DEFAULT 0,
                "LoadoutsJson" TEXT NOT NULL,
                "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0,
                "LegendaryEncounterHistoryJson" TEXT NOT NULL DEFAULT '[]'
            );
            CREATE TABLE "UserPresets" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "LoadoutsJson" TEXT NOT NULL,
                "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT "UX_UserPresets_Username_Name" UNIQUE ("Username", "Name")
            );
            """);
    }

    private static async Task WithTemporarySchema(Func<string, Task> test)
    {
        var schema = $"admin_regression_{Guid.NewGuid():N}";
        await using (var db = CreateDbContext("public"))
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
            await using var db = CreateDbContext("public");
            await db.Database.ExecuteSqlRawAsync(
                "DROP SCHEMA IF EXISTS " + QuoteIdentifier(schema) + " CASCADE;");
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}