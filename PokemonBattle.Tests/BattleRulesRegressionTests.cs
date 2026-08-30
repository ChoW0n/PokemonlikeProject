using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class BattleRulesRegressionTests
{
    [Fact]
    public void TryUseMove_never_decrements_pp_below_zero()
    {
        var pokemon = CreatePokemon(25, "thunderbolt");
        int maxPp = pokemon.CurrentPP["thunderbolt"];

        int successfulUses = 0;
        for (int i = 0; i < maxPp + 5; i++)
        {
            if (pokemon.TryUseMove("thunderbolt")) successfulUses++;
        }

        Assert.Equal(maxPp, successfulUses);
        Assert.Equal(0, pokemon.CurrentPP["thunderbolt"]);
        Assert.False(pokemon.CanUseMove("thunderbolt"));
    }

    [Fact]
    public void Choice_item_locks_move_and_switching_clears_the_lock()
    {
        var pokemon = CreatePokemon(25, "thunderbolt", "tackle", heldItem: "구애안경");
        var engine = CreateEngine();

        Assert.True(pokemon.TryUseMove("thunderbolt"));
        Assert.Equal("thunderbolt", pokemon.ChoiceLockedMove);
        Assert.False(pokemon.CanUseMove("tackle"));

        engine.PrepareSwitchOut(pokemon);

        Assert.Null(pokemon.ChoiceLockedMove);
        Assert.True(pokemon.CanUseMove("tackle"));
        Assert.Equal(0, pokemon.StatStages["attack"]);
        Assert.Equal(0, pokemon.TurnsOnField);
    }

    [Fact]
    public async Task TakeTurn_uses_struggle_when_no_move_has_pp_and_deals_damage()
    {
        var attacker = CreatePokemon(25, "tackle");
        var defender = CreatePokemon(1, "tackle");
        attacker.CurrentPP["tackle"] = 0;
        int defenderHpBefore = defender.CurrentHp;
        var events = new List<BattleEvent>();
        var engine = CreateEngine();

        var result = await engine.TakeTurnAsync(
            attacker,
            defender,
            moveKey: null,
            attackerIsHero: true,
            emit: battleEvent =>
            {
                events.Add(battleEvent);
                return Task.CompletedTask;
            });

        Assert.Null(result.FaintedPokemon);
        Assert.True(defender.CurrentHp < defenderHpBefore);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("몸부림쳤다", StringComparison.Ordinal) == true);
        Assert.Equal(0, attacker.CurrentPP["tackle"]);

        await engine.ApplyEndOfTurnEffectsAsync(
            new[] { attacker, defender },
            battleEvent =>
            {
                events.Add(battleEvent);
                return Task.CompletedTask;
            });

        Assert.Equal(1, attacker.TurnsOnField);
        Assert.Equal(1, defender.TurnsOnField);
    }

    [Fact]
    public async Task Electric_absorption_abilities_only_activate_for_electric_moves()
    {
        var abilityCases = new[]
        {
            new { Ability = "축전", ExpectedStage = 0, ExpectedMessage = "축전", Heals = true },
            new { Ability = "피뢰침", ExpectedStage = 1, ExpectedMessage = "피뢰침", Heals = false }
        };

        foreach (var abilityCase in abilityCases)
        {
            var defender = CreatePokemon(1, "tackle", ability: abilityCase.Ability);
            int maxHp = defender.MaxHp;
            defender.CurrentHp = maxHp / 2;
            int hpBefore = defender.CurrentHp;
            int stageBefore = defender.StatStages["special-attack"];
            var electricAttacker = CreatePokemon(25, "thunderbolt");
            var electricEvents = new List<BattleEvent>();

            await CreateEngine().TakeTurnAsync(
                electricAttacker,
                defender,
                "thunderbolt",
                attackerIsHero: false,
                emit: battleEvent =>
                {
                    electricEvents.Add(battleEvent);
                    return Task.CompletedTask;
                });

            Assert.Equal(abilityCase.Heals ? hpBefore + maxHp / 4 : hpBefore, defender.CurrentHp);
            Assert.Equal(stageBefore + abilityCase.ExpectedStage, defender.StatStages["special-attack"]);
            Assert.Contains(electricEvents, battleEvent =>
                battleEvent.Message?.Contains(abilityCase.ExpectedMessage, StringComparison.Ordinal) == true);

            var nonElectricDefender = CreatePokemon(1, "tackle", ability: abilityCase.Ability);
            nonElectricDefender.CurrentHp = nonElectricDefender.MaxHp - 10;
            int nonElectricHpBefore = nonElectricDefender.CurrentHp;
            int nonElectricStageBefore = nonElectricDefender.StatStages["special-attack"];
            var normalAttacker = CreatePokemon(25, "tackle");

            await CreateEngine().TakeTurnAsync(
                normalAttacker,
                nonElectricDefender,
                "tackle",
                attackerIsHero: false,
                emit: _ => Task.CompletedTask);

            Assert.Equal(nonElectricHpBefore - ExpectedDamage(normalAttacker, nonElectricDefender, "tackle"), nonElectricDefender.CurrentHp);
            Assert.Equal(nonElectricStageBefore, nonElectricDefender.StatStages["special-attack"]);
        }
    }

    [Fact]
    public async Task Flash_fire_activates_for_fire_moves_but_not_normal_moves()
    {
        var fireDefender = CreatePokemon(1, "tackle", ability: "타오르는불꽃");
        var fireAttacker = CreatePokemon(4, "ember");
        var fireEvents = new List<BattleEvent>();

        await CreateEngine().TakeTurnAsync(
            fireAttacker,
            fireDefender,
            "ember",
            attackerIsHero: false,
            emit: battleEvent =>
            {
                fireEvents.Add(battleEvent);
                return Task.CompletedTask;
            });

        Assert.True(fireDefender.FlashFireActive);
        Assert.Contains(fireEvents, battleEvent =>
            battleEvent.Message?.Contains("타오르는불꽃", StringComparison.Ordinal) == true);

        var normalDefender = CreatePokemon(1, "tackle", ability: "타오르는불꽃");
        var normalAttacker = CreatePokemon(25, "tackle");

        await CreateEngine().TakeTurnAsync(
            normalAttacker,
            normalDefender,
            "tackle",
            attackerIsHero: false,
            emit: _ => Task.CompletedTask);

        Assert.False(normalDefender.FlashFireActive);
    }

    private static Pokemon CreatePokemon(
        int pokemonId,
        params string[] moves)
    {
        return new Pokemon(PokemonDatabase.All[pokemonId], moves.ToList(), level: 50);
    }

    private static Pokemon CreatePokemon(
        int pokemonId,
        string move,
        string? secondMove = null,
        string ability = "",
        string heldItem = "없음")
    {
        var moves = secondMove == null ? new[] { move } : new[] { move, secondMove };
        return new Pokemon(PokemonDatabase.All[pokemonId], moves.ToList(), ability, heldItem, level: 50);
    }

    private static BattleEngine CreateEngine() => new(new Random(1234), Array.Empty<IBattleEffectHandler>());

    private static int ExpectedDamage(Pokemon attacker, Pokemon defender, string moveKey)
    {
        var move = MoveDatabase.All[moveKey];
        int attack = move.IsSpecial ? attacker.EffectiveSpAtk : attacker.EffectiveAtk;
        int defense = move.IsSpecial ? defender.EffectiveSpDef : defender.EffectiveDef;
        return Math.Max(0, (int)(move.Power * ((double)attack / Math.Max(defense, 1))));
    }
}