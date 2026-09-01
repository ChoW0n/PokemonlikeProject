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
            runMeta: new RunMetaState { LegacyIds = new List<string> { "last-breath" } });

        await handler.EndOfTurnAsync(context);

        Assert.Equal(before + Math.Max(1, pokemon.MaxHp / 12), pokemon.CurrentHp);
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
        BattleWeather.Reset();
        BattleField.Reset();
    }

    private static Pokemon NewPokemon(int pokemonId) =>
        new(PokemonDatabase.All[pokemonId], new List<string> { "tackle" }, level: 10);

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
}