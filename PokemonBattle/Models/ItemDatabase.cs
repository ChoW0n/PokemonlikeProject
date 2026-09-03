namespace PokemonBattle.Models;

public class Item
{
    public string Name;
    public string Description;
    public string? IconUrl;

    public Item(string name, string description, string? iconUrl = null)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
    }
}

public static class ItemDatabase
{
    private const string Base = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/";

    public static List<Item> GeneralItems = new List<Item>
    {
        new Item("없음", "도구를 지니지 않음"),
        new Item("먹다남은음식", "매턴 종료 시 최대 HP의 1/16을 회복한다.", Base + "leftovers.png"),
        new Item("구애스카프", "속도가 1.5배 상승하지만 처음 낸 기술만 계속 쓸 수 있다.", Base + "choice-scarf.png"),
        new Item("구애머리띠", "물리 기술 위력이 1.5배 상승하지만 처음 낸 기술만 계속 쓸 수 있다.", Base + "choice-band.png"),
        new Item("구애안경", "특수 기술 위력이 1.5배 상승하지만 처음 낸 기술만 계속 쓸 수 있다.", Base + "choice-specs.png"),
        new Item("생명의구슬", "모든 기술의 위력이 1.3배 상승하지만, 공격기를 쓸 때마다 최대 HP의 10%만큼 반동 데미지를 입는다.", Base + "life-orb.png"),
        new Item("오랭열매", "HP가 절반 이하가 되면 최대 HP의 10을 회복한다.", Base + "oran-berry.png"),
        new Item("자뭉열매", "HP가 절반 이하가 되면 최대 HP의 1/4을 회복한다.", Base + "sitrus-berry.png"),
        new Item("무화열매", "HP가 1/4 이하가 되면 최대 HP의 1/3을 회복한다. 먹보는 절반 이하에서 먹는다.", Base + "figy-berry.png"),
        new Item("리샘열매", "상태 이상이 되면 상태 이상을 회복한다.", Base + "lum-berry.png"),
        new Item("달콤한꿀", "포켓몬을 유인하는 달콤한 꿀이다.", Base + "honey.png"),
        new Item("기합의띠", "풀피 상태에서 한 방에 쓰러질 위기에 처하면 HP 1을 남기고 버틴다.", Base + "focus-sash.png"),
        new Item("기합의머리띠", "HP와 무관하게 일정 확률로 한 방에 쓰러질 위기에서 버틴다.", Base + "focus-band.png"),
        new Item("검은안경", "악 타입 기술의 위력이 1.2배 상승한다.", Base + "black-glasses.png"),
        new Item("신비의물방울", "물 타입 기술의 위력이 1.2배 상승한다.", Base + "mystic-water.png"),
        new Item("부드러운모래", "땅 타입 기술의 위력이 1.2배 상승한다.", Base + "soft-sand.png"),
        new Item("용의이빨", "드래곤 타입 기술의 위력이 1.2배 상승한다.", Base + "dragon-fang.png"),
        new Item("실크스카프", "노말 타입 기술의 위력이 1.2배 상승한다.", Base + "silk-scarf.png"),
        new Item("기적의씨", "풀 타입 기술의 위력이 1.2배 상승한다.", Base + "miracle-seed.png"),
        new Item("예리한부리", "비행 타입 기술의 위력이 1.2배 상승한다.", Base + "sharp-beak.png"),
        new Item("자석", "전기 타입 기술의 위력이 1.2배 상승한다.", Base + "magnet.png"),
        new Item("힘의머리띠", "물리 기술의 위력이 10% 상승한다.", Base + "muscle-band.png"),
        new Item("현명한안경", "특수 기술의 위력이 10% 상승한다.", Base + "wise-glasses.png"),
        new Item("달인의띠", "효과가 굉장한 기술의 위력이 20% 상승한다.", Base + "expert-belt.png"),
        new Item("울퉁불퉁멧", "접촉한 상대에게 최대 HP의 1/6만큼 데미지를 준다.", Base + "rocky-helmet.png"),
        new Item("조개껍질방울", "공격으로 준 데미지의 1/8만큼 HP를 회복한다.", Base + "shell-bell.png"),
        new Item("검은진흙", "독 타입은 매턴 HP를 회복하고, 그 외에는 데미지를 입는다.", Base + "black-sludge.png"),
        new Item("맹독구슬", "턴 종료 시 소지자를 맹독 상태로 만든다.", Base + "toxic-orb.png"),
        new Item("화염구슬", "턴 종료 시 소지자를 화상 상태로 만든다.", Base + "flame-orb.png"),
        new Item("돌격조끼", "특수방어가 1.5배가 되지만 변화 기술을 사용할 수 없다.", Base + "assault-vest.png"),
        new Item("진화의휘석", "진화할 수 있는 포켓몬의 방어와 특수방어가 1.5배가 된다.", Base + "eviolite.png"),
        new Item("약점보험", "효과가 굉장한 기술을 받으면 공격과 특수공격이 크게 상승한다.", Base + "weakness-policy.png"),
        new Item("하얀허브", "능력이 떨어질 때 모든 능력 저하를 한 번 회복한다.", Base + "white-herb.png"),
        new Item("풍선", "지면 타입 기술을 무효화하지만 데미지를 받으면 사라진다.", Base + "air-balloon.png"),
        new Item("보호패드", "접촉 기술의 접촉 효과를 받지 않는다.", Base + "protective-pads.png"),
        new Item("은밀망토", "기술의 추가 효과를 받지 않는다."),
        new Item("클리어참", "상대의 기술과 특성으로 능력이 떨어지지 않는다."),
        new Item("빛의점토", "리플렉터와 빛의장막의 지속 시간이 8턴이 된다.", Base + "light-clay.png"),
        new Item("예리한손톱", "급소에 맞을 확률이 상승한다.", Base + "razor-claw.png"),
        new Item("광각렌즈", "기술의 명중률이 10% 상승한다.", Base + "wide-lens.png"),
        new Item("반짝가루", "상대 기술의 명중률이 10% 낮아진다.", Base + "bright-powder.png"),
        new Item("방진고글", "모래바람 등 날씨 데미지와 가루 계열 기술에 면역이 된다.", Base + "safety-goggles.png"),
    };

    public static Dictionary<string, Item> ExclusiveItems = new Dictionary<string, Item>
    {
        ["피카츄"] = new Item("전기구슬", "피카츄 전용 도구: 공격과 특수공격이 2배가 된다.", Base + "light-ball.png"),
        ["텅구리"] = new Item("두꺼운뼈", "텅구리 전용 도구: 공격이 2배가 된다.", Base + "thick-club.png"),
        ["파오리"] = new Item("대파", "파오리 전용 도구: 급소에 맞을 확률이 크게 상승한다.", Base + "stick.png"),
        ["메타몽"] = new Item("메탈파우더", "메타몽 전용 도구: 방어와 특수방어가 1.5배가 된다.", Base + "metal-powder.png"),
        ["라티오스"] = new Item("이슬의구슬", "라티오스 전용 도구: 특수공격과 특수방어가 1.5배가 된다.", Base + "soul-dew.png"),
        ["라티아스"] = new Item("이슬의구슬", "라티아스 전용 도구: 특수공격과 특수방어가 1.5배가 된다.", Base + "soul-dew.png"),
    };

    public static List<Item> GetAvailableItems(string pokemonName)
    {
        var list = new List<Item>(GeneralItems);
        if (ExclusiveItems.TryGetValue(pokemonName, out var exclusive))
        {
            list.Add(exclusive);
        }
        return list;
    }
}
