using PokemonBattle.Models;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class TechnicalMachineRegressionTests
{
    [Fact]
    public void Machine_only_metadata_matches_known_pokeapi_learning_methods()
    {
        var bulbasaur = PokemonDatabase.All[1];

        Assert.Contains("cut", bulbasaur.MachineOnlyMoveNames);
        Assert.Contains("protect", bulbasaur.MachineOnlyMoveNames);
        Assert.Contains("tackle", bulbasaur.MoveNames);
        Assert.DoesNotContain("tackle", bulbasaur.MachineOnlyMoveNames);
        Assert.Contains("cut", bulbasaur.MoveNames);
    }

    [Fact]
    public void Machine_only_metadata_contains_only_implemented_moves_and_has_no_duplicates()
    {
        foreach (var data in PokemonDatabase.All.Values)
        {
            Assert.Equal(
                data.MachineOnlyMoveNames.Length,
                data.MachineOnlyMoveNames.Distinct(StringComparer.Ordinal).Count());
            Assert.All(
                data.MachineOnlyMoveNames,
                moveKey =>
                {
                    Assert.Contains(moveKey, data.MoveNames, StringComparer.Ordinal);
                    Assert.True(MoveDatabase.All.ContainsKey(moveKey));
                });
        }
    }
}