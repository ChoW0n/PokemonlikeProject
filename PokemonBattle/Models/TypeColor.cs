namespace PokemonBattle.Models;

//기술 연출(빔/베기/파동)에 입힐 속성별 색상
public static class TypeColors
{
    private static readonly Dictionary<PokemonType, string> Map = new()
    {
        [PokemonType.Normal] = "#A8A878", [PokemonType.Fire] = "#F08030", [PokemonType.Water] = "#6890F0",
        [PokemonType.Electric] = "#F8D030", [PokemonType.Grass] = "#78C850", [PokemonType.Ice] = "#98D8D8",
        [PokemonType.Fighting] = "#C03028", [PokemonType.Poison] = "#A040A0", [PokemonType.Ground] = "#E0C068",
        [PokemonType.Flying] = "#A890F0", [PokemonType.Psychic] = "#F85888", [PokemonType.Bug] = "#A8B820",
        [PokemonType.Rock] = "#B8A038", [PokemonType.Ghost] = "#705898", [PokemonType.Dragon] = "#7038F8",
        [PokemonType.Dark] = "#705848", [PokemonType.Steel] = "#B8B8D0", [PokemonType.Fairy] = "#EE99AC",
    };

    public static string GetHex(PokemonType t) => Map[t];
    //속성군에 따라 5종 이펙트 카테고리 중 하나를 배정 (변화기는 항상 sparkle)
    public static string GetEffectKind(PokemonType type, bool isStatus)
    {
        if (isStatus) return "sparkle";

        return type switch
        {
            PokemonType.Water or PokemonType.Ice or PokemonType.Dragon or PokemonType.Steel => "pierce",
            PokemonType.Fire or PokemonType.Electric => "burst",
            PokemonType.Ground or PokemonType.Rock or PokemonType.Fighting or PokemonType.Normal => "impact",
            PokemonType.Grass or PokemonType.Bug or PokemonType.Poison or PokemonType.Flying => "multi",
            PokemonType.Psychic or PokemonType.Ghost or PokemonType.Fairy or PokemonType.Dark => "sparkle",
            _ => "burst"
        };
    }

}
