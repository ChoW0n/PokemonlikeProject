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
    public async Task Weather_nullification_matches_air_lock_for_all_weather_effect_paths()
    {
        var cloudNine = CreatePokemon(55, "tackle", ability: "날씨부정");
        var airLock = CreatePokemon(384, "tackle", ability: "에어록");
        var normal = CreatePokemon(132, "tackle");

        try
        {
            Assert.True(BattleWeather.AreEffectsSuppressed(cloudNine, normal));
            Assert.True(BattleWeather.AreEffectsSuppressed(airLock, normal));

            BattleWeather.Set(BattleWeather.Rain);
            var weatherBall = MoveDatabase.All["weather-ball"];
            Assert.Equal(
                PokemonType.Normal,
                MoveRuleMetadata.ResolveMoveType("weather-ball", weatherBall, cloudNine, normal));
            Assert.Equal(
                weatherBall.Power,
                MoveRuleMetadata.EffectivePower("weather-ball", weatherBall, cloudNine, normal));
            Assert.Equal(
                weatherBall.Accuracy,
                MoveRuleMetadata.EffectiveAccuracy(
                    "weather-ball", weatherBall, cloudNine, normal));

            var swiftSwimmer = CreatePokemon(25, "tackle", ability: "쓱쓱");
            Assert.Equal(swiftSwimmer.Spd, swiftSwimmer.EffectiveSpdAgainst(cloudNine));

            var rainDish = CreatePokemon(270, "tackle", ability: "젖은접시");
            rainDish.CurrentHp = rainDish.MaxHp / 2;
            int beforeHealing = rainDish.CurrentHp;
            await CreateEngine().ApplyEndOfTurnEffectsAsync(
                new[] { rainDish, cloudNine }, _ => Task.CompletedTask);
            Assert.Equal(beforeHealing, rainDish.CurrentHp);

            BattleWeather.Set(BattleWeather.Sun);
            var synthesis = MoveDatabase.All["synthesis"];
            Assert.Equal(
                50,
                MoveRuleMetadata.RecoveryAmount(
                    "synthesis", synthesis, 100, cloudNine, normal));

            var sunPower = CreatePokemon(670, "tackle", ability: "선파워");
            sunPower.CurrentHp = sunPower.MaxHp;
            int beforeSunDamage = sunPower.CurrentHp;
            await CreateEngine().ApplyEndOfTurnEffectsAsync(
                new[] { sunPower, cloudNine }, _ => Task.CompletedTask);
            Assert.Equal(beforeSunDamage, sunPower.CurrentHp);

            BattleWeather.Set(BattleWeather.Sand);
            var sandTarget = CreatePokemon(132, "tackle");
            int beforeSandDamage = sandTarget.CurrentHp;
            await CreateEngine().ApplyEndOfTurnEffectsAsync(
                new[] { sandTarget, cloudNine }, _ => Task.CompletedTask);
            Assert.Equal(beforeSandDamage, sandTarget.CurrentHp);

            var sandForce = CreatePokemon(25, "earthquake", ability: "모래의힘");
            var sandForceContext = new BattlePowerContext(
                sandForce, cloudNine, MoveDatabase.All["earthquake"],
                PokemonType.Ground, false, MoveDatabase.All["earthquake"].Power, "earthquake");
            new DamageModifierEffectHandler().ModifyPower(sandForceContext);
            Assert.Equal(MoveDatabase.All["earthquake"].Power, sandForceContext.Power);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }
    }

    [Fact]
    public void Weather_nullification_does_not_disable_terrain_effects()
    {
        var cloudNine = CreatePokemon(55, "tackle", ability: "날씨부정");
        var attacker = CreatePokemon(25, "seed-bomb");
        var move = MoveDatabase.All["seed-bomb"];

        try
        {
            BattleWeather.Set(BattleWeather.Sun);
            BattleField.Current = BattleField.Grassy;
            var context = new BattlePowerContext(
                attacker, cloudNine, move, PokemonType.Grass, false, move.Power, "seed-bomb");

            new DamageModifierEffectHandler().ModifyPower(context);

            Assert.Equal(move.Power * 1.3, context.Power, 2);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }
    }

    [Fact]
    public void Aura_abilities_boost_both_sides_and_aura_break_reverses_them()
    {
        var fairyMove = MoveDatabase.All["moonblast"];
        var darkMove = MoveDatabase.All["dark-pulse"];
        var fairyAura = CreatePokemon(716, "moonblast", ability: "페어리오라");
        var darkAura = CreatePokemon(717, "dark-pulse", ability: "다크오라");
        var auraBreak = CreatePokemon(718, "tackle", ability: "오라브레이크");
        var normal = CreatePokemon(132, "tackle");
        var handler = new DamageModifierEffectHandler();

        try
        {
            var fairyContext = new BattlePowerContext(
                normal, fairyAura, fairyMove, PokemonType.Fairy, false, fairyMove.Power, "moonblast");
            handler.ModifyPower(fairyContext);
            Assert.Equal(fairyMove.Power * (4.0 / 3.0), fairyContext.Power, 2);

            var darkContext = new BattlePowerContext(
                darkAura, normal, darkMove, PokemonType.Dark, false, darkMove.Power, "dark-pulse");
            handler.ModifyPower(darkContext);
            Assert.Equal(darkMove.Power * (4.0 / 3.0), darkContext.Power, 2);

            var reversedFairyContext = new BattlePowerContext(
                fairyAura, auraBreak, fairyMove, PokemonType.Fairy, false, fairyMove.Power, "moonblast");
            handler.ModifyPower(reversedFairyContext);
            Assert.Equal(fairyMove.Power * 0.75, reversedFairyContext.Power, 2);

            var reversedDarkContext = new BattlePowerContext(
                darkAura, auraBreak, darkMove, PokemonType.Dark, false, darkMove.Power, "dark-pulse");
            handler.ModifyPower(reversedDarkContext);
            Assert.Equal(darkMove.Power * 0.75, reversedDarkContext.Power, 2);
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
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
    public async Task Battle_environment_survives_independent_async_calls()
    {
        try
        {
            var engine = CreateEngine();
            var weatherAttacker = CreatePokemon(25, "rain-dance");
            var fieldAttacker = CreatePokemon(25, "electric-terrain");
            var defender = CreatePokemon(202, "tackle");

            await engine.TakeTurnAsync(
                weatherAttacker,
                defender,
                "rain-dance",
                attackerIsHero: true,
                _ => Task.CompletedTask);
            await engine.TakeTurnAsync(
                fieldAttacker,
                defender,
                "electric-terrain",
                attackerIsHero: true,
                _ => Task.CompletedTask);

            await Task.Run(() => engine.ApplyEndOfTurnEffectsAsync(
                Array.Empty<Pokemon>(),
                _ => Task.CompletedTask));

            Assert.Equal(BattleWeather.Rain, engine.CurrentWeather);
            Assert.Equal(BattleField.Electric, engine.CurrentField);
            Assert.Equal(4, BattleWeather.TurnsRemaining);
            Assert.Equal(4, BattleField.TurnsRemaining);
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