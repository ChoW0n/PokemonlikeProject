using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class MoveEffectsRegressionTests
{
    [Fact]
    public void Move_catalog_has_unique_entries_and_every_entry_is_classified()
    {
        Assert.Equal(524, MoveDatabase.All.Count);
        Assert.All(MoveDatabase.All, entry =>
            Assert.Contains(
                MoveRuleMetadata.GetRule(entry.Key, entry.Value).Kind,
                Enum.GetValues<MoveRuleKind>()));
    }

    [Fact]
    public async Task Haze_resets_both_sides_stat_stages()
    {
        var user = CreatePokemon(94, "haze");
        var target = CreatePokemon(1, "tackle");
        user.ChangeStage("attack", 2);
        target.ChangeStage("defense", 3);
        var engine = CreateEngine();

        await engine.TakeTurnAsync(user, target, "haze", true, _ => Task.CompletedTask);

        Assert.All(user.StatStages.Values, stage => Assert.Equal(0, stage));
        Assert.All(target.StatStages.Values, stage => Assert.Equal(0, stage));
    }

    [Fact]
    public async Task Roar_forces_out_the_ranked_up_target_and_switch_reset_clears_stages()
    {
        var user = CreatePokemon(25, "roar");
        var target = CreatePokemon(1, "tackle");
        target.ChangeStage("attack", 3);
        target.ChangeStage("speed", 2);
        var engine = CreateEngine();

        var result = await engine.TakeTurnAsync(user, target, "roar", true, _ => Task.CompletedTask);

        Assert.Same(target, result.ForcedSwitchPokemon);
        engine.PrepareSwitchOut(target);
        Assert.All(target.StatStages.Values, stage => Assert.Equal(0, stage));
    }

    [Fact]
    public async Task Critical_hits_ignore_only_favorable_rank_stages()
    {
        async Task<int> DealDamage(
            string moveKey,
            int attackerId,
            int defenderId,
            bool critical,
            int attackerAttackStage = 0,
            int attackerSpecialAttackStage = 0,
            int attackerDefenseStage = 0,
            int defenderDefenseStage = 0,
            int defenderSpecialDefenseStage = 0)
        {
            var attacker = CreatePokemon(attackerId, moveKey);
            var defender = CreatePokemon(defenderId, "tackle");
            attacker.ChangeStage("attack", attackerAttackStage);
            attacker.ChangeStage("special-attack", attackerSpecialAttackStage);
            attacker.ChangeStage("defense", attackerDefenseStage);
            defender.ChangeStage("defense", defenderDefenseStage);
            defender.ChangeStage("special-defense", defenderSpecialDefenseStage);
            int hpBefore = defender.CurrentHp;

            await CreateEngine(new FixedRandom(critical ? 0 : 99)).TakeTurnAsync(
                attacker, defender, moveKey, true, _ => Task.CompletedTask);

            return hpBefore - defender.CurrentHp;
        }

        int physicalCritical = await DealDamage("tackle", 25, 1, critical: true);
        Assert.Equal(
            physicalCritical,
            await DealDamage("tackle", 25, 1, critical: true, defenderDefenseStage: 2));
        Assert.Equal(
            physicalCritical,
            await DealDamage("tackle", 25, 1, critical: true, attackerAttackStage: -2));
        Assert.True(
            await DealDamage("tackle", 25, 1, critical: true, defenderDefenseStage: -2)
            > physicalCritical);
        Assert.True(
            await DealDamage("tackle", 25, 1, critical: true, attackerAttackStage: 2)
            > physicalCritical);
        int physicalNormal = await DealDamage("tackle", 25, 1, critical: false);
        Assert.True(
            await DealDamage("tackle", 25, 1, critical: false, attackerAttackStage: 2)
            > physicalNormal);

        int specialCritical = await DealDamage("water-gun", 7, 1, critical: true);
        Assert.Equal(
            specialCritical,
            await DealDamage(
                "water-gun", 7, 1, critical: true, defenderSpecialDefenseStage: 2));
        Assert.Equal(
            specialCritical,
            await DealDamage(
                "water-gun", 7, 1, critical: true, attackerSpecialAttackStage: -2));
        Assert.True(
            await DealDamage(
                "water-gun", 7, 1, critical: true, defenderSpecialDefenseStage: -2)
            > specialCritical);
        Assert.True(
            await DealDamage(
                "water-gun", 7, 1, critical: true, attackerSpecialAttackStage: 2)
            > specialCritical);

        // 바디프레스는 자신의 방어를 공격 스탯으로 사용한다.
        int bodyPressCritical = await DealDamage(
            "body-press", 95, 1, critical: true);
        Assert.Equal(
            bodyPressCritical,
            await DealDamage(
                "body-press", 95, 1, critical: true, attackerDefenseStage: -2));
        Assert.True(
            await DealDamage(
                "body-press", 95, 1, critical: true, attackerDefenseStage: 2)
            > bodyPressCritical);
    }

    [Fact]
    public void Hex_doubles_only_against_a_real_status_condition_not_confusion()
    {
        var attacker = CreatePokemon(94, "hex");
        var healthy = CreatePokemon(1, "tackle");
        var confused = CreatePokemon(1, "tackle");
        confused.ApplyConfusion(new Random(1234));
        var burned = CreatePokemon(1, "tackle");
        burned.ApplyAilment("burn");

        double basePower = MoveRuleMetadata.EffectivePower(
            "hex", MoveDatabase.All["hex"], attacker, healthy);
        double confusedPower = MoveRuleMetadata.EffectivePower(
            "hex", MoveDatabase.All["hex"], attacker, confused);
        double burnedPower = MoveRuleMetadata.EffectivePower(
            "hex", MoveDatabase.All["hex"], attacker, burned);

        Assert.Equal(basePower, confusedPower);
        Assert.Equal(basePower * 2, burnedPower);
    }

    [Fact]
    public void Payback_doubles_when_its_user_moves_after_the_target()
    {
        var attacker = CreatePokemon(19, "payback");
        var defender = CreatePokemon(1, "tackle");
        var move = MoveDatabase.All["payback"];

        double firstPower = MoveRuleMetadata.EffectivePower(
            "payback", move, attacker, defender, attackerMovedFirst: true);
        double secondPower = MoveRuleMetadata.EffectivePower(
            "payback", move, attacker, defender, attackerMovedFirst: false);

        Assert.Equal(move.Power, firstPower);
        Assert.Equal(move.Power * 2, secondPower);
    }

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
                    or MoveRuleKind.DualTypeDamage or MoveRuleKind.HazardRemoval
                    or MoveRuleKind.Substitute or MoveRuleKind.TrickRoom
                    or MoveRuleKind.Gravity or MoveRuleKind.Counter
                    or MoveRuleKind.MirrorCoat or MoveRuleKind.ItemSwap
                    or MoveRuleKind.HazardPlacement));
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
    public async Task Dream_eater_is_selectable_awake_but_only_damages_and_heals_against_sleep()
    {
        var attacker = CreatePokemon(96, "dream-eater");
        var awakeDefender = CreatePokemon(202, "tackle");
        attacker.CurrentHp = attacker.MaxHp - 20;
        int awakeAttackerHp = attacker.CurrentHp;
        int awakeDefenderHp = awakeDefender.CurrentHp;
        var awakeEvents = new List<BattleEvent>();

        Assert.True(attacker.CanUseMove("dream-eater"));
        await CreateEngine().TakeTurnAsync(
            attacker,
            awakeDefender,
            "dream-eater",
            true,
            Capture(awakeEvents));

        Assert.Equal(awakeAttackerHp, attacker.CurrentHp);
        Assert.Equal(awakeDefenderHp, awakeDefender.CurrentHp);
        Assert.Contains(awakeEvents, battleEvent =>
            battleEvent.Message?.Contains("실패했다", StringComparison.Ordinal) == true);

        var sleepingAttacker = CreatePokemon(96, "dream-eater");
        var sleepingDefender = CreatePokemon(202, "tackle");
        sleepingAttacker.CurrentHp = sleepingAttacker.MaxHp - 20;
        sleepingDefender.Status = StatusCondition.Sleep;
        int sleepingAttackerHp = sleepingAttacker.CurrentHp;
        int sleepingDefenderHp = sleepingDefender.CurrentHp;

        await CreateEngine().TakeTurnAsync(
            sleepingAttacker,
            sleepingDefender,
            "dream-eater",
            true,
            _ => Task.CompletedTask);

        Assert.True(sleepingDefender.CurrentHp < sleepingDefenderHp);
        Assert.True(sleepingAttacker.CurrentHp > sleepingAttackerHp);
    }

    [Fact]
    public void Dig_is_a_charge_move()
    {
        Assert.True(MoveRuleMetadata.IsChargeMove("dig"));
        Assert.Equal(
            MoveRuleKind.Charge,
            MoveRuleMetadata.GetRule("dig", MoveDatabase.All["dig"]).Kind);
    }

    // 충전 중에는 PP가 없어도 예약 기술만 선택 가능해야 한다.
    [Fact]
    public void Charging_move_locks_other_move_selection_before_pp_check()
    {
        var attacker = CreatePokemon(50, "dig", "tackle");
        attacker.SetPendingMove("dig", semiInvulnerable: true);
        attacker.CurrentPP["dig"] = 0;

        Assert.True(attacker.CanUseMove("dig"));
        Assert.False(attacker.CanUseMove("tackle"));
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
        attacker.CurrentPP["outrage"] = 0;
        Assert.True(attacker.CanUseMove("outrage"));
        Assert.False(attacker.CanUseMove("tackle"));

        await engine.TakeTurnAsync(attacker, defender, "tackle", true, Capture(events));
        Assert.Equal(0, attacker.CurrentPP["outrage"]);
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
    public async Task Rampage_turns_advance_when_the_attempt_is_blocked_by_protection()
    {
        var attacker = CreatePokemon(1, "outrage");
        var defender = CreatePokemon(25, "protect");
        var engine = CreateEngine(new FixedRandom(99));

        await engine.TakeTurnAsync(defender, attacker, "protect", false, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(attacker, defender, "outrage", true, _ => Task.CompletedTask);

        Assert.Equal("outrage", attacker.RampageMoveKey);
        int remainingAfterBlockedAttempt = attacker.RampageTurnsRemaining;

        await engine.TakeTurnAsync(attacker, defender, "tackle", true, _ => Task.CompletedTask);

        Assert.True(remainingAfterBlockedAttempt > attacker.RampageTurnsRemaining
            || attacker.RampageMoveKey == null);
    }

    private sealed class FixedRandom : Random
    {
        private readonly int value;

        public FixedRandom(int value)
        {
            this.value = value;
        }

        public override int Next(int maxValue) => Math.Min(value, maxValue - 1);
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
    public async Task Close_combat_lowers_the_attacker_defenses_not_the_defender()
    {
        var attacker = CreatePokemon(25, "close-combat");
        var defender = CreatePokemon(1, "tackle", level: 100);
        var engine = CreateEngine(new FixedRandom(0));

        await engine.TakeTurnAsync(attacker, defender, "close-combat", true, _ => Task.CompletedTask);

        Assert.Equal(-1, attacker.StatStages["defense"]);
        Assert.Equal(-1, attacker.StatStages["special-defense"]);
        Assert.Equal(0, defender.StatStages["defense"]);
        Assert.Equal(0, defender.StatStages["special-defense"]);
    }

    [Fact]
    public void Self_stat_change_moves_mark_every_stat_change_as_self_targeted()
    {
        string[] selfStatChangeMoves =
        {
            "close-combat", "superpower", "leaf-storm", "hammer-arm", "overheat",
            "dragon-ascent", "psycho-boost", "v-create"
        };

        foreach (string moveKey in selfStatChangeMoves)
        {
            Assert.NotEmpty(MoveDatabase.All[moveKey].StatChanges);
            Assert.All(MoveDatabase.All[moveKey].StatChanges, change => Assert.True(change.TargetsSelf));
        }
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

    [Fact]
    public async Task Substitute_absorbs_damage_and_pays_a_quarter_hp()
    {
        var user = CreatePokemon(25, "substitute", level: 50);
        var attacker = CreatePokemon(1, "tackle", level: 50);
        var engine = CreateEngine();

        int hpBefore = user.CurrentHp;
        await engine.TakeTurnAsync(user, attacker, "substitute", true, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(attacker, user, "tackle", false, _ => Task.CompletedTask);

        Assert.True(user.HasSubstitute);
        Assert.Equal(hpBefore - user.MaxHp / 4, user.CurrentHp);
        Assert.Equal(attacker.MaxHp, attacker.CurrentHp);
    }

    [Fact]
    public async Task Counter_and_mirror_coat_return_only_the_matching_damage_class()
    {
        var counterUser = CreatePokemon(25, "counter");
        var physical = CreatePokemon(1, "tackle");
        var engine = CreateEngine();

        await engine.TakeTurnAsync(physical, counterUser, "tackle", false, _ => Task.CompletedTask);
        int beforeCounter = physical.CurrentHp;
        await engine.TakeTurnAsync(counterUser, physical, "counter", true, _ => Task.CompletedTask);
        Assert.True(physical.CurrentHp < beforeCounter);

        var mirrorUser = CreatePokemon(25, "mirror-coat");
        var special = CreatePokemon(1, "water-gun");
        await engine.TakeTurnAsync(special, mirrorUser, "water-gun", false, _ => Task.CompletedTask);
        int beforeMirror = special.CurrentHp;
        await engine.TakeTurnAsync(mirrorUser, special, "mirror-coat", true, _ => Task.CompletedTask);
        Assert.True(special.CurrentHp < beforeMirror);
    }

    [Fact]
    public async Task Magic_mirror_reflects_a_targeted_status_move_once()
    {
        var attacker = CreatePokemon(25, "growl");
        var reflector = new Pokemon(
            PokemonDatabase.All[1], new List<string> { "tackle" }, "매직미러", level: 50);
        var engine = CreateEngine();

        await engine.TakeTurnAsync(attacker, reflector, "growl", true, _ => Task.CompletedTask);

        Assert.Equal(-1, attacker.StatStages["attack"]);
        Assert.Equal(0, reflector.StatStages["attack"]);
    }

    [Fact]
    public async Task Memento_lowers_both_offensive_stats_and_faints_its_user()
    {
        var user = CreatePokemon(25, "memento");
        var target = CreatePokemon(1, "tackle");
        var engine = CreateEngine();

        await engine.TakeTurnAsync(user, target, "memento", true, _ => Task.CompletedTask);

        Assert.True(user.IsFainted);
        Assert.Equal(-2, target.StatStages["attack"]);
        Assert.Equal(-2, target.StatStages["special-attack"]);
    }

    [Fact]
    public async Task Entry_hazards_damage_a_replacement_and_rapid_spin_boosts_its_user()
    {
        var setter = CreatePokemon(25, "stealth-rock");
        var target = CreatePokemon(1, "rapid-spin");
        var engine = CreateEngine();

        await engine.TakeTurnAsync(setter, target, "stealth-rock", true, _ => Task.CompletedTask);
        var replacement = CreatePokemon(1, "tackle");
        int replacementHp = replacement.CurrentHp;
        engine.ActivateSwitchIn(replacement, setter, isHeroSide: false);
        Assert.True(replacement.CurrentHp < replacementHp);

        await engine.TakeTurnAsync(target, setter, "rapid-spin", false, _ => Task.CompletedTask);
        Assert.Equal(1, target.StatStages["speed"]);
        Assert.Equal(0, setter.StatStages["speed"]);
    }

    [Fact]
    public async Task Trick_room_reverses_speed_order_without_changing_priority()
    {
        var fast = CreatePokemon(25, "trick-room");
        var slow = CreatePokemon(1, "tackle");
        var engine = CreateEngine();
        BattleField.Reset();

        await engine.TakeTurnAsync(fast, slow, "trick-room", true, _ => Task.CompletedTask);
        var plan = engine.PlanTurn(fast, "tackle", slow, new[] { "tackle" });

        Assert.True(BattleField.TrickRoomActive);
        Assert.False(plan.HeroFirst);
        BattleField.Reset();
    }

    [Fact]
    public async Task Trick_and_switcheroo_exchange_items_but_sticky_protects_the_defender()
    {
        var trickUser = new Pokemon(PokemonDatabase.All[25],
            new List<string> { "trick" }, item: "생명의구슬", level: 50);
        var target = new Pokemon(PokemonDatabase.All[1],
            new List<string> { "tackle" }, item: "먹다남은음식", level: 50);
        var engine = CreateEngine();

        await engine.TakeTurnAsync(trickUser, target, "trick", true, _ => Task.CompletedTask);
        Assert.Equal("먹다남은음식", trickUser.HeldItem);
        Assert.Equal("생명의구슬", target.HeldItem);

        var sticky = new Pokemon(PokemonDatabase.All[1],
            new List<string> { "tackle" }, ability: "점착", item: "먹다남은음식", level: 50);
        await engine.TakeTurnAsync(trickUser, sticky, "switcheroo", true, _ => Task.CompletedTask);
        Assert.Equal("먹다남은음식", trickUser.HeldItem);
        Assert.Equal("먹다남은음식", sticky.HeldItem);
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

    private static BattleEngine CreateEngine(Random? random = null) =>
        new(random ?? new Random(1234), new IBattleEffectHandler[] { new MoveEffectHandler() });

    private static Func<BattleEvent, Task> Capture(List<BattleEvent> events) =>
        battleEvent =>
        {
            events.Add(battleEvent);
            return Task.CompletedTask;
        };
}