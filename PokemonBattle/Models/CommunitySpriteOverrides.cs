namespace PokemonBattle.Models;

//커뮤니티(Ghasty001/Animated_sprites_by_Ghasty001, MIT 라이선스 아님·크레딧 표기 조건 자유사용)가
//공유한 고품질 애니메이션 스프라이트 오버라이드 목록.
//새 포켓몬을 추가하고 싶으면 DataGen을 재실행할 필요 없이 이 목록에 영문 이름만 한 줄 추가하면 됨.
//크레딧: 원작자 Ghasty001 및 원본 스프라이트 제작자들 (저장소 README 참고)
public static class CommunitySpriteOverrides
{
    private const string Base = "https://raw.githubusercontent.com/Ghasty001/Animated_sprites_by_Ghasty001/main/";

    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "braviary",   //#628
        "trevenant",  //#709
        "heliolisk",  //#695
        //향후 도감이 넓어지거나 저장소가 업데이트되면 여기에 영문 이름만 추가
    };

    public static string? GetFrontUrl(string englishName) =>
        Supported.Contains(englishName) ? $"{Base}FRONT/{englishName.ToUpperInvariant()}.gif" : null;

    public static string? GetBackUrl(string englishName) =>
        Supported.Contains(englishName) ? $"{Base}BACK/{englishName.ToUpperInvariant()}.gif" : null;
}
