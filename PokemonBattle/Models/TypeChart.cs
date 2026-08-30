namespace PokemonBattle.Models;

//속성 상성표: 공격 타입 -> 방어 타입 -> 배율 (6세대 기준, 페어리 타입 포함)
public static class TypeChart
{
    public static Dictionary<PokemonType, Dictionary<PokemonType, double>> Chart =
        new Dictionary<PokemonType, Dictionary<PokemonType, double>>();

    private static readonly Dictionary<PokemonType, PokemonType[]> SuperEffective = new()
    {
        [PokemonType.Fire] = new[] { PokemonType.Grass, PokemonType.Ice, PokemonType.Bug, PokemonType.Steel },
        [PokemonType.Water] = new[] { PokemonType.Fire, PokemonType.Ground, PokemonType.Rock },
        [PokemonType.Electric] = new[] { PokemonType.Water, PokemonType.Flying },
        [PokemonType.Grass] = new[] { PokemonType.Water, PokemonType.Ground, PokemonType.Rock },
        [PokemonType.Ice] = new[] { PokemonType.Grass, PokemonType.Ground, PokemonType.Flying, PokemonType.Dragon },
        [PokemonType.Fighting] = new[] { PokemonType.Normal, PokemonType.Ice, PokemonType.Rock, PokemonType.Dark, PokemonType.Steel },
        [PokemonType.Poison] = new[] { PokemonType.Grass, PokemonType.Fairy },
        [PokemonType.Ground] = new[] { PokemonType.Fire, PokemonType.Electric, PokemonType.Poison, PokemonType.Rock, PokemonType.Steel },
        [PokemonType.Flying] = new[] { PokemonType.Grass, PokemonType.Fighting, PokemonType.Bug },
        [PokemonType.Psychic] = new[] { PokemonType.Fighting, PokemonType.Poison },
        [PokemonType.Bug] = new[] { PokemonType.Grass, PokemonType.Psychic, PokemonType.Dark },
        [PokemonType.Rock] = new[] { PokemonType.Fire, PokemonType.Ice, PokemonType.Flying, PokemonType.Bug },
        [PokemonType.Ghost] = new[] { PokemonType.Psychic, PokemonType.Ghost },
        [PokemonType.Dragon] = new[] { PokemonType.Dragon },
        [PokemonType.Dark] = new[] { PokemonType.Psychic, PokemonType.Ghost },
        [PokemonType.Steel] = new[] { PokemonType.Ice, PokemonType.Rock, PokemonType.Fairy },
        [PokemonType.Fairy] = new[] { PokemonType.Fighting, PokemonType.Dragon, PokemonType.Dark },
    };

    private static readonly Dictionary<PokemonType, PokemonType[]> NotEffective = new()
    {
        [PokemonType.Normal] = new[] { PokemonType.Rock, PokemonType.Steel },
        [PokemonType.Fire] = new[] { PokemonType.Fire, PokemonType.Water, PokemonType.Rock, PokemonType.Dragon },
        [PokemonType.Water] = new[] { PokemonType.Water, PokemonType.Grass, PokemonType.Dragon },
        [PokemonType.Electric] = new[] { PokemonType.Electric, PokemonType.Grass, PokemonType.Dragon },
        [PokemonType.Grass] = new[] { PokemonType.Fire, PokemonType.Grass, PokemonType.Poison, PokemonType.Flying, PokemonType.Bug, PokemonType.Dragon, PokemonType.Steel },
        [PokemonType.Ice] = new[] { PokemonType.Fire, PokemonType.Water, PokemonType.Ice, PokemonType.Steel },
        [PokemonType.Fighting] = new[] { PokemonType.Poison, PokemonType.Flying, PokemonType.Psychic, PokemonType.Bug, PokemonType.Fairy },
        [PokemonType.Poison] = new[] { PokemonType.Poison, PokemonType.Ground, PokemonType.Rock, PokemonType.Ghost },
        [PokemonType.Ground] = new[] { PokemonType.Grass, PokemonType.Bug },
        [PokemonType.Flying] = new[] { PokemonType.Electric, PokemonType.Rock, PokemonType.Steel },
        [PokemonType.Psychic] = new[] { PokemonType.Psychic, PokemonType.Steel },
        [PokemonType.Bug] = new[] { PokemonType.Fire, PokemonType.Fighting, PokemonType.Poison, PokemonType.Flying, PokemonType.Ghost, PokemonType.Steel, PokemonType.Fairy },
        [PokemonType.Rock] = new[] { PokemonType.Fighting, PokemonType.Ground, PokemonType.Steel },
        [PokemonType.Ghost] = new[] { PokemonType.Dark },
        [PokemonType.Dragon] = new[] { PokemonType.Steel },
        [PokemonType.Dark] = new[] { PokemonType.Fighting, PokemonType.Dark, PokemonType.Fairy },
        [PokemonType.Steel] = new[] { PokemonType.Fire, PokemonType.Water, PokemonType.Electric, PokemonType.Steel },
        [PokemonType.Fairy] = new[] { PokemonType.Fire, PokemonType.Poison, PokemonType.Steel },
    };

    private static readonly Dictionary<PokemonType, PokemonType[]> NoEffect = new()
    {
        [PokemonType.Normal] = new[] { PokemonType.Ghost },
        [PokemonType.Electric] = new[] { PokemonType.Ground },
        [PokemonType.Fighting] = new[] { PokemonType.Ghost },
        [PokemonType.Poison] = new[] { PokemonType.Steel },
        [PokemonType.Ground] = new[] { PokemonType.Flying },
        [PokemonType.Psychic] = new[] { PokemonType.Dark },
        [PokemonType.Ghost] = new[] { PokemonType.Normal },
        [PokemonType.Dragon] = new[] { PokemonType.Fairy },
    };

    static TypeChart()
    {
        var allTypes = Enum.GetValues<PokemonType>();

        foreach (var attackType in allTypes)
        {
            Chart[attackType] = new Dictionary<PokemonType, double>();
            foreach (var defendType in allTypes)
            {
                Chart[attackType][defendType] = 1.0;
            }

            if (SuperEffective.TryGetValue(attackType, out var superList))
                foreach (var t in superList) Chart[attackType][t] = 2.0;

            if (NotEffective.TryGetValue(attackType, out var notList))
                foreach (var t in notList) Chart[attackType][t] = 0.5;

            if (NoEffect.TryGetValue(attackType, out var noList))
                foreach (var t in noList) Chart[attackType][t] = 0.0;
        }
    }

    public static double GetMultiplier(PokemonType attackType, PokemonType defendType)
    {
        return Chart[attackType][defendType];
    }
}
