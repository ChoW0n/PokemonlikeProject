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
    public void LoadoutJsonRoundTripsGenderAndAcceptsLegacyLoadouts()
    {
        var original = new PokemonLoadout
        {
            PokemonId = 25,
            ChosenMoveNames = new List<string> { "thunder-shock" },
            ChosenAbility = "정전기",
            ChosenItem = TeamLoadoutRules.NoItem,
            Level = 8,
            Gender = PokemonGender.Female
        };

        var restored = Assert.Single(LoadoutJson.Deserialize(
            LoadoutJson.Serialize(new[] { original })));
        Assert.Equal(PokemonGender.Female, restored.Gender);

        var legacy = Assert.Single(LoadoutJson.Deserialize(
            """[{"PokemonId":25,"ChosenMoveNames":["thunder-shock"],"ChosenAbility":"정전기","ChosenItem":"없음","Level":8}]"""));
        Assert.Null(legacy.Gender);
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
    public void ItemRulesTrimWhitespaceBeforeCheckingDuplicates()
    {
        var loadouts = new[]
        {
            new PokemonLoadout { PokemonId = 1, ChosenItem = " 기합의띠 " },
            new PokemonLoadout { PokemonId = 4, ChosenItem = "기합의띠" }
        };

        Assert.True(TeamLoadoutRules.HasDuplicateItems(loadouts));
        Assert.False(TeamLoadoutRules.CanUseItem(loadouts, 7, " 기합의띠 "));
        Assert.Equal("기합의띠", TeamLoadoutRules.NormalizeUniqueItems(loadouts)[0].ChosenItem);
        Assert.Equal(TeamLoadoutRules.NoItem, TeamLoadoutRules.NormalizeUniqueItems(loadouts)[1].ChosenItem);
    }

    [Fact]
    public void OneTimeItemCleanupClearsItemsWithoutChangingOtherLoadoutData()
    {
        var original = new PokemonLoadout
        {
            PokemonId = 25,
            ChosenMoveNames = new List<string> { "thunder-shock" },
            ChosenAbility = "정전기",
            ChosenItem = " 기합의띠 ",
            Level = 18
        };

        var cleared = Assert.Single(LoadoutJson.ClearChosenItems(
            new[] { original }));

        Assert.Equal(25, cleared.PokemonId);
        Assert.Equal(new[] { "thunder-shock" }, cleared.ChosenMoveNames);
        Assert.Equal("정전기", cleared.ChosenAbility);
        Assert.Equal(18, cleared.Level);
        Assert.Equal(TeamLoadoutRules.NoItem, cleared.ChosenItem);
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
    public void SkillAdjustment_strengthens_synergy_and_relaxes_low_skill_enemy_choices()
    {
        var data = new PokemonData(
            "테스트포켓몬",
            "test-pokemon",
            PokemonType.Normal,
            null,
            50,
            50,
            50,
            50,
            50,
            50,
            new[] { "tackle" },
            new[] { "근성", "도주" },
            "",
            "",
            null,
            1);

        int highSkillSynergyAbilities = Enumerable.Range(0, 400)
            .Count(_ => EnemyTeamProvider.PickProAbility(
                data,
                new[] { "tackle", "take-down" },
                skillAdjustment: 5) == "근성");
        int lowSkillSynergyAbilities = Enumerable.Range(0, 400)
            .Count(_ => EnemyTeamProvider.PickProAbility(
                data,
                new[] { "tackle", "take-down" },
                skillAdjustment: -3) == "근성");

        Assert.True(highSkillSynergyAbilities > lowSkillSynergyAbilities + 40);
    }

    [Fact]
    public void SkillAdjustment_makes_low_skill_enemy_items_more_random()
    {
        var items = new[]
        {
            ItemDatabase.GeneralItems.First(item => item.Name == "생명의구슬"),
            ItemDatabase.GeneralItems.First(item => item.Name == TeamLoadoutRules.NoItem)
        };

        int highSkillNonEmptyItems = Enumerable.Range(0, 100)
            .Count(_ => EnemyTeamProvider.PickProItem(
                new[] { "tackle" },
                items,
                skillAdjustment: 5) == "생명의구슬");
        int lowSkillEmptyItems = Enumerable.Range(0, 100)
            .Count(_ => EnemyTeamProvider.PickProItem(
                new[] { "tackle" },
                items,
                skillAdjustment: -3) == TeamLoadoutRules.NoItem);

        Assert.Equal(100, highSkillNonEmptyItems);
        Assert.True(lowSkillEmptyItems > 0);
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
            -1,
            SkillRatingCalculator.CalculateDifficultyAdjustment(
                SkillRatingCalculator.DefaultRating - 50));
        Assert.Equal(
            1,
            SkillRatingCalculator.CalculateDifficultyAdjustment(
                SkillRatingCalculator.DefaultRating + 50));
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
    public async Task SkillRatingPeakRisesAndSurvivesALaterDrop()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            const string username = "skill-rating-peak";
            double risingRating;
            double fallingRating;

            await using (var db = CreateDbContext(schema))
            {
                var service = new SkillRatingService(db);
                risingRating = await service.UpdateForRunAsync(
                    username,
                    new RunPerformanceSummary(3, 3, 0.8, 7, true),
                    peakRound: 3);
                fallingRating = await service.UpdateForRunAsync(
                    username,
                    new RunPerformanceSummary(0, 1, 0, 30, false),
                    peakRound: 4);
            }

            await using var verifyDb = CreateDbContext(schema);
            var rating = await verifyDb.PlayerSkillRatings
                .SingleAsync(item => item.Username == username);
            Assert.True(risingRating > SkillRatingCalculator.DefaultRating);
            Assert.True(fallingRating < risingRating);
            Assert.Equal(risingRating, rating.PeakRating);
            Assert.Equal(3, rating.PeakRound);
            Assert.NotNull(rating.PeakAchievedAtUtc);
        });
    }

    [Fact]
    public async Task RunStorePersistsRunMetaAcrossFreshDbContext()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            const string username = "run-meta-persistence";
            var meta = new RunMetaState
            {
                LegacyIds = new List<string> { "first-strike" },
                BattlefieldImprintId = "storm-garden",
                BattlefieldImprintStage = 4,
                RiskCovenantId = "blood-debt",
                RiskCovenantStage = 4,
                RiskCovenantDecisionMade = true,
                RiskCovenantAccepted = true,
                StolenMoves = new List<StolenMoveRecord>
                {
                    new() { PokemonId = 1, MoveKey = "tackle" }
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                await new RunStore(db).Save(
                    username,
                    4,
                    4,
                    new List<PokemonLoadout>
                    {
                        new() { PokemonId = 1, ChosenMoveNames = new List<string> { "tackle" } }
                    },
                    0,
                    metaState: meta);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new RunStore(freshDb).Load(username);
                Assert.Equal(new[] { "first-strike" }, restored.metaState.LegacyIds);
                Assert.Equal("storm-garden", restored.metaState.BattlefieldImprintId);
                Assert.True(restored.metaState.RiskCovenantAccepted);
                Assert.Equal(4, restored.metaState.BattlefieldImprintStage);
                Assert.Equal("tackle", Assert.Single(restored.metaState.StolenMoves).MoveKey);
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
    public async Task BattleResultIsRecordedForVictory()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "battle-result-win";
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
                    new PlayerProgressionStore(db));
                await state.LoadRunForCurrentUser();

                var player = new Pokemon(
                    PokemonDatabase.All[1],
                    new List<string> { "tackle" },
                    level: 10);
                await state.WinRound(turns: 6, playerTeam: new[] { player });
            }

            await using var verifyDb = CreateDbContext(schema);
            var result = Assert.Single(await verifyDb.BattleResults
                .Where(item => item.Username == username)
                .ToListAsync());
            Assert.True(result.Won);
            Assert.False(result.IsRivalBattle);
            Assert.Equal(1, result.Round);
            Assert.Equal(6, result.Turns);
            Assert.Equal(1, result.PlayerHpRatio);
            Assert.Equal(0, result.DifficultyAdjustment);
            Assert.Equal(SkillRatingCalculator.DefaultRating, result.SkillRating);
        });
    }

    [Fact]
    public async Task BattleResultIsRecordedForLoss()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "battle-result-loss";
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
                    new PlayerProgressionStore(db));
                await state.LoadRunForCurrentUser();

                var player = new Pokemon(
                    PokemonDatabase.All[1],
                    new List<string> { "tackle" },
                    level: 10);
                player.CurrentHp = player.MaxHp / 2;
                var enemy = new Pokemon(
                    PokemonDatabase.All[4],
                    new List<string> { "tackle" },
                    level: 10);
                enemy.CurrentHp = enemy.MaxHp / 2;
                await state.LoseBattle(
                    turns: 4,
                    playerTeam: new[] { player },
                    enemyTeam: new[] { enemy });
            }

            await using var verifyDb = CreateDbContext(schema);
            var result = Assert.Single(await verifyDb.BattleResults
                .Where(item => item.Username == username)
                .ToListAsync());
            Assert.False(result.Won);
            Assert.False(result.IsRivalBattle);
            Assert.Equal(1, result.Round);
            Assert.Equal(4, result.Turns);
            Assert.Equal(0.5, result.PlayerHpRatio, 6);
            Assert.True(result.EnemyHpRatio > 0);
            Assert.Equal(0, result.DifficultyAdjustment);
            Assert.Equal(SkillRatingCalculator.DefaultRating, result.SkillRating);
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
    public void EnemyTeamSizeScalesWithRunRoundInsteadOfUnlockCount()
    {
        Assert.Equal(1, EnemyTeamProvider.GetTeamSizeForRound(1));
        Assert.Equal(1, EnemyTeamProvider.GetTeamSizeForRound(2));
        Assert.Equal(2, EnemyTeamProvider.GetTeamSizeForRound(3));
        Assert.Equal(3, EnemyTeamProvider.GetTeamSizeForRound(5));
        Assert.Equal(5, EnemyTeamProvider.GetTeamSizeForRound(9));
        Assert.Equal(6, EnemyTeamProvider.GetTeamSizeForRound(11));
        Assert.Equal(6, EnemyTeamProvider.GetTeamSizeForRound(999));
        Assert.Equal(1, EnemyTeamProvider.GetTeamSizeForRound(0));
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
    public void UnlockedLegendaryTeamsContainAtMostOneLegendary()
    {
        var excluded = new HashSet<int>();

        foreach (int teamSize in new[] { 5, 6 })
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var team = EnemyTeamProvider.GetRandomTeam(
                    teamSize,
                    721,
                    firstStageOnly: false,
                    excluded,
                    legendaryUnlocked: true);

                Assert.Equal(teamSize, team.Count);
                Assert.InRange(
                    team.Count(entry => EnemyTeamProvider.IsLegendary(entry.Key)),
                    0,
                    1);
            }
        }
    }

    [Fact]
    public void Enemy_species_selection_pressure_increases_with_round_and_skill()
    {
        var pool = PokemonDatabase.All
            .Where(entry => entry.Key <= 300 && !EnemyTeamProvider.IsLegendary(entry.Key))
            .Select(entry => entry.Value)
            .ToList();
        int minimum = pool.Min(EnemyTeamProvider.GetBaseStatTotal);
        int maximum = pool.Max(EnemyTeamProvider.GetBaseStatTotal);
        var early = pool
            .OrderByDescending(data => EnemyTeamProvider.GetSpeciesSelectionWeight(data, minimum, maximum, 1, -3))
            .First();
        var late = pool
            .OrderByDescending(data => EnemyTeamProvider.GetSpeciesSelectionWeight(data, minimum, maximum, 12, 5))
            .First();

        Assert.Equal(maximum, EnemyTeamProvider.GetBaseStatTotal(late));
        Assert.Equal(1, EnemyTeamProvider.GetEvolutionStage(early));
        Assert.True(
            EnemyTeamProvider.GetSpeciesSelectionWeight(late, minimum, maximum, 12, 5)
            > EnemyTeamProvider.GetSpeciesSelectionWeight(late, minimum, maximum, 1, -3));
        Assert.True(
            EnemyTeamProvider.GetSpeciesSelectionWeight(
                pool.OrderBy(EnemyTeamProvider.GetBaseStatTotal).First(),
                minimum,
                maximum,
                12,
                5)
            < EnemyTeamProvider.GetSpeciesSelectionWeight(late, minimum, maximum, 12, 5));
    }

    [Fact]
    public void Enemy_evolution_stage_pressure_follows_round_and_skill()
    {
        var pool = PokemonDatabase.All
            .Where(entry => entry.Key <= 300 && !EnemyTeamProvider.IsLegendary(entry.Key))
            .Select(entry => entry.Value)
            .ToList();
        int minimum = pool.Min(EnemyTeamProvider.GetBaseStatTotal);
        int maximum = pool.Max(EnemyTeamProvider.GetBaseStatTotal);
        var firstStage = PokemonDatabase.All[1];
        var secondStage = PokemonDatabase.All[2];
        var finalStage = PokemonDatabase.All[3];

        Assert.Equal(1, EnemyTeamProvider.GetEvolutionStage(firstStage));
        Assert.Equal(2, EnemyTeamProvider.GetEvolutionStage(secondStage));
        Assert.Equal(3, EnemyTeamProvider.GetEvolutionStage(finalStage));

        double earlyFinal = EnemyTeamProvider.GetSpeciesSelectionWeight(
            finalStage, minimum, maximum, round: 1, skillAdjustment: -3);
        double lateFinal = EnemyTeamProvider.GetSpeciesSelectionWeight(
            finalStage, minimum, maximum, round: 12, skillAdjustment: 5);
        double earlyFirst = EnemyTeamProvider.GetSpeciesSelectionWeight(
            firstStage, minimum, maximum, round: 1, skillAdjustment: -3);
        double lateFirst = EnemyTeamProvider.GetSpeciesSelectionWeight(
            firstStage, minimum, maximum, round: 12, skillAdjustment: 5);
        double lateSecond = EnemyTeamProvider.GetSpeciesSelectionWeight(
            secondStage, minimum, maximum, round: 12, skillAdjustment: 5);
        double lateFinalAtNeutralSkill = EnemyTeamProvider.GetSpeciesSelectionWeight(
            finalStage, minimum, maximum, round: 12, skillAdjustment: 0);

        Assert.True(lateFinal > earlyFinal);
        Assert.True(lateSecond > lateFirst);
        Assert.True(lateFinal > lateFinalAtNeutralSkill);
        Assert.True(lateFinal / lateFirst > earlyFinal / earlyFirst);
    }

    [Fact]
    public void Enemy_evolution_stage_probabilities_follow_the_requested_curve()
    {
        var pool = PokemonDatabase.All
            .Where(entry => entry.Key <= 300 && !EnemyTeamProvider.IsLegendary(entry.Key))
            .ToList();
        int minimum = pool.Min(entry => EnemyTeamProvider.GetBaseStatTotal(entry.Value));
        int maximum = pool.Max(entry => EnemyTeamProvider.GetBaseStatTotal(entry.Value));
        var expected = new Dictionary<int, (double First, double Second, double Final)>
        {
            [1] = (0.6157, 0.2934, 0.0909),
            [5] = (0.4648, 0.3589, 0.1763),
            [10] = (0.3478, 0.4061, 0.2460),
            [15] = (0.2769, 0.4333, 0.2898),
            [20] = (0.2769, 0.4333, 0.2898)
        };

        foreach (var (round, target) in expected)
        {
            var stageWeights = new double[4];
            foreach (var entry in pool)
            {
                stageWeights[EnemyTeamProvider.GetEvolutionStage(entry.Key)] +=
                    EnemyTeamProvider.GetSpeciesSelectionWeight(
                        entry.Value,
                        minimum,
                        maximum,
                        round,
                        skillAdjustment: 0);
            }

            double total = stageWeights.Skip(1).Sum();
            Assert.Equal(target.First, stageWeights[1] / total, 3);
            Assert.Equal(target.Second, stageWeights[2] / total, 3);
            Assert.Equal(target.Final, stageWeights[3] / total, 3);
        }
    }

    [Fact]
    public void High_round_enemy_ability_selection_can_spawn_unaware_but_caps_it_per_team()
    {
        var unawareData = PokemonDatabase.All.Values
            .Where(data => data.AbilityNames.Contains("천진"))
            .OrderByDescending(EnemyTeamProvider.GetBaseStatTotal)
            .First();
        var moves = EnemyTeamProvider.PickProMoveset(unawareData);
        int unawareSelections = Enumerable.Range(0, 500)
            .Count(_ => EnemyTeamProvider.PickProAbility(
                unawareData,
                moves,
                skillAdjustment: 5,
                round: 20) == "천진");

        Assert.True(unawareSelections > 0);
        Assert.NotEqual(
            "천진",
            EnemyTeamProvider.PickProAbility(
                unawareData,
                moves,
                skillAdjustment: 5,
                round: 20,
                unawareAlreadyChosen: true));

        var team = new[] { 399, 400, 528, 399, 400, 528 }
            .Select(id => PokemonDatabase.All[id])
            .ToList();
        bool unawareAlreadyChosen = false;
        int teamUnawareCount = 0;
        foreach (var data in team)
        {
            string ability = EnemyTeamProvider.PickProAbility(
                data,
                EnemyTeamProvider.PickProMoveset(data),
                skillAdjustment: 5,
                round: 20,
                unawareAlreadyChosen);
            if (ability == "천진")
            {
                teamUnawareCount++;
                unawareAlreadyChosen = true;
            }
        }

        Assert.InRange(teamUnawareCount, 0, 1);
    }

    [Fact]
    public void First_stage_only_enemy_pool_excludes_evolved_species()
    {
        var team = EnemyTeamProvider.GetRandomTeam(
            count: 6,
            poolSize: 300,
            firstStageOnly: true,
            excludeIds: new HashSet<int>(),
            legendaryUnlocked: false);

        Assert.NotEmpty(team);
        Assert.All(team, entry => Assert.Equal(1, EnemyTeamProvider.GetEvolutionStage(entry.Key)));
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
                Assert.Equal("기합의띠", restoredLoadout.ChosenItem);
            }
        });
    }

    [Fact]
    public async Task PlayerProgressionSchedules_one_rival_at_fifty_battles_and_rewards_once()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "rival-progress";
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle", "growl" },
                    ChosenAbility = "심록",
                    ChosenItem = "없음",
                    Level = 4
                }
            };
            var rivalLoadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 4,
                    ChosenMoveNames = new List<string> { "ember", "growl" },
                    ChosenAbility = "맹화",
                    ChosenItem = "없음",
                    Level = 9
                }
            };
            var farRivalLoadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 25,
                    ChosenMoveNames = new List<string> { "thunder-shock" },
                    ChosenAbility = "정전기",
                    ChosenItem = "없음",
                    Level = 12
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                db.Users.AddRange(
                    new UserAccount { Username = username, PasswordHash = "test" },
                    new UserAccount { Username = "rival-source", PasswordHash = "test" },
                    new UserAccount { Username = "rival-far", PasswordHash = "test" },
                    new UserAccount { Username = "admin", PasswordHash = "test", IsAdmin = true });
                db.PlayerSkillRatings.AddRange(
                    new PlayerSkillRating { Username = username, Rating = 1000 },
                    new PlayerSkillRating { Username = "rival-source", Rating = 1010 },
                    new PlayerSkillRating { Username = "rival-far", Rating = 1600 },
                    new PlayerSkillRating { Username = "admin", Rating = 1001 });
                await db.SaveChangesAsync();

                var store = new PlayerProgressionStore(db, new FixedRandom(99));
                await store.SaveLatestLoadoutsAsync(username, loadouts);
                await store.RecordTeamSelectionsAsync(username, loadouts);
                await store.SaveLatestLoadoutsAsync("rival-source", rivalLoadouts);
                await store.RecordTeamSelectionsAsync("rival-source", rivalLoadouts);
                await store.SaveLatestLoadoutsAsync("rival-far", farRivalLoadouts);
                await store.SaveLatestLoadoutsAsync("admin", farRivalLoadouts);
                for (var battle = 0; battle < 50; battle++)
                {
                    await store.CompleteBattleAsync(username, loadouts, isRivalBattle: false, won: true);
                }

                var pending = await store.GetPendingRivalAsync(username);
                Assert.NotNull(pending);
                Assert.Equal("rival-source", pending.Username);
                var rival = Assert.Single(pending.Loadouts);
                Assert.Equal(4, rival.PokemonId);
                Assert.Equal(9, rival.Level);
                Assert.All(rival.ChosenMoveNames, move => Assert.Contains(move, PokemonDatabase.All[4].MoveNames));

                await store.CompleteBattleAsync(username, loadouts, isRivalBattle: true, won: true);
                await store.CompleteBattleAsync(username, loadouts, isRivalBattle: true, won: true);
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restored = await new PlayerProgressionStore(freshDb).LoadAsync(username);
                Assert.Equal(50, restored.completedBattles);
                Assert.False(restored.rivalPending);
                Assert.Contains(restored.messages, message => message.Title == "라이벌전 승리");
                Assert.Contains(restored.messages, message => message.Title == "기술머신 보상");
                Assert.Single(restored.machines);
                Assert.Equal(1, restored.machines[0].Quantity);
            }

            await using (var isolatedDb = CreateDbContext(schema))
            {
                var other = await new PlayerProgressionStore(isolatedDb).LoadAsync("other-user");
                Assert.Equal(0, other.completedBattles);
                Assert.Empty(other.messages);
                Assert.Empty(other.machines);
            }
        });
    }

    [Fact]
    public async Task Pending_rival_without_eligible_other_user_is_skipped()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "rival-only-user";
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle" },
                    ChosenAbility = "심록",
                    ChosenItem = "없음",
                    Level = 4
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                db.Users.AddRange(
                    new UserAccount { Username = username, PasswordHash = "test" },
                    new UserAccount { Username = "admin", PasswordHash = "test", IsAdmin = true });
                await db.SaveChangesAsync();

                var store = new PlayerProgressionStore(db, new FixedRandom(0));
                await store.SaveLatestLoadoutsAsync(username, loadouts);
                await store.RecordTeamSelectionsAsync(username, loadouts);
                await store.SaveLatestLoadoutsAsync("admin", loadouts);
                for (var battle = 0; battle < 50; battle++)
                {
                    await store.CompleteBattleAsync(username, loadouts, isRivalBattle: false, won: true);
                }

                Assert.Null(await store.GetPendingRivalAsync(username));
            }

            await using var verifyDb = CreateDbContext(schema);
            var restored = await new PlayerProgressionStore(verifyDb)
                .LoadAsync(username);
            Assert.False(restored.rivalPending);
        });
    }

    [Fact]
    public async Task General_victory_has_a_chance_to_grant_a_technical_machine_and_mail()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle" },
                    ChosenAbility = "심록",
                    ChosenItem = "없음",
                    Level = 4
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                var store = new PlayerProgressionStore(db, new FixedRandom(0));
                await store.CompleteBattleAsync(
                    "general-machine-reward",
                    loadouts,
                    isRivalBattle: false,
                    won: true);
            }

            await using var freshDb = CreateDbContext(schema);
            var restored = await new PlayerProgressionStore(freshDb, new FixedRandom(99))
                .LoadAsync("general-machine-reward");

            Assert.Contains(restored.messages, message => message.Title == "기술머신 획득");
            Assert.Single(restored.machines);
            Assert.Equal(1, restored.machines[0].Quantity);
        });
    }

    [Fact]
    public async Task Technical_machine_rewards_use_unowned_machine_only_moves_and_vary_between_grants()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "technical-machine-reward-pool";
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle" },
                    ChosenAbility = "심록",
                    ChosenItem = TeamLoadoutRules.NoItem,
                    Level = 4
                }
            };

            await using var db = CreateDbContext(schema);
            var store = new PlayerProgressionStore(db, new FixedRandom(0));
            string? firstReward = await store.GrantTechnicalMachineRewardAsync(username, loadouts);
            string? secondReward = await store.GrantTechnicalMachineRewardAsync(username, loadouts);
            var machines = await db.TechnicalMachines
                .Where(machine => machine.Username == username && machine.Quantity > 0)
                .ToListAsync();

            Assert.NotNull(firstReward);
            Assert.NotNull(secondReward);
            Assert.Equal(2, machines.Count);
            Assert.All(
                machines,
                machine => Assert.Contains(machine.MoveKey, PokemonDatabase.All[1].MachineOnlyMoveNames));
            Assert.All(machines, machine => Assert.Equal(1, machine.Quantity));
        });
    }

    [Fact]
    public async Task Technical_machine_reward_count_includes_available_machines_duplicates_and_apples()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "technical-machine-reward-count";
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 1,
                    ChosenMoveNames = new List<string> { "tackle" },
                    ChosenAbility = "심록",
                    ChosenItem = TeamLoadoutRules.NoItem,
                    Level = 4
                }
            };

            await using (var db = CreateDbContext(schema))
            {
                var store = new PlayerProgressionStore(db);
                await store.SaveLatestLoadoutsAsync(username, loadouts);
                db.TechnicalMachines.AddRange(
                    new TechnicalMachineInventory
                    {
                        Username = username,
                        MoveKey = "cut",
                        Quantity = 1
                    },
                    new TechnicalMachineInventory
                    {
                        Username = username,
                        MoveKey = "toxic",
                        Quantity = 3
                    });
                await db.SaveChangesAsync();
            }

            await using var verifyDb = CreateDbContext(schema);
            int count = await new PlayerProgressionStore(verifyDb)
                .CountTechnicalMachineRewardsAsync(username);

            Assert.Equal(5, count);
        });
    }

    [Fact]
    public async Task Technical_machine_fallback_excludes_owned_moves()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "technical-machine-fallback";
            var loadouts = new List<PokemonLoadout>
            {
                new()
                {
                    PokemonId = 201,
                    ChosenMoveNames = new List<string> { "hidden-power" },
                    ChosenAbility = "부유",
                    ChosenItem = TeamLoadoutRules.NoItem,
                    Level = 4
                }
            };

            await using var db = CreateDbContext(schema);
            var store = new PlayerProgressionStore(db, new FixedRandom(0));
            string? firstReward = await store.GrantTechnicalMachineRewardAsync(username, loadouts);
            string? secondReward = await store.GrantTechnicalMachineRewardAsync(username, loadouts);

            Assert.Equal(MoveDatabase.All["hidden-power"].Name, firstReward);
            Assert.Null(secondReward);
            var machine = Assert.Single(await db.TechnicalMachines
                .Where(item => item.Username == username)
                .ToListAsync());
            Assert.Equal("hidden-power", machine.MoveKey);
            Assert.Equal(1, machine.Quantity);
        });
    }

    [Fact]
    public async Task TechnicalMachineSelectionConsumesExactlyOneAndSavedMoveSurvivesReload()
    {
        await WithTemporarySchema(async schema =>
        {
            await CreatePlayerRunsTable(schema);
            await CreateProgressionTables(schema);
            const string username = "technical-machine-selection";
            var initialLoadout = new PokemonLoadout
            {
                PokemonId = 1,
                ChosenMoveNames = new List<string> { "tackle" },
                ChosenAbility = "심록",
                ChosenItem = TeamLoadoutRules.NoItem,
                Level = 1
            };

            await using (var seedDb = CreateDbContext(schema))
            {
                await new RunStore(seedDb).Save(
                    username,
                    0,
                    0,
                    new List<PokemonLoadout> { initialLoadout },
                    0);
                seedDb.TechnicalMachines.Add(new TechnicalMachineInventory
                {
                    Username = username,
                    MoveKey = "cut",
                    Quantity = 1
                });
                await seedDb.SaveChangesAsync();
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
                    new SkillRatingService(db),
                    new PlayerProgressionStore(db));

                await state.LoadRunForCurrentUser();
                Assert.Equal(1, state.TechnicalMachines.Single(machine => machine.MoveKey == "cut").Quantity);

                var configuredLoadout = state.PlayerLoadouts.Single().Clone();
                configuredLoadout.ChosenMoveNames.Add("cut");
                Assert.True(await state.TryLearnTechnicalMachineAsync("cut"));
                Assert.Equal(0, state.TechnicalMachines.Single(machine => machine.MoveKey == "cut").Quantity);
                await state.SetPlayerLoadouts(new List<PokemonLoadout> { configuredLoadout });
            }

            await using (var freshDb = CreateDbContext(schema))
            {
                var restoredProgress = await new PlayerProgressionStore(freshDb).LoadAsync(username);
                Assert.Empty(restoredProgress.machines);

                var restoredState = new GameState(
                    new InMemoryScoreStore(),
                    new InMemoryPresetStore(),
                    new UnlockService(freshDb, currentUser),
                    new RunStore(freshDb),
                    currentUser,
                    new SkillRatingService(freshDb),
                    new PlayerProgressionStore(freshDb));
                await restoredState.LoadRunForCurrentUser();

                var restoredLoadout = Assert.Single(restoredState.PlayerLoadouts);
                Assert.Contains("cut", restoredLoadout.ChosenMoveNames);
                Assert.False(await restoredState.TryLearnTechnicalMachineAsync("cut"));
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
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "PlayerRuns"
                        ADD COLUMN IF NOT EXISTS "RunMetaStateJson" TEXT NOT NULL DEFAULT '{{}}';
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
                        "RoundPerformancesJson" TEXT NOT NULL DEFAULT '[]',
                        "RunMetaStateJson" TEXT NOT NULL DEFAULT '{{}}'
            );
            """);
        await db.Database.ExecuteSqlRawAsync("""
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

    private static async Task CreateProgressionTables(string schema)
    {
        await using var db = CreateDbContext(schema);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE "Users" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "IsAdmin" BOOLEAN NOT NULL DEFAULT FALSE
            );
            CREATE UNIQUE INDEX "IX_Users_Username"
                ON "Users" ("Username");
            CREATE TABLE "PlayerProgressions" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "CompletedBattles" INTEGER NOT NULL DEFAULT 0,
                "RivalPending" BOOLEAN NOT NULL DEFAULT FALSE,
                "RivalNumber" INTEGER NOT NULL DEFAULT 0,
                "LatestLoadoutsJson" TEXT NOT NULL DEFAULT '[]',
                "MovePreferencesJson" TEXT NOT NULL DEFAULT '{{}}',
                "UpdatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE UNIQUE INDEX "IX_PlayerProgressions_Username"
                ON "PlayerProgressions" ("Username");
            CREATE TABLE "MailboxMessages" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "DeduplicationKey" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "Body" TEXT NOT NULL,
                "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
                "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE UNIQUE INDEX "IX_MailboxMessages_Username_DeduplicationKey"
                ON "MailboxMessages" ("Username", "DeduplicationKey");
            CREATE TABLE "TechnicalMachines" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "MoveKey" TEXT NOT NULL,
                "Quantity" INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX "IX_TechnicalMachines_Username_MoveKey"
                ON "TechnicalMachines" ("Username", "MoveKey");
            CREATE TABLE "UnlockedPokemons" (
                "Id" SERIAL PRIMARY KEY,
                "Username" TEXT NOT NULL,
                "PokemonId" INTEGER NOT NULL
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
            CREATE INDEX "IX_BattleResults_Username_CreatedAtUtc"
                ON "BattleResults" ("Username", "CreatedAtUtc" DESC);
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

    private sealed class FixedRandom : Random
    {
        private readonly int value;

        public FixedRandom(int value)
        {
            this.value = value;
        }

        public override int Next(int maxValue) => Math.Min(value, maxValue - 1);
    }
}
