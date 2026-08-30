using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public class ProgressionRegressionTests
{
    [Fact]
    public void PresetStoreKeepsIndependentLevelOneSnapshot()
    {
        var store = new InMemoryPresetStore();
        var original = new PokemonLoadout
        {
            PokemonId = 1,
            ChosenMoveNames = new List<string> { "tackle" },
            ChosenAbility = "심록",
            ChosenItem = "기합의띠",
            Level = 8
        };

        store.Save("팀", new List<PokemonLoadout> { original });
        original.Level = 20;
        original.ChosenMoveNames.Add("growl");

        var loaded = Assert.Single(store.Load("팀")!);
        Assert.Equal(1, loaded.Level);
        Assert.Equal(new[] { "tackle" }, loaded.ChosenMoveNames);

        loaded.ChosenMoveNames.Add("vine-whip");
        var loadedAgain = Assert.Single(store.Load("팀")!);
        Assert.Equal(new[] { "tackle" }, loadedAgain.ChosenMoveNames);
    }

    [Fact]
    public void PresetKeepsCurrentRunLevelOnlyForExistingPokemon()
    {
        var preset = new[]
        {
            new PokemonLoadout { PokemonId = 1, Level = 1 },
            new PokemonLoadout { PokemonId = 4, Level = 1 }
        };
        var currentRun = new[]
        {
            new PokemonLoadout { PokemonId = 1, Level = 12 }
        };

        var merged = PresetLoadoutMapper.ApplyCurrentRunLevels(preset, currentRun);

        Assert.Equal(12, merged.Single(loadout => loadout.PokemonId == 1).Level);
        Assert.Equal(1, merged.Single(loadout => loadout.PokemonId == 4).Level);
    }

    [Fact]
    public void LegendaryProgressClampsAtOneHundred()
    {
        Assert.Equal(100, LegendaryProgression.AddProgress(96, 20));
        Assert.Equal(0, LegendaryProgression.AddProgress(0, -10));
        Assert.True(LegendaryProgression.IsUnlocked(100));
        Assert.False(LegendaryProgression.IsUnlocked(99));
    }

    [Fact]
    public void LegendaryPoolOpensOnlyAfterUnlock()
    {
        var excluded = new HashSet<int>();
        var lockedTeam = Enumerable.Range(0, 20)
            .Select(_ => EnemyTeamProvider.GetRandomTeam(6, 721, false, excluded, legendaryUnlocked: false))
            .SelectMany(team => team)
            .ToList();

        Assert.DoesNotContain(lockedTeam, entry => EnemyTeamProvider.IsLegendary(entry.Key));

        var unlockedTeam = Enumerable.Range(0, 100)
            .Select(_ => EnemyTeamProvider.GetRandomTeam(6, 721, false, excluded, legendaryUnlocked: true))
            .SelectMany(team => team)
            .ToList();

        Assert.Contains(unlockedTeam, entry => EnemyTeamProvider.IsLegendary(entry.Key));
    }
}