using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public class RunMetaRegressionTests
{
    [Fact]
    public void NormalizeRemovesInvalidAndDuplicateRunMetaChoices()
    {
        var state = RunMetaCatalog.Normalize(new RunMetaState
        {
            LegacyIds = new List<string> { "first-strike", "first-strike", "unknown" },
            PendingLegacyChoices = new List<string> { "first-strike", "affliction", "affliction", "unknown" },
            LegacyClaimsRemaining = 99,
            StolenMoves =
            [
                new() { PokemonId = 1, MoveKey = "tackle" },
                new() { PokemonId = 1, MoveKey = "tackle" },
                new() { PokemonId = 1, MoveKey = "growl" },
                new() { PokemonId = 9999, MoveKey = "tackle" }
            ],
            PendingStolenMoveChoices =
            [
                new() { SourcePokemonId = 4, MoveKey = "scratch" },
                new() { SourcePokemonId = 4, MoveKey = "scratch" },
                new() { SourcePokemonId = 4, MoveKey = "growl" }
            ]
        });

        Assert.Equal(new[] { "first-strike" }, state.LegacyIds);
        Assert.Equal(new[] { "affliction" }, state.PendingLegacyChoices);
        Assert.Equal(8, state.LegacyClaimsRemaining);
        Assert.Single(state.StolenMoves);
        Assert.Single(state.PendingStolenMoveChoices);
    }

    [Fact]
    public void FullMovesetCanReplaceOneMoveWithAStolenMove()
    {
        var loadout = new PokemonLoadout
        {
            PokemonId = 1,
            ChosenMoveNames = new List<string>
                { "tackle", "vine-whip", "growl", "growth" }
        };

        Assert.True(RunMetaCatalog.TryApplyStolenMove(
            loadout, "seed-bomb", "growl"));
        Assert.Equal(
            new[] { "tackle", "vine-whip", "seed-bomb", "growth" },
            loadout.ChosenMoveNames);
        Assert.False(RunMetaCatalog.TryApplyStolenMove(
            loadout, "seed-bomb", "growth"));
        Assert.False(RunMetaCatalog.TryApplyStolenMove(
            loadout, "power-whip"));
    }

    [Fact]
    public void StolenMoveEligibilityUsesTheSourceSpeciesLearnset()
    {
        Assert.True(RunMetaCatalog.IsStolenMoveEligible("seed-bomb", 1));
        Assert.False(RunMetaCatalog.IsStolenMoveEligible("thunderbolt", 1));
        Assert.False(RunMetaCatalog.IsStolenMoveEligible("growl", 1));
        Assert.False(RunMetaCatalog.IsStolenMoveEligible("tackle", 9999));
    }

    [Fact]
    public void RunLegacyPowerEffectsOnlyApplyToThePlayerSide()
    {
        var attacker = NewPokemon(1);
        var defender = NewPokemon(4);
        var handler = new RunMetaEffectHandler();
        var firstStrike = new RunMetaState
        {
            LegacyIds = new List<string> { "first-strike" }
        };

        var playerAttack = NewPowerContext(
            attacker, defender, firstStrike, attackerIsHero: true, movedFirst: true);
        handler.ModifyPower(playerAttack);
        Assert.Equal(125, playerAttack.Power);

        var enemyAttack = NewPowerContext(
            attacker, defender, firstStrike, attackerIsHero: false, movedFirst: true);
        handler.ModifyPower(enemyAttack);
        Assert.Equal(100, enemyAttack.Power);
    }

    [Fact]
    public void RunLegacyUsesStatusAndHighHpGuards()
    {
        var attacker = NewPokemon(1);
        var defender = NewPokemon(4);
        defender.Status = StatusCondition.Burn;
        var handler = new RunMetaEffectHandler();

        var afflicted = NewPowerContext(
            attacker,
            defender,
            new RunMetaState { LegacyIds = new List<string> { "affliction" } },
            attackerIsHero: true,
            movedFirst: false);
        handler.ModifyPower(afflicted);
        Assert.Equal(125, afflicted.Power);

        var highHp = NewPowerContext(
            attacker,
            defender,
            new RunMetaState { LegacyIds = new List<string> { "iron-vitality" } },
            attackerIsHero: false,
            movedFirst: false);
        handler.ModifyPower(highHp);
        Assert.Equal(75, highHp.Power);

        defender.CurrentHp = defender.MaxHp / 2;
        handler.ModifyPower(highHp);
        Assert.Equal(75, highHp.Power);
    }

    [Fact]
    public async Task LastBreathRestoresOneTwelfthAtTurnEnd()
    {
        var pokemon = NewPokemon(1);
        pokemon.CurrentHp = pokemon.MaxHp / 2;
        int before = pokemon.CurrentHp;
        var handler = new RunMetaEffectHandler();
        var context = new BattleEndOfTurnContext(
            pokemon,
            _ => Task.CompletedTask,
            runMeta: new RunMetaState { LegacyIds = new List<string> { "last-breath" } },
            isHero: true);

        await handler.EndOfTurnAsync(context);

        Assert.Equal(before + Math.Max(1, pokemon.MaxHp / 16), pokemon.CurrentHp);
    }

    [Fact]
    public async Task LastBreath_only_restores_the_player_side_at_turn_end()
    {
        var hero = NewPokemon(1);
        var enemy = NewPokemon(4);
        hero.CurrentHp = hero.MaxHp / 2;
        enemy.CurrentHp = enemy.MaxHp / 2;
        int heroBefore = hero.CurrentHp;
        int enemyBefore = enemy.CurrentHp;
        var engine = new BattleEngine(
            new Random(1),
            new IBattleEffectHandler[] { new RunMetaEffectHandler() });
        engine.ConfigureRunMeta(new RunMetaState
        {
            LegacyIds = new List<string> { "last-breath" }
        });

        await engine.ApplyEndOfTurnEffectsAsync(
            new[] { hero, enemy },
            _ => Task.CompletedTask);

        Assert.Equal(heroBefore + Math.Max(1, hero.MaxHp / 16), hero.CurrentHp);
        Assert.Equal(enemyBefore, enemy.CurrentHp);
    }

    [Fact]
    public void ExpandedRunMetaCatalogContainsEightLegaciesAndThreeCovenants()
    {
        Assert.Equal(8, RunMetaCatalog.Legacies.Count);
        Assert.Equal(3, RunMetaCatalog.RiskCovenants.Count);
        Assert.Contains(RunMetaCatalog.Legacies, legacy => legacy.Id == "hunters-eye");
        Assert.Contains(RunMetaCatalog.Legacies, legacy => legacy.Id == "chain-breaker");
        Assert.Contains(RunMetaCatalog.RiskCovenants, covenant => covenant.Id == "dark-pact");
        Assert.Contains(RunMetaCatalog.RiskCovenants, covenant => covenant.Id == "rampage-curse");
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    public void LegacyClaimsAreScheduledEveryTwoWins(int totalWins, int expectedClaims)
    {
        Assert.Equal(
            expectedClaims,
            RunMetaCatalog.ScheduledLegacyClaimsForWin(totalWins));
    }

    [Fact]
    public void LowHpWeatherAndDarkCovenantPowerEffectsUseTheirDeclaredGuards()
    {
        var attacker = NewPokemon(1);
        var defender = NewPokemon(4);
        var handler = new RunMetaEffectHandler();

        attacker.CurrentHp = attacker.MaxHp / 4;
        var lowHp = NewPowerContext(
            attacker,
            defender,
            new RunMetaState { LegacyIds = new List<string> { "judges-scale" } },
            attackerIsHero: true,
            movedFirst: false);
        handler.ModifyPower(lowHp);
        Assert.Equal(120, lowHp.Power, 6);

        BattleWeather.Set(BattleWeather.Rain);
        var weather = NewPowerContext(
            attacker,
            defender,
            new RunMetaState { LegacyIds = new List<string> { "calm-before-storm" } },
            attackerIsHero: true,
            movedFirst: false);
        handler.ModifyPower(weather);
        Assert.Equal(110, weather.Power, 6);
        BattleWeather.Reset();

        attacker.SelectedAbility = "심록";
        var darkPact = NewPowerContext(
            attacker,
            defender,
            new RunMetaState
            {
                RiskCovenantId = "dark-pact",
                RiskCovenantAccepted = true
            },
            attackerIsHero: false,
            movedFirst: false);
        handler.ModifyPower(darkPact);
        Assert.Equal(115, darkPact.Power, 6);
    }

    [Fact]
    public void RampagePenaltyAndChainBreakerModifyPlayerBattleStats()
    {
        var pokemon = NewPokemon(1);
        int originalMaxHp = pokemon.MaxHp;
        pokemon.ApplyRunModifiers(10, resistsStatusStatPenalties: true);

        Assert.Equal(Math.Max(1, originalMaxHp * 90 / 100), pokemon.MaxHp);
        Assert.Equal(pokemon.MaxHp, pokemon.CurrentHp);

        pokemon.Status = StatusCondition.Burn;
        Assert.Equal((int)(pokemon.Atk * 0.75), pokemon.EffectiveAtk);
        pokemon.Status = StatusCondition.Paralysis;
        Assert.Equal((int)(pokemon.Spd * 0.75), pokemon.EffectiveSpd);
    }

    [Fact]
    public void BattlefieldImprintSetsTheInitialEnvironment()
    {
        var hero = NewPokemon(1);
        var enemy = NewPokemon(4);
        var engine = new BattleEngine(new Random(1), Array.Empty<IBattleEffectHandler>());

        engine.InitializeWeather(
            hero,
            enemy,
            initialWeather: BattleWeather.Rain,
            initialField: BattleField.Electric);

        Assert.Equal(BattleWeather.Rain, BattleWeather.Current);
        Assert.Equal(BattleField.Electric, BattleField.Current);
        Assert.Equal(5, BattleField.TurnsRemaining);
        BattleWeather.Reset();
        BattleField.Reset();
    }

    [Fact]
    public async Task CalmFieldTemporarilyIgnoresBothSidesStatStages()
    {
        BattleField.Reset();
        try
        {
            var baselineAttacker = NewPokemon(1);
            var baselineDefender = NewPokemon(4);
            int baselineDamage = await DealTackleDamageAsync(
                baselineAttacker, baselineDefender);
            double baselineAccuracy = MoveRuleMetadata.EffectiveAccuracy(
                "thunder",
                MoveDatabase.All["thunder"],
                baselineAttacker,
                baselineDefender);

            var hero = NewPokemon(1);
            var enemy = NewPokemon(4);
            hero.ChangeStage("attack", 2);
            hero.ChangeStage("accuracy", 2);
            hero.ChangeStage("special-attack", 2);
            enemy.ChangeStage("evasion", 2);
            enemy.ChangeStage("special-defense", 2);

            BattleField.Set(BattleField.Calm, turns: 5);
            int sealedDamage = await DealTackleDamageAsync(hero, enemy);

            Assert.Equal(2, hero.StatStages["attack"]);
            Assert.Equal(2, hero.StatStages["accuracy"]);
            Assert.Equal(2, hero.StatStages["special-attack"]);
            Assert.Equal(2, enemy.StatStages["evasion"]);
            Assert.Equal(2, enemy.StatStages["special-defense"]);
            Assert.Equal(hero.Atk, hero.EffectiveAtkAgainst(enemy));
            Assert.Equal(hero.SpAtk, hero.EffectiveSpAtkAgainst(enemy));
            Assert.Equal(enemy.Def, enemy.EffectiveDefAgainst(hero));
            Assert.Equal(enemy.SpDef, enemy.EffectiveSpDefAgainst(hero));
            Assert.Equal(
                baselineAccuracy,
                MoveRuleMetadata.EffectiveAccuracy(
                    "thunder",
                    MoveDatabase.All["thunder"],
                    hero,
                    enemy),
                8);
            Assert.Equal(baselineDamage, sealedDamage);
            Assert.Equal(5, BattleField.TurnsRemaining);

            for (int turn = 0; turn < 4; turn++)
            {
                Assert.False(BattleField.AdvanceTurn());
                Assert.Equal(BattleField.Calm, BattleField.Current);
            }

            Assert.True(BattleField.AdvanceTurn());
            Assert.Equal(BattleField.None, BattleField.Current);
            Assert.Equal(0, BattleField.TurnsRemaining);
            Assert.Equal(2, hero.StatStages["attack"]);
            Assert.Equal(2, enemy.StatStages["evasion"]);

            int damageAfterField = await DealTackleDamageAsync(hero, enemy);
            Assert.True(damageAfterField > sealedDamage);
            Assert.True(hero.EffectiveAtkAgainst(enemy) > hero.Atk);

            var enemyBaselineAttacker = NewPokemon(445, level: 50);
            var heroBaselineDefender = NewPokemon(1, level: 50);
            int enemyBaselineDamage = await DealTackleDamageAsync(
                enemyBaselineAttacker, heroBaselineDefender);
            var enemyAttacker = NewPokemon(445, level: 50);
            var heroDefender = NewPokemon(1, level: 50);
            enemyAttacker.ChangeStage("attack", 2);
            enemyAttacker.ChangeStage("accuracy", 2);
            heroDefender.ChangeStage("evasion", 2);

            BattleField.Set(BattleField.Calm, turns: 5);
            int enemySideSealedDamage = await DealTackleDamageAsync(
                enemyAttacker, heroDefender);

            Assert.Equal(enemyBaselineDamage, enemySideSealedDamage);
            Assert.Equal(2, enemyAttacker.StatStages["attack"]);
            Assert.Equal(2, enemyAttacker.StatStages["accuracy"]);
            Assert.Equal(2, heroDefender.StatStages["evasion"]);
            Assert.Equal(
                enemyAttacker.Atk,
                enemyAttacker.EffectiveAtkAgainst(heroDefender));

            for (int turn = 0; turn < 5; turn++)
            {
                BattleField.AdvanceTurn();
            }

            int enemyDamageAfterField = await DealTackleDamageAsync(
                enemyAttacker, heroDefender);
            Assert.True(enemyDamageAfterField > enemySideSealedDamage);
        }
        finally
        {
            BattleField.Reset();
        }
    }

    [Fact]
    public void BattlefieldCatalogIncludesTheStatSealingImprint()
    {
        Assert.Equal(5, RunMetaCatalog.BattlefieldImprints.Count);
        var imprint = RunMetaCatalog.Battlefield("stillness-sanctum");

        Assert.NotNull(imprint);
        Assert.Equal(BattleWeather.Clear, imprint!.Weather);
        Assert.Equal(BattleField.Calm, imprint.Field);
        Assert.Contains("랭크", imprint.Description, StringComparison.Ordinal);
        Assert.Contains("랭크", BattleEnvironmentDescriptions.Field(BattleField.Calm));
    }

    private static Pokemon NewPokemon(int pokemonId, int level = 10) =>
        new(PokemonDatabase.All[pokemonId], new List<string> { "tackle" }, level: level);

    private static async Task<int> DealTackleDamageAsync(
        Pokemon attacker,
        Pokemon defender)
    {
        int before = defender.CurrentHp;
        var engine = new BattleEngine(
            new FixedRandom(99),
            new IBattleEffectHandler[]
            {
                new MoveEffectHandler(),
                new ContactReactionEffectHandler(),
                new AbilityLifecycleEffectHandler(),
                new DamageModifierEffectHandler()
            });
        await engine.TakeTurnAsync(
            attacker, defender, "tackle", true, _ => Task.CompletedTask);
        return before - defender.CurrentHp;
    }

    private static BattlePowerContext NewPowerContext(
        Pokemon attacker,
        Pokemon defender,
        RunMetaState meta,
        bool attackerIsHero,
        bool movedFirst) =>
        new(
            attacker,
            defender,
            MoveDatabase.All["tackle"],
            PokemonType.Normal,
            makesContact: true,
            power: 100,
            moveKey: "tackle",
            attackerMovedFirst: movedFirst,
            runMeta: meta,
            attackerIsHero: attackerIsHero);

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