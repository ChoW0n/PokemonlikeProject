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
        new Item("힘의머리띠", "물리 기술의 위력이 10% 상승한다. (효과 구현 예정)", Base + "muscle-band.png"),
        new Item("방진고글", "모래바람 등 날씨 데미지와 가루 계열 기술에 면역이 된다. (효과 구현 예정)", Base + "safety-goggles.png"),
    };

    public static Dictionary<string, Item> ExclusiveItems = new Dictionary<string, Item>
    {
        ["피카츄"] = new Item("전기구슬", "피카츄 전용 도구: 전기 타입 기술의 위력이 대폭 상승한다. (효과 구현 예정)", Base + "light-ball.png"),
        ["텅구리"] = new Item("두꺼운뼈", "텅구리 전용 도구: 물리 방어력이 2배가 된다. (효과 구현 예정)", Base + "thick-club.png"),
        ["파오리"] = new Item("대파", "파오리 전용 도구: 급소에 맞을 확률이 크게 상승한다. (효과 구현 예정)"),
        ["메타몽"] = new Item("메탈파우더", "메타몽 전용 도구: 회피율이 상승한다. (효과 구현 예정)", Base + "metal-powder.png"),
        ["라티오스"] = new Item("이슬의구슬", "라티오스 전용 도구: 특수공격과 특수방어가 상승한다. (효과 구현 예정)", Base + "soul-dew.png"),
        ["라티아스"] = new Item("이슬의구슬", "라티아스 전용 도구: 특수공격과 특수방어가 상승한다. (효과 구현 예정)", Base + "soul-dew.png"),
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
