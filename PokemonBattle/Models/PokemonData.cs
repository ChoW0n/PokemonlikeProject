namespace PokemonBattle.Models;

public class PokemonData
{
    public string Name;
    public string EnglishName; //커뮤니티 리소스 매칭용 (PokeAPI 원본 영문 slug)
    public PokemonType Type1;
    public PokemonType? Type2;
    public int BaseHp;
    public int BaseAtk;
    public int BaseDef;
    public int BaseSpAtk;
    public int BaseSpDef;
    public int BaseSpd;
    public string[] MoveNames;
    public string[] AbilityNames;
    public string ImageUrl;
    public string BackImageUrl;
    public int? EvolvesToId;
    public int EvolveLevel;

    public PokemonData(string name, string englishName, PokemonType type1, PokemonType? type2, int hp, int atk, int def, int spAtk, int spDef, int spd, string[] moveNames, string[] abilityNames, string imageUrl, string backImageUrl, int? evolvesToId, int evolveLevel)
    {
        Name = name;
        EnglishName = englishName;
        Type1 = type1;
        Type2 = type2;
        BaseHp = hp;
        BaseAtk = atk;
        BaseDef = def;
        BaseSpAtk = spAtk;
        BaseSpDef = spDef;
        BaseSpd = spd;
        MoveNames = moveNames;
        AbilityNames = abilityNames;
        ImageUrl = imageUrl;
        BackImageUrl = backImageUrl;
        EvolvesToId = evolvesToId;
        EvolveLevel = evolveLevel;
    }

    public string TypeDisplay => Type2 == null ? Type1.ToString() : $"{Type1}/{Type2}";

    //실제 화면에 쓸 스프라이트: 커뮤니티 오버라이드가 있으면 그걸, 없으면 기본 자동생성 스프라이트
    public string EffectiveImageUrl => CommunitySpriteOverrides.GetFrontUrl(EnglishName) ?? ImageUrl;
    public string EffectiveBackImageUrl => CommunitySpriteOverrides.GetBackUrl(EnglishName) ?? BackImageUrl;
}
