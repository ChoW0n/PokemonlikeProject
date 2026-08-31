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
    public void DuplicateTeamItemsKeepTheFirstItemAndAllowNoneToRepeat()
    {
        var normalized = TeamLoadoutRules.NormalizeUniqueItems(new[]
        {
            new PokemonLoadout { PokemonId = 1, ChosenItem = "기합의띠" },
            new PokemonLoadout { PokemonId = 4, ChosenItem = "기합의띠" },
            new PokemonLoadout { PokemonId = 7, ChosenItem = "없음" },
            new PokemonLoadout { PokemonId = 10, ChosenItem = "없음" }
        });

        Assert.Equal("기합의띠", normalized[0].ChosenItem);
        Assert.Equal("없음", normalized[1].ChosenItem);
        Assert.Equal("없음", normalized[2].ChosenItem);
        Assert.Equal("없음", normalized[3].ChosenItem);
        Assert.False(TeamLoadoutRules.HasDuplicateItems(normalized));
        Assert.True(TeamLoadoutRules.CanUseItem(normalized, 1, "기합의띠"));
        Assert.False(TeamLoadoutRules.CanUseItem(normalized, 4, "기합의띠"));
    }

    [Fact]
    public void ProItemSelectionReturnsNoItemWhenEveryCandidateIsAlreadyUsed()
    {
        var item = ItemDatabase.GeneralItems.First(item => item.Name == "기합의띠");

        var selected = EnemyTeamProvider.PickProItem(
            new[] { "tackle" },
            new[] { item },
            new HashSet<string>(new[] { item.Name }, StringComparer.Ordinal));

        Assert.Equal(TeamLoadoutRules.NoItem, selected);
    }

    [Fact]
    public void ProItemExcludesChoiceItemsWhenMovesetHasAStatusMove()
    {
        var items = new[]
        {
            ItemDatabase.GeneralItems.First(item => item.Name == "구애머리띠"),
            ItemDatabase.GeneralItems.First(item => item.Name == "구애안경"),
            ItemDatabase.GeneralItems.First(item => item.Name == "구애스카프"),
            ItemDatabase.GeneralItems.First(item => item.Name == "생명의구슬")
        };

        var selected = EnemyTeamProvider.PickProItem(
            new[] { "tackle", "growl" },
            items);

        Assert.Equal("생명의구슬", selected);
    }

    [Fact]
    public void ProItemMatchesChoiceItemToPurePhysicalMoveset()
    {
        var items = new[]
        {
            ItemDatabase.GeneralItems.First(item => item.Name == "구애머리띠"),
            ItemDatabase.GeneralItems.First(item => item.Name == "구애안경"),
            ItemDatabase.GeneralItems.First(item => item.Name == "생명의구슬")
        };

        var selected = EnemyTeamProvider.PickProItem(
            new[] { "tackle" },
            items);

        Assert.Contains(selected, new[] { "구애머리띠", "생명의구슬" });
        Assert.NotEqual("구애안경", selected);
    }

    [Fact]
    public void ProAbilityOnlySelectsImplementedAbilities()
    {
        var data = PokemonDatabase.All.Values
            .First(pokemon => pokemon.AbilityNames.Any(AbilityDatabase.IsImplemented));

        var selected = EnemyTeamProvider.PickProAbility(data, new[] { "tackle", "growl" });

        Assert.True(AbilityDatabase.IsImplemented(selected));
    }

    [Fact]
    public void SkillRatingCalculatorUsesRoundsHpAndTurnEfficiency()
    {
        var strongWin = new RunPerformanceSummary(
            ClearedRounds: 5,
            TotalRounds: 5,
            AverageHpRatio: 1,
            AverageTurns: 5,
            Won: true);
        var slowLoss = new RunPerformanceSummary(
            ClearedRounds: 0,
            TotalRounds: 1,
            AverageHpRatio: 0,
            AverageTurns: 20,
            Won: false);

        Assert.Equal(1, SkillRatingCalculator.CalculatePerformanceScore(strongWin));
        Assert.True(
            SkillRatingCalculator.UpdateRating(
                SkillRatingCalculator.DefaultRating,
                strongWin)
            > SkillRatingCalculator.DefaultRating);
        Assert.True(
            SkillRatingCalculator.UpdateRating(
                SkillRatingCalculator.DefaultRating,
                slowLoss)
            < SkillRatingCalculator.DefaultRating);
    }

    [Fact]
    public void SkillDifficultyAdjustmentIsBoundedAndUsesTheDefaultAsNeutral()
    {
        Assert.Equal(
            0,
            SkillRatingCalculator.CalculateDifficultyAdjustment(
                SkillRatingCalculator.DefaultRating));
        Assert.Equal(
            SkillRatingCalculator.MinimumDifficultyAdjustment,
            SkillRatingCalculator.CalculateDifficultyAdjustment(0));
        Assert.Equal(
            SkillRatingCalculator.MaximumDifficultyAdjustment,
            SkillRatingCalculator.CalculateDifficultyAdjustment(5000));
    }

    [Fact]
    public async Task SkillRatingPersistsAcrossFreshDbContext()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            const string username = "skill-rating-persistence";
            var summary = new RunPerformanceSummary(3, 3, 0.8, 7, true);

            double updated;
            await using (var db = CreateDbContext(schema))
            {
                var service = new SkillRatingService(db);
                Assert.Equal(
                    SkillRatingCalculator.DefaultRating,
                    (await service.GetOrCreateAsync(username)).Rating);
                updated = await service.UpdateForRunAsync(username, summary);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new SkillRatingService(freshDb)
                    .GetOrCreateAsync(username);
                Assert.Equal(updated, restored.Rating);
                Assert.Equal(1, restored.CompletedRuns);
            }
        });
    }

    [Fact]
    public async Task GameStateKeepsDifficultyFixedUntilTheRunIsReset()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            const string username = "skill-difficulty-fixed";
            await using (var seedDb = CreateDbContext(schema))
            {
                seedDb.PlayerSkillRatings.Add(new PlayerSkillRating
                {
                    Username = username,
                    Rating = 1500
                });
                await seedDb.SaveChangesAsync();
                await new RunStore(seedDb).Save(
                    username,
                    2,
                    2,
                    new List<PokemonLoadout> { new() { PokemonId = 1, Level = 3 } },
                    0,
                    difficultyAdjustment: -2,
                    roundPerformances: new List<RunRoundPerformance>());
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
                    currentUser,
                    new SkillRatingService(db));

                await state.LoadRunForCurrentUser();
                Assert.Equal(-2, state.CurrentRunDifficultyAdjustment);

                await state.WinRound();
                Assert.Equal(-2, state.CurrentRunDifficultyAdjustment);
            }
        });
    }

    [Fact]
    public async Task WinAndLossBothUseRoundPerformanceForTheNextRunRating()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            const string winner = "skill-win";
            var currentUser = new CurrentUserService();
            currentUser.SignIn(winner, isAdmin: false);

            await using (var db = CreateDbContext(schema))
            {
                var state = new GameState(
                    new InMemoryScoreStore(),
                    new InMemoryPresetStore(),
                    new UnlockService(db, currentUser),
                    new RunStore(db),
                    currentUser,
                    new SkillRatingService(db));
                await state.LoadRunForCurrentUser();
                await state.WinRound();

                var runAfterRound = await new RunStore(db).Load(winner);
                var recorded = Assert.Single(runAfterRound.roundPerformances);
                Assert.True(recorded.Cleared);
                Assert.True(state.ResultSkillRating > SkillRatingCalculator.DefaultRating);
                Assert.True(state.LastSkillRatingChange > 0);
                Assert.True(
                    state.NextRunDifficultyAdjustment
                    >= SkillRatingCalculator.CalculateDifficultyAdjustment(
                        SkillRatingCalculator.DefaultRating));

                await state.ResetForNewRun();
                Assert.Empty((await new RunStore(db).Load(winner)).roundPerformances);
                Assert.True(state.SkillRating > SkillRatingCalculator.DefaultRating);
            }

            const string loser = "skill-loss";
            var losingUser = new CurrentUserService();
            losingUser.SignIn(loser, isAdmin: false);
            await using (var db = CreateDbContext(schema))
            {
                var state = new GameState(
                    new InMemoryScoreStore(),
                    new InMemoryPresetStore(),
                    new UnlockService(db, losingUser),
                    new RunStore(db),
                    losingUser,
                    new SkillRatingService(db));
                await state.LoadRunForCurrentUser();
                await state.LoseBattle();
                Assert.True(state.SkillRating < SkillRatingCalculator.DefaultRating);
                Assert.Equal(state.SkillRating, state.ResultSkillRating);
                Assert.True(state.LastSkillRatingChange < 0);
            }
        });
    }

    [Fact]
    public async Task InMemoryPresetsUpdateDeleteAndIsolateUsers()
    {
        var currentUser = new CurrentUserService();
        var store = new InMemoryPresetStore(currentUser);
        currentUser.SignIn("preset-user-a", isAdmin: false);

        await store.SaveAsync("  팀  ", new List<PokemonLoadout>
        {
            new() { PokemonId = 1, ChosenItem = "기합의띠", Level = 9 }
        });
        await store.SaveAsync("팀", new List<PokemonLoadout>
        {
            new() { PokemonId = 4, ChosenItem = "먹다남은음식", Level = 9 }
        });

        Assert.Equal(new[] { "팀" }, await store.ListNamesAsync());
        var updated = await store.LoadAsync("팀");
        var updatedLoadout = Assert.Single(updated!);
        Assert.Equal(4, updatedLoadout.PokemonId);
        Assert.Equal(1, updatedLoadout.Level);

        currentUser.SignIn("preset-user-b", isAdmin: false);
        Assert.Empty(await store.ListNamesAsync());
        Assert.Null(await store.LoadAsync("팀"));

        currentUser.SignIn("preset-user-a", isAdmin: false);
        Assert.True(await store.DeleteAsync("팀"));
        Assert.Empty(await store.ListNamesAsync());
        Assert.False(await store.DeleteAsync("팀"));
    }

    [Fact]
    public async Task PostgresPresetsSurviveFreshContextAndKeepUsersSeparate()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreateUserPresetsTable(schema);
            var loadouts = new List<PokemonLoadout>
            {
                new() { PokemonId = 1, ChosenItem = "기합의띠", Level = 14 }
            };

            var firstUser = new CurrentUserService();
            firstUser.SignIn("preset-persistence-a", isAdmin: false);
            await using (var db = CreateDbContext(schema))
            {
                var store = new PostgresPresetStore(db, firstUser);
                await store.SaveAsync("팀", loadouts);
                await store.SaveAsync("팀", new List<PokemonLoadout>
                {
                    new() { PokemonId = 4, ChosenItem = "먹다남은음식", Level = 22 }
                });
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var store = new PostgresPresetStore(freshDb, firstUser);
                var restored = await store.LoadAsync("팀");
                var restoredLoadout = Assert.Single(restored!);
                Assert.Equal(4, restoredLoadout.PokemonId);
                Assert.Equal(1, restoredLoadout.Level);
            }

            var secondUser = new CurrentUserService();
            secondUser.SignIn("preset-persistence-b", isAdmin: false);
            await using (var otherDb = CreateDbContext(schema))
            {
                var store = new PostgresPresetStore(otherDb, secondUser);
                Assert.Empty(await store.ListNamesAsync());
                Assert.False(await store.DeleteAsync("팀"));
            }

            await using (var deleteDb = CreateDbContext(schema))
            {
                Assert.True(await new PostgresPresetStore(deleteDb, firstUser).DeleteAsync("팀"));
            }
        });
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
    public void LegendaryEncounterConsumesOnlyAnUnlockedLegendaryLineup()
    {
        var consumed = LegendaryProgression.ConsumeEncounter(
            currentProgressPercent: 100,
            containsLegendary: true,
            alreadyConsumed: false);

        Assert.True(consumed.WasConsumed);
        Assert.Equal(0, consumed.ProgressPercent);
    }

    [Fact]
    public void LegendaryEncounterDoesNotConsumeBeforeUnlockOrWithoutLegendary()
    {
        var locked = LegendaryProgression.ConsumeEncounter(99, true, false);
        var ordinary = LegendaryProgression.ConsumeEncounter(100, false, false);

        Assert.False(locked.WasConsumed);
        Assert.Equal(99, locked.ProgressPercent);
        Assert.False(ordinary.WasConsumed);
        Assert.Equal(100, ordinary.ProgressPercent);
    }

    [Fact]
    public void LegendaryEncounterConsumptionIsIdempotentForTheSameLineup()
    {
        var repeated = LegendaryProgression.ConsumeEncounter(0, true, true);

        Assert.False(repeated.WasConsumed);
        Assert.Equal(0, repeated.ProgressPercent);
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
                var history = new List<LegendaryEncounterHistoryEntry>
                {
                    new()
                    {
                        CycleNumber = 2,
                        Stage = 24,
                        PokemonIds = new List<int> { 144 },
                        EncounteredAtUtc = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero)
                    }
                };
                await new RunStore(db).Save(username, 23, 31, loadouts, 68, history);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(23, restored.score);
                Assert.Equal(31, restored.highScore);
                Assert.Equal(68, restored.legendaryProgressPercent);
                var restoredHistory = Assert.Single(restored.legendaryEncounterHistory);
                Assert.Equal(2, restoredHistory.CycleNumber);
                Assert.Equal(24, restoredHistory.Stage);
                Assert.Equal(new[] { 144 }, restoredHistory.PokemonIds);
                var restoredLoadout = Assert.Single(restored.loadouts);
                Assert.Equal(1, restoredLoadout.PokemonId);
                Assert.Equal(7, restoredLoadout.Level);
                Assert.Equal(new[] { "tackle" }, restoredLoadout.ChosenMoveNames);
            }
        });
    }

    [Fact]
    public async Task GameStatePersistsHighScoreAcrossNewRunAndFreshContext()
    {
        await WithTemporarySchema(async schema =>
        {
            const string username = "high-score-regression";
            await CreatePlayerRunsTable(schema);

            await using (var seedDb = CreateDbContext(schema))
            {
                await new RunStore(seedDb).Save(
                    username,
                    18,
                    12,
                    new List<PokemonLoadout> { new() { PokemonId = 1, Level = 3 } },
                    0);
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
                    currentUser,
                    new SkillRatingService(db));

                await state.LoadRunForCurrentUser();
                Assert.Equal(18, state.CurrentScore);
                Assert.Equal(12, state.HighScore);

                await state.LoseBattle();
                Assert.Equal(0, state.CurrentScore);
                Assert.Equal(18, state.HighScore);

                await state.ResetForNewRun();
                Assert.Equal(0, state.CurrentScore);
                Assert.Equal(18, state.HighScore);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(0, restored.score);
                Assert.Equal(18, restored.highScore);
                Assert.Empty(restored.loadouts);
            }
        });
    }

    [Fact]
    public async Task LegendaryEncounterResetsProgressAndKeepsItZeroAfterWinning()
    {
        await WithTemporarySchema(async schema =>
        {
            const string username = "progress-regression-legendary-encounter";
            await CreatePlayerRunsTable(schema);

            await using (var seedDb = CreateDbContext(schema))
            {
                await new RunStore(seedDb).Save(
                    username,
                    7,
                    7,
                    new List<PokemonLoadout>(),
                    LegendaryProgression.MaxProgressPercent);
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
                    currentUser,
                    new SkillRatingService(db));

                await state.LoadRunForCurrentUser();
                await state.SetEnemyLoadouts(new List<PokemonLoadout>
                {
                    new() { PokemonId = 1 }
                });
                Assert.Equal(LegendaryProgression.MaxProgressPercent, state.LegendaryProgressPercent);
                Assert.False(state.LegendaryEncounterConsumed);

                await state.SetEnemyLoadouts(new List<PokemonLoadout>
                {
                    new() { PokemonId = 144 }
                });

                Assert.Equal(0, state.LegendaryProgressPercent);
                Assert.True(state.LegendaryEncounterConsumed);
                var history = Assert.Single(state.LegendaryEncounterHistory);
                Assert.Equal(1, history.CycleNumber);
                Assert.Equal(8, history.Stage);
                Assert.Equal(new[] { 144 }, history.PokemonIds);

                //ConfirmTeamAndGo can submit the same fixed lineup again without a second consumption.
                await state.SetEnemyLoadouts(new List<PokemonLoadout>
                {
                    new() { PokemonId = 144 }
                });
                Assert.Equal(0, state.LegendaryProgressPercent);

                await state.WinRound();

                Assert.Equal(0, state.LegendaryProgressPercent);
                Assert.Equal(0, state.LastLegendaryProgressReward);

                await state.ResetForNewRun();
                Assert.Equal(0, state.LegendaryProgressPercent);
                Assert.Single(state.LegendaryEncounterHistory);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);
                Assert.Equal(0, restored.legendaryProgressPercent);
                Assert.Single(restored.legendaryEncounterHistory);
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
                    52,
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
                    currentUser,
                    new SkillRatingService(db));

                await state.LoadRunForCurrentUser();
                Assert.Equal(41, state.CurrentScore);
                Assert.Equal(52, state.HighScore);
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
                Assert.Equal(52, restored.highScore);
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
                        ADD COLUMN IF NOT EXISTS "HighScore" INTEGER NOT NULL DEFAULT 0;
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0;
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "LegendaryEncounterHistoryJson" TEXT NOT NULL DEFAULT '[]';
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "DifficultyAdjustment" INTEGER NOT NULL DEFAULT 0;
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "RoundPerformancesJson" TEXT NOT NULL DEFAULT '[]';
                    """);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);

                Assert.Equal(19, restored.score);
                Assert.Equal(0, restored.highScore);
                Assert.Empty(restored.loadouts);
                Assert.Equal(0, restored.legendaryProgressPercent);
                Assert.Empty(restored.legendaryEncounterHistory);
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
                "HighScore" INTEGER NOT NULL DEFAULT 0,
                "LoadoutsJson" TEXT NOT NULL,
                        "LegendaryProgressPercent" INTEGER NOT NULL DEFAULT 0,
                        "LegendaryEncounterHistoryJson" TEXT NOT NULL DEFAULT '[]',
                        "DifficultyAdjustment" INTEGER NOT NULL DEFAULT 0,
                        "RoundPerformancesJson" TEXT NOT NULL DEFAULT '[]'
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "PlayerSkillRatings" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "Rating" DOUBLE PRECISION NOT NULL DEFAULT 1000,
                "CompletedRuns" INTEGER NOT NULL DEFAULT 0,
                "UpdatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE UNIQUE INDEX "IX_PlayerSkillRatings_Username"
                ON "PlayerSkillRatings" ("Username");
            """);
    }

    private static async Task CreateUserPresetsTable(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
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
