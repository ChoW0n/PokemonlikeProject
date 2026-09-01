using PokemonBattle.Models;
using Xunit;

namespace PokemonBattle.Tests;

public class SpriteRegressionTests
{
    [Fact]
    public void Braviary_uses_the_matching_community_front_and_back_sprites()
    {
        var braviary = PokemonDatabase.All[628];

        Assert.Equal(
            "https://raw.githubusercontent.com/Ghasty001/Animated_sprites_by_Ghasty001/main/FRONT/BRAVIARY.gif",
            braviary.EffectiveImageUrl);
        Assert.Equal(
            "https://raw.githubusercontent.com/Ghasty001/Animated_sprites_by_Ghasty001/main/BACK/BRAVIARY.gif",
            braviary.EffectiveBackImageUrl);
    }

    [Fact]
    public void Pokemon_without_an_override_keeps_the_database_sprite()
    {
        var pikachu = PokemonDatabase.All[25];

        Assert.Equal(pikachu.ImageUrl, pikachu.EffectiveImageUrl);
        Assert.Equal(pikachu.BackImageUrl, pikachu.EffectiveBackImageUrl);
    }

    [Fact]
    public void Pokemon_database_contains_official_height_data_for_small_and_large_species()
    {
        Assert.Equal(3, PokemonDatabase.All[172].HeightDecimeters); //피츄
        Assert.Equal(15, PokemonDatabase.All[92].HeightDecimeters); //고오스
        Assert.Equal(21, PokemonDatabase.All[143].HeightDecimeters); //잠만보
        Assert.Equal(0.72, PokemonSpriteScale.ForHeight(3));
        Assert.True(PokemonSpriteScale.ForHeight(21) > PokemonSpriteScale.ForHeight(3));
        Assert.Equal(PokemonSpriteScale.MaximumScale, PokemonSpriteScale.ForHeight(145));
    }
}