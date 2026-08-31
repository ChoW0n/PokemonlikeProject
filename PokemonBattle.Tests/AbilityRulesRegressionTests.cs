using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class AbilityRulesRegressionTests
{
    [Fact]
    public async Task Critical_hit_rules_apply_sniper_anger_point_and_critical_immunity()
    {
        const string moveKey = "frost-breath";
        var sniper = CreatePokemon(132, moveKey, ability: "스나이퍼");
        var target = CreatePokemon(202, "tackle");
        int hpBefore = target.CurrentHp;
        int scaledPower = (int)(MoveDatabase.All[moveKey].Power
            * ((double)sniper.EffectiveSpAtk / target.EffectiveSpDef));
        int expectedSniperDamage = (int)((int)(scaledPower * 1.5) * 1.5);
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            sniper, target, moveKey, true, Capture(events));

        Assert.Equal(expectedSniperDamage, hpBefore - target.CurrentHp);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("급소에 맞았다", StringComparison.Ordinal) == true);
        Assert.True(MoveRuleMetadata.HasHighCriticalRate("cross-poison"));

        var normalAttacker = CreatePokemon(132, moveKey);
        var armoredTarget = CreatePokemon(202, "tackle", ability: "조가비갑옷");
        hpBefore = armoredTarget.CurrentHp;
        int expectedNormalDamage = (int)(MoveDatabase.All[moveKey].Power
            * ((double)normalAttacker.EffectiveSpAtk / armoredTarget.EffectiveSpDef));
        events.Clear();

        await CreateFullEngine().TakeTurnAsync(
            normalAttacker, armoredTarget, moveKey, true, Capture(events));

        Assert.Equal(expectedNormalDamage, hpBefore - armoredTarget.CurrentHp);
        Assert.DoesNotContain(events, battleEvent =>
            battleEvent.Message?.Contains("급소에 맞았다", StringComparison.Ordinal) == true);

        var angerPointTarget = CreatePokemon(202, "tackle", ability: "분노의경혈");
        events.Clear();

        await CreateFullEngine().TakeTurnAsync(
            CreatePokemon(132, moveKey), angerPointTarget, moveKey, true, Capture(events));

        Assert.Equal(6, angerPointTarget.StatStages["attack"]);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("분노의경혈", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Berry_rules_apply_gluttony_cheek_pouch_and_belch_gate()
    {
        BattleWeather.Current = "맑음";
        var glutton = CreatePokemon(132, "belch", ability: "먹보", heldItem: "무화열매");
        glutton.CurrentHp = glutton.MaxHp / 2;
        Assert.False(glutton.CanUseMove("belch"));

        var events = new List<BattleEvent>();
        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(new[] { glutton }, Capture(events));

        Assert.Equal("없음", glutton.HeldItem);
        Assert.True(glutton.HasConsumedBerry);
        Assert.True(glutton.CanUseMove("belch"));
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("무화열매", StringComparison.Ordinal) == true);

        var normalEater = CreatePokemon(132, "tackle", heldItem: "무화열매");
        normalEater.CurrentHp = normalEater.MaxHp / 2;
        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(new[] { normalEater }, _ => Task.CompletedTask);
        Assert.Equal("무화열매", normalEater.HeldItem);

        var cheekPouch = CreatePokemon(132, "tackle", ability: "볼주머니", heldItem: "오랭열매");
        cheekPouch.CurrentHp = cheekPouch.MaxHp / 2;
        int hpBefore = cheekPouch.CurrentHp;
        events.Clear();

        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(new[] { cheekPouch }, Capture(events));

        Assert.Equal(
            hpBefore + 10 + Math.Max(1, cheekPouch.MaxHp / 8),
            cheekPouch.CurrentHp);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("볼주머니", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Berry_moves_consume_or_destroy_the_defenders_berry()
    {
        var eater = CreatePokemon(132, "bug-bite");
        eater.CurrentHp = eater.MaxHp / 2;
        var berryHolder = CreatePokemon(202, "tackle", heldItem: "자뭉열매");
        berryHolder.CurrentHp = berryHolder.MaxHp / 2 + 1;
        var events = new List<BattleEvent>();
        int eaterHpBefore = eater.CurrentHp;

        await CreateFullEngine().TakeTurnAsync(
            eater, berryHolder, "bug-bite", true, Capture(events));

        Assert.Equal("없음", berryHolder.HeldItem);
        Assert.True(eater.HasConsumedBerry);
        Assert.Equal(
            Math.Min(eater.MaxHp, eaterHpBefore + Math.Max(1, eater.MaxHp / 4)),
            eater.CurrentHp);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("빼앗아 먹었다", StringComparison.Ordinal) == true);

        var incinerator = CreatePokemon(4, "incinerate");
        berryHolder = CreatePokemon(132, "tackle", heldItem: "리샘열매");
        events.Clear();

        await CreateFullEngine().TakeTurnAsync(
            incinerator, berryHolder, "incinerate", true, Capture(events));

        Assert.Equal("없음", berryHolder.HeldItem);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("불태워졌다", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Berry_activates_immediately_after_damage_and_status()
    {
        var healingBerryHolder = CreatePokemon(132, "tackle", heldItem: "자뭉열매");
        healingBerryHolder.CurrentHp = healingBerryHolder.MaxHp / 2 + 1;
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            CreatePokemon(132, "tackle"),
            healingBerryHolder,
            "tackle",
            true,
            Capture(events));

        Assert.Equal("없음", healingBerryHolder.HeldItem);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("자뭉열매", StringComparison.Ordinal) == true);

        const string statusMoveKey = "regression-guaranteed-poison";
        MoveDatabase.All[statusMoveKey] = new Move(
            "회귀 독 기술", 0, PokemonType.Poison, 10, 100, true, 0,
            true, false, "poison", 100, 0, new List<StatChangeEntry>(), 0,
            "반드시 독 상태로 만든다.", 0, 0, 1, 1);
        try
        {
            var lumHolder = CreatePokemon(132, "tackle", heldItem: "리샘열매");
            events.Clear();

            await CreateFullEngine().TakeTurnAsync(
                CreatePokemon(132, statusMoveKey),
                lumHolder,
                statusMoveKey,
                true,
                Capture(events));

            Assert.Equal(StatusCondition.None, lumHolder.Status);
            Assert.Equal("없음", lumHolder.HeldItem);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("리샘열매", StringComparison.Ordinal) == true);
        }
        finally
        {
            MoveDatabase.All.Remove(statusMoveKey);
        }
    }

    [Fact]
    public async Task Harvest_restores_the_consumed_berry_once_at_turn_end()
    {
        BattleWeather.Reset();
        BattleField.Reset();
        var harvester = CreatePokemon(132, "tackle", ability: "수확", heldItem: "자뭉열매");
        harvester.CurrentHp = harvester.MaxHp / 2;
        var events = new List<BattleEvent>();

        await CreateFullEngine(new FixedRandom(0)).ApplyEndOfTurnEffectsAsync(
            new[] { harvester, CreatePokemon(202, "tackle") },
            Capture(events));

        Assert.Equal("자뭉열매", harvester.HeldItem);
        Assert.True(harvester.HasConsumedBerry);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("수확", StringComparison.Ordinal) == true);

        var failedHarvest = CreatePokemon(132, "tackle", ability: "수확", heldItem: "오랭열매");
        failedHarvest.CurrentHp = failedHarvest.MaxHp / 2;
        events.Clear();
        await CreateFullEngine(new FixedRandom(99)).ApplyEndOfTurnEffectsAsync(
            new[] { failedHarvest },
            Capture(events));

        Assert.Equal("없음", failedHarvest.HeldItem);
        Assert.DoesNotContain(events, battleEvent =>
            battleEvent.Message?.Contains("수확", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Unnerve_blocks_berry_consumption_until_the_opponent_leaves()
    {
        var eater = CreatePokemon(132, "tackle", heldItem: "자뭉열매");
        eater.CurrentHp = eater.MaxHp / 2;
        var unnerve = CreatePokemon(667, "tackle", ability: "긴장감");
        var events = new List<BattleEvent>();

        await CreateFullEngine(new FixedRandom(0)).ApplyEndOfTurnEffectsAsync(
            new[] { eater, unnerve },
            Capture(events));

        Assert.Equal("자뭉열매", eater.HeldItem);
        Assert.False(eater.HasConsumedBerry);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("긴장감", StringComparison.Ordinal) == true);

        unnerve.MarkFainted();
        await CreateFullEngine(new FixedRandom(0)).ApplyEndOfTurnEffectsAsync(
            new[] { eater, unnerve },
            Capture(events));

        Assert.Equal("없음", eater.HeldItem);
        Assert.True(eater.HasConsumedBerry);
    }

    [Fact]
    public async Task Unnerve_blocks_bug_bite_from_eating_the_defender_berry()
    {
        var attacker = CreatePokemon(132, "bug-bite");
        var defender = CreatePokemon(667, "tackle", ability: "긴장감", heldItem: "자뭉열매");
        var events = new List<BattleEvent>();

        await CreateFullEngine(new FixedRandom(0)).TakeTurnAsync(
            attacker, defender, "bug-bite", true, Capture(events));

        Assert.Equal("자뭉열매", defender.HeldItem);
        Assert.False(attacker.HasConsumedBerry);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("긴장감", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Pickup_resolves_once_after_battle_and_reports_the_item()
    {
        var picker = CreatePokemon(660, "tackle", ability: "픽업");
        var events = new List<BattleEvent>();
        var engine = CreateFullEngine(new FixedRandom(0));

        await engine.ApplyEndOfBattleEffectsAsync(new[] { picker }, Capture(events));

        Assert.Equal("먹다남은음식", picker.HeldItem);
        Assert.True(picker.HasPickedUpItem);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("픽업", StringComparison.Ordinal) == true);

        events.Clear();
        await engine.ApplyEndOfBattleEffectsAsync(new[] { picker }, Capture(events));
        Assert.Empty(events);
        Assert.Equal("먹다남은음식", picker.HeldItem);

        var failedPicker = CreatePokemon(660, "tackle", ability: "픽업");
        await CreateFullEngine(new FixedRandom(99)).ApplyEndOfBattleEffectsAsync(
            new[] { failedPicker },
            _ => Task.CompletedTask);
        Assert.Equal("없음", failedPicker.HeldItem);
        Assert.True(failedPicker.HasPickedUpItem);
    }

    [Fact]
    public void Trapping_abilities_block_only_eligible_switches()
    {
        var engine = CreateFullEngine();
        var grounded = CreatePokemon(132, "tackle");
        var flying = CreatePokemon(6, "tackle");
        var levitating = CreatePokemon(92, "tackle", ability: "부유");
        var arenaTrap = CreatePokemon(132, "tackle", ability: "개미지옥");

        Assert.False(engine.CanSwitch(grounded, arenaTrap));
        Assert.True(engine.CanSwitch(flying, arenaTrap));
        Assert.True(engine.CanSwitch(levitating, arenaTrap));
        Assert.True(engine.CanSwitch(CreatePokemon(92, "tackle"), arenaTrap));

        var shadowTag = CreatePokemon(202, "tackle", ability: "그림자밟기");
        Assert.False(engine.CanSwitch(grounded, shadowTag));
        Assert.True(engine.CanSwitch(CreatePokemon(92, "tackle"), shadowTag));
        Assert.True(engine.CanSwitch(
            CreatePokemon(202, "tackle", ability: "그림자밟기"),
            shadowTag));
    }

    [Fact]
    public void Run_away_allows_wild_escape_and_magnet_pull_only_traps_steel()
    {
        var engine = CreateFullEngine();
        var wildOpponent = CreatePokemon(132, "tackle");
        var runner = CreatePokemon(132, "tackle", ability: "도주");

        Assert.True(engine.CanEscape(runner, wildOpponent));
        Assert.True(engine.CanEscape(CreatePokemon(132, "tackle"), wildOpponent));
        Assert.False(engine.CanEscape(runner, wildOpponent, isWildBattle: false));

        var magnetPull = CreatePokemon(81, "tackle", ability: "자력");
        var steelTarget = CreatePokemon(81, "tackle");
        var nonSteelTarget = CreatePokemon(132, "tackle");

        Assert.False(engine.CanSwitch(steelTarget, magnetPull));
        Assert.True(engine.CanSwitch(nonSteelTarget, magnetPull));
        Assert.True(engine.CanSwitch(
            CreatePokemon(81, "tackle", ability: "자력"),
            magnetPull));
    }

    [Fact]
    public async Task Suction_cups_blocks_forced_switch_but_not_regular_switch()
    {
        var engine = CreateFullEngine();
        var suctionCups = CreatePokemon(686, "tackle", ability: "흡반");
        var attacker = CreatePokemon(95, "dragon-tail");
        var events = new List<BattleEvent>();

        Assert.False(suctionCups.CanBeForcedSwitched);
        Assert.True(engine.CanSwitch(suctionCups, attacker));

        var blocked = await engine.TakeTurnAsync(
            attacker, suctionCups, "dragon-tail", true, Capture(events));

        Assert.Null(blocked.ForcedSwitchPokemon);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("흡반", StringComparison.Ordinal) == true);

        var switchingAttacker = CreatePokemon(113, "u-turn");
        var freshSuctionCups = CreatePokemon(686, "tackle", ability: "흡반");
        var switchResult = await engine.TakeTurnAsync(
            switchingAttacker, freshSuctionCups, "u-turn", true, _ => Task.CompletedTask);
        Assert.Same(switchingAttacker, switchResult.ForcedSwitchPokemon);

        var normalTarget = CreatePokemon(132, "tackle");
        var allowed = await engine.TakeTurnAsync(
            CreatePokemon(95, "dragon-tail"), normalTarget, "dragon-tail", true, _ => Task.CompletedTask);

        Assert.Same(normalTarget, allowed.ForcedSwitchPokemon);
    }

    [Fact]
    public async Task Form_change_abilities_update_stats_and_emit_logs()
    {
        var aegislash = CreatePokemon(681, "tackle", "kings-shield", ability: "배틀스위치");
        int shieldAttack = aegislash.Atk;
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            aegislash, CreatePokemon(132, "tackle"), "tackle", true, Capture(events));

        Assert.True(aegislash.IsAlternateForm);
        Assert.True(aegislash.Atk > shieldAttack);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("공격모드", StringComparison.Ordinal) == true);

        await CreateFullEngine().TakeTurnAsync(
            aegislash, CreatePokemon(132, "tackle"), "kings-shield", true, Capture(events));
        Assert.False(aegislash.IsAlternateForm);
        Assert.True(aegislash.IsProtected);

        var contactAttacker = CreatePokemon(132, "tackle");
        await CreateFullEngine().TakeTurnAsync(
            contactAttacker, aegislash, "tackle", false, Capture(events));
        Assert.Equal(-2, contactAttacker.StatStages["attack"]);

        int hpBeforeBypass = aegislash.CurrentHp;
        Assert.True(MoveRuleMetadata.BypassesProtection("hyperspace-hole"));
        await CreateFullEngine().TakeTurnAsync(
            CreatePokemon(132, "hyperspace-hole"),
            aegislash,
            "hyperspace-hole",
            true,
            Capture(events));
        Assert.True(aegislash.CurrentHp < hpBeforeBypass);

        var darmanitan = CreatePokemon(555, "tackle", ability: "달마모드");
        int standardSpecialAttack = darmanitan.SpAtk;
        darmanitan.CurrentHp = darmanitan.MaxHp / 2;
        events.Clear();

        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(new[] { darmanitan }, Capture(events));

        Assert.True(darmanitan.IsAlternateForm);
        Assert.True(darmanitan.SpAtk > standardSpecialAttack);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("달마모드", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Multitype_uses_plate_type_for_judgment_stab_and_effectiveness()
    {
        Assert.True(AbilityDatabase.IsImplemented("멀티타입"));
        var arceus = CreatePokemon(
            493, "judgment", ability: "멀티타입", heldItem: "불꽃플레이트");
        var grassTarget = CreatePokemon(1, "tackle");

        Assert.Equal(PokemonType.Fire, arceus.CurrentType1);
        Assert.Null(arceus.CurrentType2);
        Assert.Equal(
            PokemonType.Fire,
            MoveRuleMetadata.ResolveMoveType("judgment", MoveDatabase.All["judgment"], arceus));

        var events = new List<BattleEvent>();
        await CreateFullEngine().TakeTurnAsync(
            arceus, grassTarget, "judgment", true, Capture(events));

        Assert.Equal(PokemonType.Fire, events.First(e =>
            e.Phase == BattleEventPhase.Impact && e.MoveKey == "judgment").EffectType);
        Assert.Equal(2.0, grassTarget.LastMultiplier);

        arceus.HeldItem = "물방울플레이트";
        Assert.Equal(PokemonType.Water, arceus.CurrentType1);
        Assert.Equal(
            PokemonType.Water,
            MoveRuleMetadata.ResolveMoveType("judgment", MoveDatabase.All["judgment"], arceus));
    }

    [Fact]
    public async Task Protean_changes_type_before_the_move_and_resets_on_switch()
    {
        Assert.True(AbilityDatabase.IsImplemented("변환자재"));
        var protean = CreatePokemon(352, "shadow-claw", ability: "변환자재");
        var target = CreatePokemon(6, "tackle");
        var events = new List<BattleEvent>();

        Assert.Equal(PokemonType.Normal, protean.CurrentType1);
        await CreateFullEngine().TakeTurnAsync(
            protean, target, "shadow-claw", true, Capture(events));

        Assert.Equal(PokemonType.Ghost, protean.CurrentType1);
        Assert.Null(protean.CurrentType2);
        Assert.Contains(events, battleEvent =>
            battleEvent.Phase == BattleEventPhase.Announce
            && battleEvent.EffectType == PokemonType.Ghost);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("변환자재", StringComparison.Ordinal) == true);

        CreateFullEngine().PrepareSwitchOut(protean);
        Assert.Equal(PokemonType.Normal, protean.CurrentType1);
        Assert.False(protean.IsTransformed);
    }

    [Fact]
    public async Task Color_change_uses_the_received_type_for_future_matchups()
    {
        Assert.True(AbilityDatabase.IsImplemented("변색"));
        var colorChanger = CreatePokemon(352, "tackle", ability: "변색");
        var waterAttacker = CreatePokemon(7, "water-gun");
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            waterAttacker, colorChanger, "water-gun", false, Capture(events));

        Assert.Equal(PokemonType.Water, colorChanger.CurrentType1);
        Assert.Null(colorChanger.CurrentType2);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("변색", StringComparison.Ordinal) == true);

        var electricMove = MoveDatabase.All["thunder-shock"];
        Assert.Equal(2.0, CreateFullEngine().PreviewMultiplier(
            electricMove, colorChanger, CreatePokemon(25, "thunder-shock")));
    }

    [Fact]
    public void Imposter_copies_species_form_moves_and_types_without_changing_hp_or_ability()
    {
        Assert.True(AbilityDatabase.IsImplemented("괴짜"));
        var ditto = CreatePokemon(132, "tackle", ability: "괴짜");
        var plateArceus = CreatePokemon(
            493, "judgment", ability: "멀티타입", heldItem: "불꽃플레이트");
        int dittoMaxHp = ditto.MaxHp;
        var messages = CreateFullEngine().ActivateSwitchIn(ditto, plateArceus);

        Assert.True(ditto.IsTransformed);
        Assert.Equal("아르세우스", ditto.Data.Name);
        Assert.Equal("괴짜", ditto.SelectedAbility);
        Assert.Equal(PokemonType.Fire, ditto.CurrentType1);
        Assert.Equal(dittoMaxHp, ditto.MaxHp);
        Assert.True(ditto.CanUseMove("judgment"));
        Assert.Contains(messages, message => message.Contains("괴짜", StringComparison.Ordinal));

        CreateFullEngine().PrepareSwitchOut(ditto);
        Assert.False(ditto.IsTransformed);
        Assert.Equal("메타몽", ditto.Data.Name);
        Assert.Equal(PokemonType.Normal, ditto.CurrentType1);
        Assert.True(ditto.CanUseMove("tackle"));
        Assert.False(ditto.CanUseMove("judgment"));
    }

    [Fact]
    public void Simple_uses_existing_stage_rules()
    {
        var simple = CreatePokemon(132, "tackle", ability: "단순");
        simple.ChangeStage("attack", 1);
        Assert.Equal(2, simple.StatStages["attack"]);
    }

    [Fact]
    public async Task Rock_head_mummy_and_aftermath_activate_at_their_documented_timing()
    {
        var rockHead = CreatePokemon(132, "take-down", ability: "돌머리");
        int hpBefore = rockHead.CurrentHp;

        await CreateFullEngine().TakeTurnAsync(
            rockHead, CreatePokemon(202, "tackle"), "take-down", true, _ => Task.CompletedTask);
        Assert.Equal(hpBefore, rockHead.CurrentHp);

        var contactAttacker = CreatePokemon(132, "tackle", ability: "유연");
        var mummy = CreatePokemon(202, "tackle", ability: "미라");
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            contactAttacker, mummy, "tackle", true, Capture(events));

        Assert.Equal("미라", contactAttacker.SelectedAbility);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("특성이 미라", StringComparison.Ordinal) == true);

        var aftermathAttacker = CreatePokemon(132, "tackle");
        var aftermath = CreatePokemon(132, "tackle", ability: "유폭");
        aftermath.CurrentHp = 1;
        hpBefore = aftermathAttacker.CurrentHp;
        events.Clear();

        await CreateFullEngine().TakeTurnAsync(
            aftermathAttacker, aftermath, "tackle", true, Capture(events));

        Assert.Equal(hpBefore - Math.Max(1, aftermathAttacker.MaxHp / 4), aftermathAttacker.CurrentHp);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("유폭", StringComparison.Ordinal) == true);
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

    private static BattleEngine CreateFullEngine(Random? random = null) => new(
        random ?? new Random(1234),
        new IBattleEffectHandler[]
        {
            new MoveEffectHandler(),
            new ContactReactionEffectHandler(),
            new AbilityLifecycleEffectHandler(),
            new DamageModifierEffectHandler()
        });

    private static Func<BattleEvent, Task> Capture(List<BattleEvent> events) =>
        battleEvent =>
        {
            events.Add(battleEvent);
            return Task.CompletedTask;
        };

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