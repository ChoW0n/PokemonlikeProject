using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class BattlePresentationRegressionTests
{
    [Fact]
    public async Task TakeTurn_emits_ordered_move_timeline_with_actor_and_damage_metadata()
    {
        var attacker = new Pokemon(PokemonDatabase.All[25], new List<string> { "thunderbolt" }, level: 50);
        var defender = new Pokemon(PokemonDatabase.All[1], new List<string> { "tackle" }, level: 50);
        var events = new List<BattleEvent>();

        await CreateEngine().TakeTurnAsync(
            attacker,
            defender,
            "thunderbolt",
            attackerIsHero: true,
            emit: battleEvent =>
            {
                events.Add(battleEvent);
                return Task.CompletedTask;
            });

        var moveEvents = events
            .Where(item => item.MoveKey == "thunderbolt")
            .ToList();

        Assert.Equal(
            new[] { BattleEventPhase.Announce, BattleEventPhase.Windup, BattleEventPhase.Impact, BattleEventPhase.Recovery },
            moveEvents.Select(item => item.Phase).ToArray());

        var impact = moveEvents.Single(item => item.Phase == BattleEventPhase.Impact);
        Assert.Equal(attacker.ActorId, impact.AttackerActorId);
        Assert.Equal(defender.ActorId, impact.DefenderActorId);
        Assert.Equal("pikachu", impact.AttackerSpecies);
        Assert.Equal("bulbasaur", impact.DefenderSpecies);
        Assert.Equal("special", impact.MoveCategory);
        Assert.Equal("opponent", impact.Target);
        Assert.True(impact.Damage >= 0);
        Assert.Equal(impact.HpBefore - impact.Damage, impact.HpAfter);
    }

    [Fact]
    public void Every_generated_move_has_a_presentation_fallback()
    {
        Assert.True(MoveDatabase.All.Count >= 493);

        foreach (var (moveKey, move) in MoveDatabase.All)
        {
            var presentation = MovePresentationCatalog.Resolve(moveKey, move);
            Assert.False(string.IsNullOrWhiteSpace(presentation), moveKey);
        }
    }

    private static BattleEngine CreateEngine() =>
        new(new Random(1234), Array.Empty<IBattleEffectHandler>());
}