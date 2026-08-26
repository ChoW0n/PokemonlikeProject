namespace PokemonBattle.Models;

//게임 전체에서 공유하는 포켓몬 도감 (새 포켓몬은 여기 한 줄만 추가하면 됨)
public static class PokemonDatabase
{
    public static Dictionary<int, PokemonData> All = new Dictionary<int, PokemonData>();

    static PokemonDatabase() //정적 생성자: 프로그램 시작 시 도감 등록
    {
        All[1] = new PokemonData("이상해씨", PokemonType.Grass, 45, 49, 49);
        All[4] = new PokemonData("파이리", PokemonType.Fire, 39, 52, 43);
        All[7] = new PokemonData("꼬부기", PokemonType.Water, 44, 48, 65);
    }
}