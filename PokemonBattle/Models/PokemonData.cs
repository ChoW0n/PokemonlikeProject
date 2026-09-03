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
    // PokeAPI 기준으로 이 종이 실제 습득할 수 있는 기술 전체.
    // MoveNames에는 현재 게임에서 구현된 기술만 들어갈 수 있으므로 강탈 검증은 이 목록을 사용한다.
    public string[] LearnableMoveNames;
    // 모든 세대의 PokeAPI 습득 방법이 machine뿐인, 현재 구현된 기술 목록.
    public string[] MachineOnlyMoveNames;
    public string[] AbilityNames;
    public string ImageUrl;
    public string BackImageUrl;
    public int? EvolvesToId;
    public int EvolveLevel;
    public int HeightDecimeters { get; internal set; } = 10;
    public double HeightMeters => HeightDecimeters / 10.0;

    public PokemonData(string name, string englishName, PokemonType type1, PokemonType? type2, int hp, int atk, int def, int spAtk, int spDef, int spd, string[] moveNames, string[] abilityNames, string imageUrl, string backImageUrl, int? evolvesToId, int evolveLevel, string[]? learnableMoveNames = null, string[]? machineOnlyMoveNames = null)
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
        LearnableMoveNames = learnableMoveNames ?? moveNames;
        MachineOnlyMoveNames = machineOnlyMoveNames ?? Array.Empty<string>();
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
