namespace PokemonBattle.Models;

public static class TypeIcons
{
    private static readonly Dictionary<PokemonType, string> Map = new()
    {
        [PokemonType.Normal] = "normal", [PokemonType.Fire] = "fire", [PokemonType.Water] = "water",
        [PokemonType.Electric] = "electric", [PokemonType.Grass] = "grass", [PokemonType.Ice] = "ice",
        [PokemonType.Fighting] = "fighting", [PokemonType.Poison] = "poison", [PokemonType.Ground] = "ground",
        [PokemonType.Flying] = "flying", [PokemonType.Psychic] = "psychic", [PokemonType.Bug] = "bug",
        [PokemonType.Rock] = "rock", [PokemonType.Ghost] = "ghost", [PokemonType.Dragon] = "dragon",
        [PokemonType.Dark] = "dark", [PokemonType.Steel] = "steel", [PokemonType.Fairy] = "fairy",
    };

    public static string GetUrl(PokemonType type) =>
        $"https://raw.githubusercontent.com/msikma/pokesprite/master/misc/type-logos/gen8/{Map[type]}.png";
}
