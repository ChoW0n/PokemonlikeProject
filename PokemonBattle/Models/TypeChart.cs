namespace PokemonBattle.Models;

//속성 상성표: 공격 타입 -> 방어 타입 -> 배율
public static class TypeChart
{
    public static Dictionary<PokemonType, Dictionary<PokemonType, double>> Chart =
        new Dictionary<PokemonType, Dictionary<PokemonType, double>>();

    static TypeChart() //정적 생성자: 프로그램 시작 시 한 번만 상성표 초기화
    {
        Chart[PokemonType.Fire] = new Dictionary<PokemonType, double>();
        Chart[PokemonType.Fire][PokemonType.Fire] = 0.5;
        Chart[PokemonType.Fire][PokemonType.Water] = 0.5;
        Chart[PokemonType.Fire][PokemonType.Grass] = 2.0;

        Chart[PokemonType.Water] = new Dictionary<PokemonType, double>();
        Chart[PokemonType.Water][PokemonType.Fire] = 2.0;
        Chart[PokemonType.Water][PokemonType.Water] = 0.5;
        Chart[PokemonType.Water][PokemonType.Grass] = 0.5;

        Chart[PokemonType.Grass] = new Dictionary<PokemonType, double>();
        Chart[PokemonType.Grass][PokemonType.Fire] = 0.5;
        Chart[PokemonType.Grass][PokemonType.Water] = 2.0;
        Chart[PokemonType.Grass][PokemonType.Grass] = 0.5;
    }

    public static double GetMultiplier(PokemonType attackType, PokemonType defendType) //상성 배율 조회
    {
        return Chart[attackType][defendType];
    }
}