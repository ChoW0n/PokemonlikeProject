using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class WeatherAndFieldRegressionTests
{
    [Fact]
    public void Weather_ball_changes_type_and_power_for_active_weather()
    {
        var move = MoveDatabase.All["weather-ball"];

        try
        {
            BattleWeather.Current = BattleWeather.Rain;

            Assert.Equal(PokemonType.Water, MoveRuleMetadata.ResolveMoveType("weather-ball", move));
            Assert.Equal(move.Power * 2, MoveRuleMetadata.EffectivePower("weather-ball", move));
        }
        finally
        {
            BattleWeather.Reset();
        }
    }

    [Fact]
    public void Weather_changes_accuracy_and_recovery_amount()
    {
        try
        {
            BattleWeather.Current = BattleWeather.Rain;
            Assert.Equal(100, MoveRuleMetadata.EffectiveAccuracy("thunder", MoveDatabase.All["thunder"]));
            Assert.Equal(25, MoveRuleMetadata.RecoveryAmount(
                "synthesis", MoveDatabase.All["synthesis"], 100));

            BattleWeather.Current = BattleWeather.Sun;
            Assert.Equal(50, MoveRuleMetadata.EffectiveAccuracy("thunder", MoveDatabase.All["thunder"]));
            Assert.Equal(66, MoveRuleMetadata.RecoveryAmount(
                "synthesis", MoveDatabase.All["synthesis"], 100));
        }
        finally
        {
            BattleWeather.Reset();
        }
    }

    [Fact]
    public async Task Weather_move_updates_the_shared_battle_weather()
    {
        try
        {
            BattleWeather.Reset();
            BattleField.Reset();
            var attacker = CreatePokemon(25, "rain-dance");
            var defender = CreatePokemon(202, "tackle");

            await CreateEngine().TakeTurnAsync(
                attacker, defender, "rain-dance", attackerIsHero: true, _ => Task.CompletedTask);

            Assert.Equal(BattleWeather.Rain, BattleWeather.Current);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }
    }

    [Fact]
    public async Task Terrain_move_updates_field_and_grassy_field_heals_at_turn_end()
    {
        try
        {
            BattleWeather.Reset();
            BattleField.Reset();
            var attacker = CreatePokemon(25, "grassy-terrain");
            var defender = CreatePokemon(202, "tackle");
            int before = attacker.MaxHp / 2;
            attacker.CurrentHp = before;

            await CreateEngine().TakeTurnAsync(
                attacker, defender, "grassy-terrain", attackerIsHero: true, _ => Task.CompletedTask);
            Assert.Equal(BattleField.Grassy, BattleField.Current);

            await CreateEngine().ApplyEndOfTurnEffectsAsync(
                new[] { attacker }, _ => Task.CompletedTask);

            Assert.Equal(before + Math.Max(1, attacker.MaxHp / 16), attacker.CurrentHp);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }
    }

    [Fact]
    public async Task Move_created_weather_and_field_expire_after_five_turns()
    {
        try
        {
            BattleWeather.Set(BattleWeather.Rain, turns: 5);
            BattleField.Set(BattleField.Electric, turns: 5);
            var engine = CreateEngine();

            for (int turn = 0; turn < 4; turn++)
            {
                await engine.ApplyEndOfTurnEffectsAsync(
                    Array.Empty<Pokemon>(), _ => Task.CompletedTask);
                Assert.Equal(BattleWeather.Rain, BattleWeather.Current);
                Assert.Equal(BattleField.Electric, BattleField.Current);
            }

            var events = new List<BattleEvent>();
            await engine.ApplyEndOfTurnEffectsAsync(
                Array.Empty<Pokemon>(), battleEvent =>
                {
                    events.Add(battleEvent);
                    return Task.CompletedTask;
                });

            Assert.Equal(BattleWeather.Clear, BattleWeather.Current);
            Assert.Equal(BattleField.None, BattleField.Current);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("날씨의 효과가 사라졌다", StringComparison.Ordinal) == true);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("필드의 효과가 사라졌다", StringComparison.Ordinal) == true);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }
    }

    [Fact]
    public void Terrain_rules_apply_damage_boosts_and_defensive_reductions()
    {
        var attacker = CreatePokemon(25, "seed-bomb");
        var defender = CreatePokemon(202, "tackle");
        var grassMove = MoveDatabase.All["seed-bomb"];
        var groundMove = MoveDatabase.All["earthquake"];

        try
        {
            BattleField.Current = BattleField.Grassy;
            var grassContext = new BattlePowerContext(
                attacker, defender, grassMove, PokemonType.Grass, false, grassMove.Power, "seed-bomb");
            new DamageModifierEffectHandler().ModifyPower(grassContext);
            Assert.Equal(grassMove.Power * 1.3, grassContext.Power, 2);

            var groundContext = new BattlePowerContext(
                attacker, defender, groundMove, PokemonType.Ground, false, groundMove.Power, "earthquake");
            new DamageModifierEffectHandler().ModifyPower(groundContext);
            Assert.Equal(groundMove.Power * 0.5, groundContext.Power, 2);
        }
        finally
        {
            BattleField.Reset();
        }
    }

    [Fact]
    public void Electric_and_misty_fields_block_status_conditions()
    {
        var pokemon = CreatePokemon(25, "tackle");

        try
        {
            BattleField.Current = BattleField.Electric;
            Assert.True(pokemon.IsImmuneToAilment("sleep"));

            BattleField.Current = BattleField.Misty;
            Assert.True(pokemon.IsImmuneToAilment("burn"));
            Assert.True(pokemon.IsImmuneToConfusion());
        }
        finally
        {
            BattleField.Reset();
        }
    }

    [Fact]
    public void Grassy_field_activates_fur_coat_like_defense_bonus()
    {
        var pokemon = CreatePokemon(25, "tackle", ability: "풀모피");

        try
        {
            BattleField.Current = BattleField.Grassy;
            int grassyDefense = pokemon.EffectiveDef;
            BattleField.Current = BattleField.None;
            Assert.Equal(grassyDefense / 2, pokemon.EffectiveDef);
        }
        finally
        {
            BattleField.Reset();
        }
    }

    [Fact]
    public async Task Psychic_field_blocks_priority_moves()
    {
        try
        {
            BattleField.Current = BattleField.Psychic;
            var attacker = CreatePokemon(25, "shadow-sneak");
            var defender = CreatePokemon(202, "tackle");
            int before = defender.CurrentHp;
            var events = new List<BattleEvent>();

            await CreateEngine().TakeTurnAsync(
                attacker, defender, "shadow-sneak", attackerIsHero: true,
                battleEvent =>
                {
                    events.Add(battleEvent);
                    return Task.CompletedTask;
                });

            Assert.Equal(before, defender.CurrentHp);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("우선도 기술을 막았다", StringComparison.Ordinal) == true);
        }
        finally
        {
            BattleField.Reset();
        }
    }

    private static Pokemon CreatePokemon(int pokemonId, string move, string ability = "")
    {
        return new Pokemon(
            PokemonDatabase.All[pokemonId],
            new List<string> { move },
            ability,
            "없음",
            level: 50);
    }

    private static BattleEngine CreateEngine() => new(
        new Random(1234),
        new IBattleEffectHandler[]
        {
            new MoveEffectHandler(),
            new AbilityLifecycleEffectHandler(),
            new DamageModifierEffectHandler()
        });
}