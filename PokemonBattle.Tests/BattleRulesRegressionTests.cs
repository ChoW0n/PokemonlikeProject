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

    [Fact]
    public async Task Drain_healing_happens_before_life_orb_recoil()
    {
        var attacker = CreatePokemon(1, "giga-drain", ability: "노가드", heldItem: "생명의구슬");
        var defender = CreatePokemon(7, "tackle");
        attacker.CurrentHp = 1;
        var snapshot = new DamageResultSnapshotHandler();

        await CreateFullEngine(snapshot).TakeTurnAsync(
            attacker,
            defender,
            "giga-drain",
            attackerIsHero: true,
            emit: _ => Task.CompletedTask);

        Assert.True(snapshot.AttackerHpAfterDamageResult > attacker.CurrentHp);
        Assert.True(snapshot.AttackerHpAfterDamageResult > 1);
        Assert.Equal(
            snapshot.AttackerHpAfterDamageResult - Math.Max(1, attacker.MaxHp / 10),
            attacker.CurrentHp);
        Assert.False(attacker.IsFainted);
    }

    [Fact]
    public async Task Move_recoil_happens_before_life_orb_recoil()
    {
        var attacker = CreatePokemon(25, "take-down", ability: "노가드", heldItem: "생명의구슬");
        var defender = CreatePokemon(1, "tackle");
        int hpBefore = attacker.CurrentHp;
        var snapshot = new DamageResultSnapshotHandler();

        await CreateFullEngine(snapshot).TakeTurnAsync(
            attacker,
            defender,
            "take-down",
            attackerIsHero: true,
            emit: _ => Task.CompletedTask);

        int expectedMoveRecoil = Math.Max(1, (hpBefore - snapshot.AttackerHpAfterDamageResult));
        Assert.Equal(hpBefore - expectedMoveRecoil, snapshot.AttackerHpAfterDamageResult);
        Assert.Equal(
            snapshot.AttackerHpAfterDamageResult - Math.Max(1, attacker.MaxHp / 10),
            attacker.CurrentHp);
    }

    [Fact]
    public async Task Protection_moves_apply_only_their_declared_contact_effect()
    {
        var engine = CreateFullEngine();

        var kingsShield = CreatePokemon(1, "kings-shield");
        var kingsShieldAttacker = CreatePokemon(25, "tackle");
        var kingsShieldEvents = new List<BattleEvent>();
        await engine.TakeTurnAsync(
            kingsShield, kingsShieldAttacker, "kings-shield", false, Capture(kingsShieldEvents));
        await engine.TakeTurnAsync(
            kingsShieldAttacker, kingsShield, "tackle", true, Capture(kingsShieldEvents));
        Assert.Equal(-2, kingsShieldAttacker.StatStages["attack"]);
        Assert.Contains(kingsShieldEvents, battleEvent =>
            battleEvent.Message?.Contains("킹실드", StringComparison.Ordinal) == true);

        var obstruct = CreatePokemon(1, "obstruct");
        var obstructAttacker = CreatePokemon(25, "tackle");
        await engine.TakeTurnAsync(obstruct, obstructAttacker, "obstruct", false, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(obstructAttacker, obstruct, "tackle", true, _ => Task.CompletedTask);
        Assert.Equal(-2, obstructAttacker.StatStages["defense"]);

        var spikyShield = CreatePokemon(1, "spiky-shield");
        var spikyShieldAttacker = CreatePokemon(25, "tackle");
        int spikyHpBefore = spikyShieldAttacker.CurrentHp;
        await engine.TakeTurnAsync(spikyShield, spikyShieldAttacker, "spiky-shield", false, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(spikyShieldAttacker, spikyShield, "tackle", true, _ => Task.CompletedTask);
        Assert.Equal(spikyHpBefore - Math.Max(1, spikyShieldAttacker.MaxHp / 8), spikyShieldAttacker.CurrentHp);

        var banefulBunker = CreatePokemon(1, "baneful-bunker");
        var banefulAttacker = CreatePokemon(25, "tackle");
        await engine.TakeTurnAsync(banefulBunker, banefulAttacker, "baneful-bunker", false, _ => Task.CompletedTask);
        await engine.TakeTurnAsync(banefulAttacker, banefulBunker, "tackle", true, _ => Task.CompletedTask);
        Assert.Equal(StatusCondition.Poison, banefulAttacker.Status);
    }

    [Fact]
    public async Task Multi_hit_contact_reaction_runs_for_each_hit_before_one_life_orb_recoil()
    {
        var attacker = CreatePokemon(25, "double-hit", ability: "노가드", heldItem: "생명의구슬");
        var defender = CreatePokemon(1, "tackle", ability: "철가시");
        var events = new List<BattleEvent>();

        await CreateFullEngine().TakeTurnAsync(
            attacker,
            defender,
            "double-hit",
            attackerIsHero: true,
            emit: battleEvent =>
            {
                events.Add(battleEvent);
                return Task.CompletedTask;
            });

        int reflectedPerHit = Math.Max(1, defender.MaxHp / 8);
        int lifeOrbRecoil = Math.Max(1, attacker.MaxHp / 10);
        Assert.Equal(2, events.Count(battleEvent =>
            battleEvent.Message?.Contains("철가시", StringComparison.Ordinal) == true));
        Assert.Contains(events, battleEvent =>
            battleEvent.Message?.Contains("2번 맞았다", StringComparison.Ordinal) == true);
        Assert.Equal(attacker.MaxHp - reflectedPerHit * 2 - lifeOrbRecoil, attacker.CurrentHp);
        Assert.False(attacker.IsFainted);
    }

    [Fact]
    public async Task Status_and_stat_changes_run_after_damage_result()
    {
        const string moveKey = "regression-status-strike";
        MoveDatabase.All[moveKey] = new Move(
            "회귀 상태 공격",
            40,
            PokemonType.Normal,
            10,
            100,
            true,
            0,
            false,
            false,
            "poison",
            100,
            0,
            new List<StatChangeEntry>
            {
                new() { Stat = "attack", Change = -1, TargetsSelf = false }
            },
            100,
            "회귀 테스트용 기술",
            0,
            0,
            1,
            1);

        try
        {
            var attacker = CreatePokemon(25, moveKey, ability: "노가드");
            var defender = CreatePokemon(1, "tackle");
            var snapshot = new DamageResultSnapshotHandler();

            await CreateFullEngine(snapshot).TakeTurnAsync(
                attacker,
                defender,
                moveKey,
                attackerIsHero: true,
                emit: _ => Task.CompletedTask);

            Assert.Equal(StatusCondition.None, snapshot.DefenderStatusAfterDamageResult);
            Assert.Equal(0, snapshot.DefenderAttackStageAfterDamageResult);
            Assert.Equal(StatusCondition.Poison, snapshot.DefenderStatusAfterMove);
            Assert.Equal(-1, snapshot.DefenderAttackStageAfterMove);
        }
        finally
        {
            MoveDatabase.All.Remove(moveKey);
        }
    }

    [Fact]
    public async Task Handler_discovery_order_does_not_override_declared_order()
    {
        var calls = new List<string>();
        var late = new OrderProbeHandler("late", order: 200, calls);
        var early = new OrderProbeHandler("early", order: 100, calls);
        var engine = new BattleEngine(
            new Random(1234),
            new IBattleEffectHandler[] { late, early });

        await engine.TakeTurnAsync(
            CreatePokemon(25, "tackle"),
            CreatePokemon(1, "tackle"),
            "tackle",
            attackerIsHero: true,
            emit: _ => Task.CompletedTask);

        Assert.Equal(new[] { "early", "late" }, calls);
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

    private static BattleEngine CreateFullEngine(params IBattleEffectHandler[] additionalHandlers)
    {
        // Deliberately reverse the normal discovery order: BattleEngine must use Order.
        var handlers = new List<IBattleEffectHandler>
        {
            new DamageModifierEffectHandler(),
            new AbilityLifecycleEffectHandler(),
            new ContactReactionEffectHandler(),
            new MoveEffectHandler()
        };
        handlers.AddRange(additionalHandlers);
        return new BattleEngine(
            new Random(1234),
            handlers.AsEnumerable().Reverse());
    }

    private static Func<BattleEvent, Task> Capture(List<BattleEvent> events) =>
        battleEvent =>
        {
            events.Add(battleEvent);
            return Task.CompletedTask;
        };

    private static int ExpectedDamage(Pokemon attacker, Pokemon defender, string moveKey)
    {
        var move = MoveDatabase.All[moveKey];
        int attack = move.IsSpecial ? attacker.EffectiveSpAtk : attacker.EffectiveAtk;
        int defense = move.IsSpecial ? defender.EffectiveSpDef : defender.EffectiveDef;
        return Math.Max(0, (int)(move.Power * ((double)attack / Math.Max(defense, 1))));
    }

    private sealed class DamageResultSnapshotHandler : IBattleEffectHandler
    {
        public int Order => 150;
        public int AttackerHpAfterDamageResult { get; private set; }
        public StatusCondition DefenderStatusAfterDamageResult { get; private set; }
        public int DefenderAttackStageAfterDamageResult { get; private set; }
        public StatusCondition DefenderStatusAfterMove { get; private set; }
        public int DefenderAttackStageAfterMove { get; private set; }

        public Task AfterDamageResultAsync(BattleEffectContext context)
        {
            AttackerHpAfterDamageResult = context.Attacker.CurrentHp;
            DefenderStatusAfterDamageResult = context.Defender.Status;
            DefenderAttackStageAfterDamageResult = context.Defender.StatStages["attack"];
            return Task.CompletedTask;
        }

        public Task AfterMoveAsync(BattleEffectContext context)
        {
            DefenderStatusAfterMove = context.Defender.Status;
            DefenderAttackStageAfterMove = context.Defender.StatStages["attack"];
            return Task.CompletedTask;
        }
    }

    private sealed class OrderProbeHandler : IBattleEffectHandler
    {
        private readonly string name;
        private readonly List<string> calls;

        public OrderProbeHandler(string name, int order, List<string> calls)
        {
            this.name = name;
            Order = order;
            this.calls = calls;
        }

        public int Order { get; }

        public Task AfterMoveAsync(BattleEffectContext context)
        {
            calls.Add(name);
            return Task.CompletedTask;
        }
    }
}