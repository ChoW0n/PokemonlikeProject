using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class StatusAndItemRegressionTests
{
    [Fact]
    public async Task Guaranteed_status_is_applied_then_poison_damages_at_turn_end()
    {
        const string moveKey = "regression-guaranteed-poison-turn-end";
        RegisterMove(moveKey, new Move(
            "회귀 확정 독", 0, PokemonType.Poison, 10, 100, true, 0,
            true, false, "poison", 100, 0, new List<StatChangeEntry>(), 0,
            "회귀 테스트용 기술", 0, 0, 1, 1));

        try
        {
            BattleWeather.Current = "맑음";
            var attacker = CreatePokemon(132, moveKey);
            var defender = CreatePokemon(202, "tackle");
            var events = new List<BattleEvent>();

            await CreateFullEngine().TakeTurnAsync(
                attacker, defender, moveKey, attackerIsHero: true, Capture(events));

            Assert.Equal(StatusCondition.Poison, defender.Status);
            int hpBeforeEndOfTurn = defender.CurrentHp;

            await CreateFullEngine().ApplyEndOfTurnEffectsAsync(
                new[] { defender }, Capture(events));

            Assert.Equal(
                hpBeforeEndOfTurn - Math.Max(1, defender.MaxHp / 8),
                defender.CurrentHp);
            Assert.Equal(1, defender.TurnsOnField);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("독으로 데미지를 입었다", StringComparison.Ordinal) == true);
        }
        finally
        {
            MoveDatabase.All.Remove(moveKey);
        }
    }

    [Fact]
    public async Task Sleep_status_with_one_turn_remaining_skips_one_turn_before_waking()
    {
        const string moveKey = "regression-guaranteed-sleep";
        RegisterMove(moveKey, new Move(
            "회귀 확정 잠듦", 0, PokemonType.Psychic, 10, 100, true, 0,
            true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0,
            "회귀 테스트용 기술", 0, 0, 1, 1));

        try
        {
            var attacker = CreatePokemon(96, moveKey);
            var sleepingPokemon = CreatePokemon(202, "tackle");
            var events = new List<BattleEvent>();

            await CreateFullEngine().TakeTurnAsync(
                attacker, sleepingPokemon, moveKey, attackerIsHero: true, Capture(events));

            Assert.Equal(StatusCondition.Sleep, sleepingPokemon.Status);

            // ApplyAilment intentionally rolls a sleep duration. Pin the duration here
            // so the action-prevention and wake-up assertions remain deterministic.
            sleepingPokemon.SleepTurnsRemaining = 1;
            int ppBefore = sleepingPokemon.CurrentPP["tackle"];

            events.Clear();
            await CreateFullEngine().TakeTurnAsync(
                sleepingPokemon, attacker, "tackle", attackerIsHero: false, Capture(events));

            Assert.Equal(StatusCondition.Sleep, sleepingPokemon.Status);
            Assert.Equal(0, sleepingPokemon.SleepTurnsRemaining);
            Assert.Equal(ppBefore, sleepingPokemon.CurrentPP["tackle"]);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("잠들어 있다", StringComparison.Ordinal) == true);

            events.Clear();
            await CreateFullEngine().TakeTurnAsync(
                sleepingPokemon, attacker, "tackle", attackerIsHero: false, Capture(events));

            Assert.Equal(StatusCondition.None, sleepingPokemon.Status);
            Assert.Equal(ppBefore - 1, sleepingPokemon.CurrentPP["tackle"]);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("잠에서 깼다", StringComparison.Ordinal) == true);
        }
        finally
        {
            MoveDatabase.All.Remove(moveKey);
        }
    }

    [Fact]
    public async Task Lum_berry_cures_poison_after_turn_end_damage_and_is_consumed()
    {
        BattleWeather.Current = "맑음";
        var pokemon = CreatePokemon(202, "tackle", heldItem: "리샘열매");
        pokemon.Status = StatusCondition.Poison;
        int hpBeforeEndOfTurn = pokemon.CurrentHp;
        var events = new List<BattleEvent>();

        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(
            new[] { pokemon }, Capture(events));

        Assert.Equal(
            hpBeforeEndOfTurn - Math.Max(1, pokemon.MaxHp / 8),
            pokemon.CurrentHp);
        Assert.Equal(StatusCondition.None, pokemon.Status);
        Assert.Equal("없음", pokemon.HeldItem);
        Assert.Equal(1, pokemon.TurnsOnField);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("리샘열매", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Type_boosting_item_changes_the_deterministic_damage_result()
    {
        BattleWeather.Current = "맑음";
        var attacker = CreatePokemon(25, "thunderbolt", heldItem: "자석");
        var defender = CreatePokemon(202, "tackle", ability: "조가비갑옷");
        var move = MoveDatabase.All["thunderbolt"];
        int hpBefore = defender.CurrentHp;

        await CreateFullEngine().TakeTurnAsync(
            attacker, defender, "thunderbolt", attackerIsHero: true,
            _ => Task.CompletedTask);

        int expectedDamage = (int)(((2.0 * attacker.Level / 5 + 2)
            * move.Power
            * 1.5 // Electric STAB.
            * 1.2 // 자석.
            * ((double)attacker.EffectiveSpAtk / defender.EffectiveSpDef)) / 50) + 2;

        Assert.Equal(expectedDamage, hpBefore - defender.CurrentHp);
        Assert.False(defender.IsFainted);
    }

    [Fact]
    public async Task Focus_sash_changes_a_lethal_damage_result_to_one_hp()
    {
        const string moveKey = "regression-focus-sash-lethal";
        RegisterMove(moveKey, new Move(
            "회귀 일격", 250, PokemonType.Normal, 5, 100, true, 0,
            false, false, "none", 0, 0, new List<StatChangeEntry>(), 0,
            "회귀 테스트용 강한 공격", 0, 0, 1, 1));

        try
        {
            var attacker = CreatePokemon(25, moveKey);
            var defender = CreatePokemon(
                10, "tackle", ability: "조가비갑옷", heldItem: "기합의띠");

            await CreateFullEngine().TakeTurnAsync(
                attacker, defender, moveKey, attackerIsHero: true,
                _ => Task.CompletedTask);

            Assert.Equal(1, defender.CurrentHp);
            Assert.False(defender.IsFainted);
            Assert.True(defender.SurvivedByEndure);
        }
        finally
        {
            MoveDatabase.All.Remove(moveKey);
        }
    }

    [Fact]
    public void Assault_vest_boosts_special_defense_and_blocks_status_moves()
    {
        var withoutItem = CreatePokemon(25, "thunderbolt");
        var withVest = CreatePokemon(25, "thunderbolt", heldItem: "돌격조끼");
        var statusUser = CreatePokemon(25, "growl", heldItem: "돌격조끼");

        Assert.Equal((int)(withoutItem.SpDef * 1.5), withVest.SpDef);
        Assert.False(statusUser.CanUseMove("growl"));
    }

    [Fact]
    public async Task Air_balloon_blocks_ground_damage_then_pops_on_damage()
    {
        var attacker = CreatePokemon(74, "earthquake", "tackle");
        var defender = CreatePokemon(202, "tackle", heldItem: "풍선");
        int hpBefore = defender.CurrentHp;
        var engine = CreateFullEngine();

        await engine.TakeTurnAsync(attacker, defender, "earthquake", true, _ => Task.CompletedTask);

        Assert.Equal(hpBefore, defender.CurrentHp);
        Assert.Equal("풍선", defender.HeldItem);

        await engine.TakeTurnAsync(attacker, defender, "tackle", true, _ => Task.CompletedTask);

        Assert.True(defender.CurrentHp < hpBefore);
        Assert.Equal("없음", defender.HeldItem);
    }

    [Fact]
    public async Task Rocky_helmet_reflects_contact_damage()
    {
        var attacker = CreatePokemon(25, "tackle");
        var defender = CreatePokemon(202, "tackle", heldItem: "울퉁불퉁멧");
        int hpBefore = attacker.CurrentHp;

        await CreateFullEngine().TakeTurnAsync(
            attacker, defender, "tackle", attackerIsHero: true, _ => Task.CompletedTask);

        Assert.Equal(hpBefore - Math.Max(1, attacker.MaxHp / 6), attacker.CurrentHp);
    }

    [Fact]
    public async Task Black_sludge_heals_poison_types_at_turn_end()
    {
        var pokemon = CreatePokemon(1, "tackle", heldItem: "검은진흙");
        pokemon.CurrentHp -= 20;
        int before = pokemon.CurrentHp;

        await CreateFullEngine().ApplyEndOfTurnEffectsAsync(
            new[] { pokemon }, _ => Task.CompletedTask);

        Assert.Equal(before + Math.Max(1, pokemon.MaxHp / 16), pokemon.CurrentHp);
    }

    [Fact]
    public async Task Weakness_policy_boosts_both_attack_stats_once()
    {
        var attacker = CreatePokemon(25, "thunderbolt");
        var defender = CreatePokemon(7, "tackle", heldItem: "약점보험");
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            attacker, defender, "thunderbolt", attackerIsHero: true, Capture(events));

        Assert.Equal("없음", defender.HeldItem);
        Assert.Equal(2, defender.StatStages["attack"]);
        Assert.Equal(2, defender.StatStages["special-attack"]);
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("약점보험", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Safety_goggles_prevent_powder_status()
    {
        var attacker = CreatePokemon(1, "sleep-powder");
        var defender = CreatePokemon(202, "tackle", heldItem: "방진고글");

        await CreateFullEngine().TakeTurnAsync(
            attacker, defender, "sleep-powder", attackerIsHero: true, _ => Task.CompletedTask);

        Assert.Equal(StatusCondition.None, defender.Status);
    }

    [Fact]
    public async Task Covert_cloak_does_not_block_a_pure_status_move()
    {
        var attacker = CreatePokemon(1, "sleep-powder");
        var defender = CreatePokemon(202, "tackle", heldItem: "은밀망토");

        await CreateFullEngine(new FixedRandom(0)).TakeTurnAsync(
            attacker, defender, "sleep-powder", attackerIsHero: true, _ => Task.CompletedTask);

        Assert.Equal(StatusCondition.Sleep, defender.Status);
    }

    [Fact]
    public async Task Overcoat_blocks_powder_status_even_when_the_move_is_status()
    {
        var attacker = CreatePokemon(1, "sleep-powder");
        var defender = CreatePokemon(202, "tackle", ability: "인분");

        await CreateFullEngine(new FixedRandom(0)).TakeTurnAsync(
            attacker, defender, "sleep-powder", attackerIsHero: true, _ => Task.CompletedTask);

        Assert.Equal(StatusCondition.None, defender.Status);
    }

    [Fact]
    public void Powder_move_catalog_covers_the_supported_powder_family()
    {
        foreach (var moveKey in new[]
        {
            "sleep-powder", "stun-spore", "poison-powder", "spore", "cotton-spore"
        })
        {
            Assert.True(MoveRuleMetadata.IsPowderMove(moveKey));
        }
    }

    [Fact]
    public async Task White_herb_restores_self_stat_drop()
    {
        var attacker = CreatePokemon(68, "close-combat", heldItem: "하얀허브");
        var defender = CreatePokemon(202, "tackle");

        await CreateFullEngine().TakeTurnAsync(
            attacker, defender, "close-combat", attackerIsHero: true, _ => Task.CompletedTask);

        Assert.Equal(0, attacker.StatStages["defense"]);
        Assert.Equal(0, attacker.StatStages["special-defense"]);
        Assert.Equal("없음", attacker.HeldItem);
    }

    [Fact]
    public async Task Endure_takes_normal_damage_but_prevents_a_lethal_hit()
    {
        const string moveKey = "regression-endure-lethal";
        RegisterMove(moveKey, new Move(
            "회귀 일격", 250, PokemonType.Normal, 5, 100, true, 0,
            false, false, "none", 0, 0, new List<StatChangeEntry>(), 0,
            "회귀 테스트용 강한 공격", 0, 0, 1, 1));

        try
        {
            var attacker = CreatePokemon(25, moveKey);
            var defender = CreatePokemon(10, "endure");
            var events = new List<BattleEvent>();
            var engine = CreateFullEngine();

            await engine.TakeTurnAsync(
                defender, attacker, "endure", attackerIsHero: false, Capture(events));
            int hpBeforeAttack = defender.CurrentHp;

            await engine.TakeTurnAsync(
                attacker, defender, moveKey, attackerIsHero: true, Capture(events));

            Assert.True(hpBeforeAttack - defender.CurrentHp > 0);
            Assert.Equal(1, defender.CurrentHp);
            Assert.False(defender.IsFainted);
            Assert.True(defender.SurvivedByEndure);
            Assert.Contains(events, battleEvent =>
                battleEvent.Message?.Contains("버텨냈다", StringComparison.Ordinal) == true);
        }
        finally
        {
            MoveDatabase.All.Remove(moveKey);
        }
    }

    private static void RegisterMove(string moveKey, Move move) =>
        MoveDatabase.All[moveKey] = move;

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