using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class MoveEffectsRegressionTests
{
    [Fact]
    public void Every_catalog_move_has_an_explicit_runtime_rule()
    {
        Assert.True(MoveDatabase.All.Count >= 490);
        Assert.DoesNotContain(MoveDatabase.All, entry =>
            MoveRuleMetadata.GetRule(entry.Key, entry.Value).Kind
                is not (MoveRuleKind.StandardDamage or MoveRuleKind.Status
                    or MoveRuleKind.Protect or MoveRuleKind.Charge
                    or MoveRuleKind.DelayedDamage or MoveRuleKind.Recharge
                    or MoveRuleKind.Binding or MoveRuleKind.LeechSeed
                    or MoveRuleKind.Yawn or MoveRuleKind.PerishSong
                    or MoveRuleKind.Disable or MoveRuleKind.MoveRestriction
                    or MoveRuleKind.ForcedSwitch or MoveRuleKind.SelfDestruct
                    or MoveRuleKind.Rampage
                    or MoveRuleKind.VariablePower or MoveRuleKind.VariableType
                    or MoveRuleKind.SpecialDefenseCalculation
                    or MoveRuleKind.DualTypeDamage or MoveRuleKind.HazardRemoval));
    }

    [Fact]
    public async Task Charge_and_delayed_moves_resolve_on_their_following_turns()
    {
        var attacker = CreatePokemon(25, "solar-beam", "thunderbolt");
        var defender = CreatePokemon(1, "tackle");
        var engine = CreateEngine();

        await engine.TakeTurnAsync(attacker, defender, "solar-beam", true, _ => Task.CompletedTask);
        int hpAfterCharge = defender.CurrentHp;
        Assert.Equal(hpAfterCharge, defender.CurrentHp);

        await engine.ApplyEndOfTurnEffectsAsync(new[] { attacker, defender }, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(attacker, defender, null, true, _ => Task.CompletedTask);
        await engine.ApplyEndOfTurnEffectsAsync(new[] { attacker, defender }, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(attacker, defender, null, true, _ => Task.CompletedTask);

        Assert.True(defender.CurrentHp < hpAfterCharge);
    }

    [Fact]
    public async Task Persistent_move_effects_apply_damage_and_recovery_at_turn_end()
    {
        var attacker = CreatePokemon(1, "leech-seed");
        var defender = CreatePokemon(25, "tackle");
        var engine = CreateEngine();
        defender.CurrentHp = defender.MaxHp - 20;

        await engine.TakeTurnAsync(attacker, defender, "leech-seed", true, _ => Task.CompletedTask);
        Assert.True(defender.LeechSeeded);
        int defenderBeforeEnd = defender.CurrentHp;
        await engine.ApplyEndOfTurnEffectsAsync(new[] { attacker, defender }, _ => Task.CompletedTask);

        Assert.True(defender.CurrentHp < defenderBeforeEnd);
        Assert.True(attacker.CurrentHp > attacker.MaxHp - 1);
    }

    [Fact]
    public async Task Rampage_locks_the_move_without_extra_pp_and_confuses_when_it_ends()
    {
        var attacker = CreatePokemon(1, "outrage", level: 1);
        var defender = CreatePokemon(202, "tackle", level: 100);
        int ppBefore = attacker.CurrentPP["outrage"];
        var events = new List<BattleEvent>();
        var engine = CreateEngine();

        await engine.TakeTurnAsync(attacker, defender, "outrage", true, Capture(events));
        Assert.Equal(ppBefore - 1, attacker.CurrentPP["outrage"]);
        Assert.Equal("outrage", attacker.RampageMoveKey);

        await engine.TakeTurnAsync(attacker, defender, "tackle", true, Capture(events));
        Assert.Equal(ppBefore - 1, attacker.CurrentPP["outrage"]);
        Assert.DoesNotContain(events, battleEvent =>
            battleEvent.Message?.Contains("그 기술을 사용할 수 없다", StringComparison.Ordinal) == true);

        while (attacker.RampageMoveKey != null)
        {
            await engine.TakeTurnAsync(attacker, defender, "tackle", true, Capture(events));
        }

        Assert.True(attacker.IsConfused);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("난동이 끝나 혼란", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Self_targeted_stat_effects_do_not_lower_the_opponent()
    {
        var attacker = CreatePokemon(4, "fiery-dance");
        var defender = CreatePokemon(1, "tackle");
        var engine = CreateEngine();
        int defenderSpecialAttack = defender.StatStages["special-attack"];

        for (var i = 0; i < 5 && attacker.StatStages["special-attack"] == 0; i++)
        {
            await engine.TakeTurnAsync(attacker, defender, "fiery-dance", true, _ => Task.CompletedTask);
            if (!attacker.IsFainted && !defender.IsFainted)
                await engine.ApplyEndOfTurnEffectsAsync(new[] { attacker, defender }, _ => Task.CompletedTask);
        }

        Assert.Equal(defenderSpecialAttack, defender.StatStages["special-attack"]);
    }

    [Fact]
    public async Task Switching_moves_report_the_correct_side_to_switch()
    {
        var attacker = CreatePokemon(25, "u-turn");
        var defender = CreatePokemon(1, "dragon-tail");
        var engine = CreateEngine();

        var attackerResult = await engine.TakeTurnAsync(
            attacker, defender, "u-turn", true, _ => Task.CompletedTask);
        Assert.Same(attacker, attackerResult.ForcedSwitchPokemon);

        var defenderResult = await engine.TakeTurnAsync(
            defender, attacker, "dragon-tail", false, _ => Task.CompletedTask);
        Assert.Same(attacker, defenderResult.ForcedSwitchPokemon);
    }

    private static Pokemon CreatePokemon(
        int id,
        string move,
        string? secondMove = null,
        int level = 50)
    {
        var moves = secondMove == null ? new[] { move } : new[] { move, secondMove };
        return new Pokemon(PokemonDatabase.All[id], moves.ToList(), "", "없음", level);
    }

    private static BattleEngine CreateEngine() =>
        new(new Random(1234), new IBattleEffectHandler[] { new MoveEffectHandler() });

    private static Func<BattleEvent, Task> Capture(List<BattleEvent> events) =>
        battleEvent =>
        {
            events.Add(battleEvent);
            return Task.CompletedTask;
        };
}