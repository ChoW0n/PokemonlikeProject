namespace PokemonBattle.Models;

//포켓몬 도감용 고정 데이터 (레벨업해도 안 변하는 기본 스펙)
public class PokemonData
{
    public string Name;
    public PokemonType Type;
    public int BaseHp;
    public int BaseAtk;
    public int BaseDef;

    public PokemonData(string name, PokemonType type, int hp, int atk, int def) //이름, 속성, 기본 스탯 초기화
    {
        Name = name;
        Type = type;
        BaseHp = hp;
        BaseAtk = atk;
        BaseDef = def;
    }
}