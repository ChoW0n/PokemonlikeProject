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
                Assert.Equal(
                    StarterCatalog.PokemonIds.OrderBy(id => id),
                    unlockedIds.OrderBy(id => id));

                var operations = new AdminOperationsService(adminDb, adminUser);
                Assert.True((await operations.ResetUnlocksToStartersAsync("player-b")).Success);
                unlockedIds = await adminDb.UnlockedPokemons
                    .Where(item => item.Username == "player-b")
                    .Select(item => item.PokemonId)
                    .Distinct()
                    .ToListAsync();
                Assert.Equal(
                    StarterCatalog.PokemonIds.OrderBy(id => id),
                    unlockedIds.OrderBy(id => id));
            }
        });
    }

    [Fact]
    public async Task NewUsersReceiveEveryGenerationStarter()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateTables(schema);
            const string username = "starter-user";

            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.Users.Add(new UserAccount
                {
                    Username = username,
                    PasswordHash = PasswordHasher.Hash("starter"),
                    IsAdmin = false
                });
                await seedDb.SaveChangesAsync();
            }

            var currentUser = new CurrentUserService();
            currentUser.SignIn(username, isAdmin: false);
            await using (var db = CreateDbContext(schema))
            {
                var unlockedIds = await new UnlockService(db, currentUser).GetUnlockedIds();

                Assert.Equal(
                    StarterCatalog.PokemonIds.OrderBy(id => id),
                    unlockedIds.OrderBy(id => id));
                Assert.Equal(
                    StarterCatalog.PokemonIds.Count,
                    await db.UnlockedPokemons.CountAsync(item => item.Username == username));
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
            Assert.Empty(await verifyDb.PlayerSkillRatings.Where(item => item.Username == "player").ToListAsync());
            Assert.Empty(await verifyDb.UserPresets.Where(item => item.Username == "player").ToListAsync());
        });
    }

    [Fact]
    public async Task LeaderboardAndAdminAnalyticsUsePersistedPlayerData()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateTables(schema);
            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.Users.AddRange(
                    new UserAccount { Username = "admin", PasswordHash = "x", IsAdmin = true },
                    new UserAccount { Username = "alpha", PasswordHash = "x", IsAdmin = false },
                    new UserAccount { Username = "beta", PasswordHash = "x", IsAdmin = false });
                seedDb.PlayerSkillRatings.AddRange(
                    new PlayerSkillRating { Username = "alpha", Rating = 1100, CompletedRuns = 2 },
                    new PlayerSkillRating { Username = "beta", Rating = 1250, CompletedRuns = 4 });
                seedDb.PlayerProgressions.AddRange(
                    new PlayerProgression
                    {
                        Username = "alpha",
                        LatestLoadoutsJson = """
                            [{"PokemonId":1,"ChosenMoveNames":["tackle"],"ChosenAbility":"심록","ChosenItem":"없음","Level":5}]
                            """,
                        MovePreferencesJson = """{"MoveCounts":{"tackle":3},"CategoryCounts":{"physical":3},"TypeCounts":{"Normal":3},"TacticalCounts":{"damage":3}}"""
                    },
                    new PlayerProgression
                    {
                        Username = "beta",
                        LatestLoadoutsJson = """
                            [{"PokemonId":1,"ChosenMoveNames":["tackle"],"ChosenAbility":"심록","ChosenItem":"없음","Level":8}]
                            """,
                        MovePreferencesJson = """{"MoveCounts":{"tackle":2},"CategoryCounts":{},"TypeCounts":{},"TacticalCounts":{}}"""
                    });
                seedDb.PlayerRuns.AddRange(
                    new PlayerRun
                    {
                        Username = "alpha",
                        LoadoutsJson = "[]",
                        RoundPerformancesJson = """[{"Cleared":true},{"Cleared":false}]"""
                    },
                    new PlayerRun
                    {
                        Username = "beta",
                        LoadoutsJson = "[]",
                        RoundPerformancesJson = """[{"Cleared":true},{"Cleared":true}]"""
                    });
                await seedDb.SaveChangesAsync();
            }

            var player = new CurrentUserService();
            player.SignIn("alpha", isAdmin: false);
            await using (var db = CreateDbContext(schema))
            {
                var leaderboard = await new LeaderboardService(db, player).LoadAsync();
                Assert.NotNull(leaderboard);
                Assert.Equal("beta", leaderboard!.Entries[0].Username);
                Assert.Equal(2, leaderboard.CurrentUser!.Rank);
                Assert.DoesNotContain(leaderboard.Entries, entry => entry.Username == "admin");
            }

            var admin = new CurrentUserService();
            admin.SignIn("admin", isAdmin: true);
            await using (var db = CreateDbContext(schema))
            {
                var snapshot = await new AdminDashboardService(db, admin).LoadAsync();
                Assert.NotNull(snapshot);
                Assert.Equal("이상해씨", snapshot!.Analytics.PokemonPopularity[0].Label);
                Assert.Equal(100, snapshot.Analytics.PokemonPopularity[0].SharePercent);
                Assert.Equal("몸통박치기", snapshot.Analytics.MovePopularity[0].Label);
                Assert.Equal("심록", snapshot.Analytics.AbilityPopularity[0].Label);
                Assert.Equal(2, snapshot.Analytics.UsersWithRoundData);
                Assert.Equal(1, snapshot.Analytics.WinRateDistribution.Single(bar => bar.Label == "26–50%").Count);
                Assert.Equal(1, snapshot.Analytics.WinRateDistribution.Single(bar => bar.Label == "76–100%").Count);

                var details = await new AdminDashboardService(db, admin).LoadUserDetailsAsync("alpha");
                Assert.NotNull(details);
                Assert.Equal(1, details!.PersonalAnalytics.TeamSize);
                Assert.Equal(3, details.PersonalAnalytics.TotalMoveSelections);
                Assert.Equal("이상해씨", details.PersonalAnalytics.PokemonComposition.Single().Label);
                Assert.Equal("몸통박치기", details.PersonalAnalytics.MovePreferences.Single().Label);
                Assert.Equal(100, details.PersonalAnalytics.MovePreferences.Single().SharePercent);
                Assert.Equal("물리", details.PersonalAnalytics.MoveCategoryPreferences.Single().Label);
                Assert.Equal("노말", details.PersonalAnalytics.MoveTypePreferences.Single().Label);
                Assert.Equal("심록", details.PersonalAnalytics.AbilityPreferences.Single().Label);
            }
        });
    }

    // 승률 집계가 유형·라운드·라이벌 회차별로 나뉘는지 확인한다.
    [Fact]
    public async Task WinRateAnalyticsGroupsBattleResultsByTypeRoundAndRivalNumber()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateTables(schema);
            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.Users.Add(new UserAccount
                {
                    Username = "admin",
                    PasswordHash = "x",
                    IsAdmin = true
                });
                seedDb.BattleResults.AddRange(
                    new BattleResult { Username = "player", Won = true, Round = 1 },
                    new BattleResult { Username = "player", Won = false, Round = 1 },
                    new BattleResult
                    {
                        Username = "player",
                        Won = true,
                        IsRivalBattle = true,
                        RivalNumber = 1,
                        Round = 2
                    },
                    new BattleResult
                    {
                        Username = "player",
                        Won = false,
                        IsRivalBattle = true,
                        RivalNumber = 1,
                        Round = 2
                    },
                    new BattleResult
                    {
                        Username = "player",
                        Won = true,
                        IsRivalBattle = true,
                        RivalNumber = 2,
                        IsLegendaryBattle = true,
                        Round = 20
                    },
                    new BattleResult
                    {
                        Username = "player",
                        Won = false,
                        IsLegendaryBattle = true,
                        Round = 25
                    });
                await seedDb.SaveChangesAsync();
            }

            var admin = new CurrentUserService();
            admin.SignIn("admin", isAdmin: true);
            await using var db = CreateDbContext(schema);
            var snapshot = await new WinRateAnalyticsService(db, admin).LoadAsync();

            Assert.NotNull(snapshot);
            Assert.Equal(new WinRateSummary(6, 3, 50), snapshot!.Overall);
            Assert.Equal(new WinRateSummary(3, 2, 200d / 3), snapshot.Rival);
            Assert.Equal(new WinRateSummary(3, 1, 100d / 3), snapshot.Normal);
            Assert.Equal(new WinRateSummary(2, 1, 50), snapshot.Legendary);

            var lateRounds = Assert.Single(snapshot.ByRound, row => row.Label == "20+");
            Assert.Equal(2, lateRounds.BattleCount);
            Assert.Equal(1, lateRounds.WinCount);
            var firstRival = Assert.Single(
                snapshot.ByRivalNumber, row => row.Label == "1회차");
            Assert.Equal(2, firstRival.BattleCount);
            Assert.Equal(1, firstRival.WinCount);
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
                "UpdatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT "UX_PlayerSkillRatings_Username" UNIQUE ("Username")
            );
            CREATE TABLE "UserPresets" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "LoadoutsJson" TEXT NOT NULL,
                "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT "UX_UserPresets_Username_Name" UNIQUE ("Username", "Name")
            );
            CREATE TABLE "PlayerProgressions" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CompletedBattles" INTEGER NOT NULL DEFAULT 0,
                "RivalPending" BOOLEAN NOT NULL DEFAULT FALSE,
                "RivalNumber" INTEGER NOT NULL DEFAULT 0,
                "LatestLoadoutsJson" TEXT NOT NULL DEFAULT '[]',
                "MovePreferencesJson" TEXT NOT NULL DEFAULT '{{}}',
                "UpdatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CONSTRAINT "UX_PlayerProgressions_Username" UNIQUE ("Username")
            );
            CREATE TABLE "BattleResults" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "IsRivalBattle" BOOLEAN NOT NULL DEFAULT FALSE,
                "IsLegendaryBattle" BOOLEAN NOT NULL DEFAULT FALSE,
                "RivalNumber" INTEGER NOT NULL DEFAULT 0,
                "Won" BOOLEAN NOT NULL,
                "EndReason" TEXT NOT NULL DEFAULT '',
                "Round" INTEGER NOT NULL DEFAULT 1,
                "Turns" INTEGER NOT NULL DEFAULT 0,
                "PlayerHpRatio" DOUBLE PRECISION NOT NULL DEFAULT 0,
                "EnemyHpRatio" DOUBLE PRECISION NOT NULL DEFAULT 0,
                "DifficultyAdjustment" INTEGER NOT NULL DEFAULT 0,
                "SkillRating" DOUBLE PRECISION NOT NULL DEFAULT 1000,
                "UnlockedCount" INTEGER NOT NULL DEFAULT 0,
                "RunSeq" INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE "AdminAuditLogs" (
                "Id" SERIAL PRIMARY KEY,
                "AdminUsername" TEXT NOT NULL,
                "Action" TEXT NOT NULL,
                "TargetUsername" TEXT NOT NULL,
                "Details" TEXT NOT NULL DEFAULT '',
                "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
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