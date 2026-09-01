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
                new() { SourcePokemonId = 4, MoveKey = "tackle" },
                new() { SourcePokemonId = 4, MoveKey = "tackle" },
                new() { SourcePokemonId = 4, MoveKey = "growl" }
            ]
        });

        Assert.Equal(new[] { "first-strike" }, state.LegacyIds);
        Assert.Equal(new[] { "affliction" }, state.PendingLegacyChoices);
        Assert.Equal(4, state.LegacyClaimsRemaining);
        Assert.Single(state.StolenMoves);
        Assert.Single(state.PendingStolenMoveChoices);
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
        Assert.Equal(120, playerAttack.Power);

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
        Assert.Equal(120, afflicted.Power);

        var highHp = NewPowerContext(
            attacker,
            defender,
            new RunMetaState { LegacyIds = new List<string> { "iron-vitality" } },
            attackerIsHero: false,
            movedFirst: false);
        handler.ModifyPower(highHp);
        Assert.Equal(80, highHp.Power);

        defender.CurrentHp = defender.MaxHp / 2;
        handler.ModifyPower(highHp);
        Assert.Equal(80, highHp.Power);
    }

    [Fact]
    public async Task LastBreathRestoresOneSixteenthAtTurnEnd()
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

        Assert.Equal(before + Math.Max(1, pokemon.MaxHp / 16), pokemon.CurrentHp);
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