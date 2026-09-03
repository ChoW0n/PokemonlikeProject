namespace PokemonBattle.Models;

public static class MoveDatabase
{
    // MoveRuleMetadata owns runtime-only move behavior and is intentionally not generated.
    public static Dictionary<string, Move> All = new Dictionary<string, Move>();

    static MoveDatabase()
    {
        All["vine-whip"] = new Move("덩굴채찍", 45, PokemonType.Grass, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "채찍처럼 휘어지는 가늘고 긴 덩굴로 상대를 힘껏 쳐서 공격한다.", 0, 0, 1, 1);
        All["tackle"] = new Move("몸통박치기", 40, PokemonType.Normal, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 향해서 몸 전체를 부딪쳐가며 공격한다.", 0, 0, 1, 1);
        All["take-down"] = new Move("돌진", 90, PokemonType.Normal, 20, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "굉장한 기세로 상대에게 부딪쳐 공격한다. 자신도 조금 데미지를 입는다.", 0, -25, 1, 1);
        All["double-edge"] = new Move("이판사판태클", 120, PokemonType.Normal, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "목숨을 걸고 상대에게 돌진하여 공격을 한다. 자신도 상당한 데미지를 입는다.", 0, -33, 1, 1);
        All["growl"] = new Move("울음소리", 0, PokemonType.Normal, 40, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "귀여운 울음소리를 들려주고 관심을 끌어 방심한 사이에 상대의 공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["leech-seed"] = new Move("씨뿌리기", 0, PokemonType.Grass, 10, 90, false, 0, true, false, "leech-seed", 100, 0, new List<StatChangeEntry>(), 0, "씨가 뿌려진 상대의 HP를 매 턴 조금씩 흡수하여 자신의 HP를 회복한다.", 0, 0, 1, 1);
        All["growth"] = new Move("성장", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = true } }, 100, "몸을 일시에 크게 성장시켜 공격과 특수공격을 올린다.", 0, 0, 1, 1);
        All["razor-leaf"] = new Move("잎날가르기", 55, PokemonType.Grass, 25, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "잎사귀를 날려 상대를 베어 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["solar-beam"] = new Move("솔라빔", 120, PokemonType.Grass, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 빛을 가득 모아 2턴째에 빛의 다발을 발사하여 공격한다.", 0, 0, 1, 1);
        All["poison-powder"] = new Move("독가루", 0, PokemonType.Poison, 35, 75, false, 0, true, false, "poison", 100, 0, new List<StatChangeEntry>(), 0, "독이 있는 가루를 많이 흩뿌려서 상대를 독 상태로 만든다.", 0, 0, 1, 1);
        All["sleep-powder"] = new Move("수면가루", 0, PokemonType.Grass, 15, 75, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "잠이 오는 가루를 많이 흩뿌려서 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["sweet-scent"] = new Move("달콤한향기", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "향기로 상대의 회피율을 크게 떨어뜨린다. 풀밭 등에서 쓰면 포켓몬이 다가온다.", 0, 0, 1, 1);
        All["synthesis"] = new Move("광합성", 0, PokemonType.Grass, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 HP를 회복한다. 날씨에 따라 회복량이 변한다.", 50, 0, 1, 1);
        All["seed-bomb"] = new Move("씨폭탄", 80, PokemonType.Grass, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단한 껍질을 가지고 있는 큰 씨앗을 힘껏 내던져 상대를 공격한다.", 0, 0, 1, 1);
        All["power-whip"] = new Move("파워휩", 120, PokemonType.Grass, 10, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "덩굴이나 촉수를 세차게 흔들어 상대를 힘껏 쳐서 공격한다.", 0, 0, 1, 1);
        All["petal-dance"] = new Move("꽃잎댄스", 120, PokemonType.Grass, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "2-3턴 동안 꽃을 흩뿌려서 상대를 공격한다. 흩뿌린 뒤에는 혼란에 빠진다.", 0, 0, 1, 1);
        All["amnesia"] = new Move("망각술", 0, PokemonType.Psychic, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = 2, TargetsSelf = true } }, 100, "머리를 비워서 순간적으로 무언가를 잊어버림으로써 자신의 특수방어를 크게 올린다.", 0, 0, 1, 1);
        All["petal-blizzard"] = new Move("꽃보라", 90, PokemonType.Grass, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "세찬 꽃보라를 일으켜서 주위에 있는 포켓몬을 공격하여 데미지를 준다.", 0, 0, 1, 1);
        All["scratch"] = new Move("할퀴기", 40, PokemonType.Normal, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단하고 뾰족한 날카로운 손톱으로 상대를 할퀴어서 공격한다.", 0, 0, 1, 1);
        All["leer"] = new Move("째려보기", 0, PokemonType.Normal, 30, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 100, "날카로운 눈초리로 겁을 주어 상대의 방어를 떨어뜨린다.", 0, 0, 1, 1);
        All["ember"] = new Move("불꽃세례", 40, PokemonType.Fire, 25, 100, false, 0, false, true, "burn", 10, 0, new List<StatChangeEntry>(), 0, "작은 불꽃을 상대에게 발사하여 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["flamethrower"] = new Move("화염방사", 90, PokemonType.Fire, 15, 100, false, 0, false, true, "burn", 10, 0, new List<StatChangeEntry>(), 0, "세찬 불꽃을 상대에게 발사하여 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["fire-spin"] = new Move("회오리불꽃", 35, PokemonType.Fire, 15, 85, false, 0, false, true, "trap", 100, 0, new List<StatChangeEntry>(), 0, "세차게 소용돌이치는 불꽃 속에 4-5턴 동안 상대를 가두어 공격한다.", 0, 0, 1, 1);
        All["rage"] = new Move("분노", 20, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "기술을 썼을 때 공격을 받으면 분노의 힘으로 공격이 올라간다.", 0, 0, 1, 1);
        All["smokescreen"] = new Move("연막", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new() { Stat = "accuracy", Change = -1, TargetsSelf = false } }, 100, "연기나 먹물을 내뿜어 상대의 명중률을 떨어뜨린다.", 0, 0, 1, 1);
        All["fury-swipes"] = new Move("마구할퀴기", 18, PokemonType.Normal, 15, 80, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "손톱이나 낫 등으로 상대를 할퀴어서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["slash"] = new Move("베어가르기", 70, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "발톱이나 낫 등으로 상대를 베어 갈라서 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["scary-face"] = new Move("겁나는얼굴", 0, PokemonType.Normal, 10, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -2, TargetsSelf = false } }, 100, "무서운 얼굴로 노려보고 겁주어 상대의 스피드를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["dragon-breath"] = new Move("용의숨결", 60, PokemonType.Dragon, 20, 100, false, 0, false, true, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "굉장한 숨결을 상대에게 내뿜어 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["metal-claw"] = new Move("메탈클로", 50, PokemonType.Steel, 35, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false } }, 10, "강철의 발톱으로 상대를 베어 갈라 공격한다. 자신의 공격이 올라갈 때도 있다.", 0, 0, 1, 1);
        All["flare-blitz"] = new Move("플레어드라이브", 120, PokemonType.Fire, 15, 100, false, 0, false, false, "burn", 10, 0, new List<StatChangeEntry>(), 0, "불꽃을 두르고 돌진한다. 자신도 상당한 데미지를 입는다. 화상 상태로 만들 때가 있다.", 0, -33, 1, 1);
        All["fire-fang"] = new Move("불꽃엄니", 65, PokemonType.Fire, 15, 95, false, 0, false, false, "burn", 10, 10, new List<StatChangeEntry>(), 0, "불꽃을 두른 이빨로 문다. 상대를 풀죽게 하거나 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["flame-burst"] = new Move("불꽃튀기기", 70, PokemonType.Fire, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "맞으면 튀는 불꽃으로 상대를 공격한다. 튕긴 불꽃은 옆의 상대에게도 쏟아진다.", 0, 0, 1, 1);
        All["inferno"] = new Move("연옥", 100, PokemonType.Fire, 5, 50, false, 0, false, true, "burn", 100, 0, new List<StatChangeEntry>(), 0, "격렬한 불꽃으로 상대를 둘러싸 공격한다. 화상 상태로 만든다.", 0, 0, 1, 1);
        All["wing-attack"] = new Move("날개치기", 60, PokemonType.Flying, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "크게 펼친 훌륭한 날개를 상대에게 부딪쳐서 공격한다.", 0, 0, 1, 1);
        All["crunch"] = new Move("깨물어부수기", 80, PokemonType.Dark, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 20, "날카로운 이빨로 상대를 깨물어 부숴서 공격한다. 상대의 방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["heat-wave"] = new Move("열풍", 95, PokemonType.Fire, 10, 90, false, 0, false, true, "burn", 10, 0, new List<StatChangeEntry>(), 0, "뜨거운 숨결을 상대에게 내뿜어 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["dragon-claw"] = new Move("드래곤클로", 80, PokemonType.Dragon, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카롭고 뾰족한 거대한 발톱으로 상대를 베어 갈라서 공격한다.", 0, 0, 1, 1);
        All["air-slash"] = new Move("에어슬래시", 75, PokemonType.Flying, 15, 95, false, 0, false, true, "none", 0, 30, new List<StatChangeEntry>(), 0, "하늘까지 베어 가르는 공기의 칼날로 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["shadow-claw"] = new Move("섀도클로", 70, PokemonType.Ghost, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "그림자로 만든 날카로운 발톱으로 상대를 베어 가른다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["headbutt"] = new Move("박치기", 70, PokemonType.Normal, 15, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "머리를 내밀어 곧장 돌진하여 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["tail-whip"] = new Move("꼬리흔들기", 0, PokemonType.Normal, 30, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 100, "꼬리를 좌우로 귀엽게 흔들어 방심을 유도한다. 상대의 방어를 떨어뜨린다.", 0, 0, 1, 1);
        All["bite"] = new Move("물기", 60, PokemonType.Dark, 25, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "날카롭고 뾰족한 이빨로 물어서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["water-gun"] = new Move("물대포", 40, PokemonType.Water, 25, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "물을 기세 좋게 상대에게 발사하여 공격한다.", 0, 0, 1, 1);
        All["hydro-pump"] = new Move("하이드로펌프", 110, PokemonType.Water, 5, 80, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "대량의 물을 세찬 기세로 상대에게 발사하여 공격한다.", 0, 0, 1, 1);
        All["bubble-beam"] = new Move("거품광선", 65, PokemonType.Water, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 10, "거품을 기세 좋게 상대에게 발사하여 공격한다. 스피드를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["withdraw"] = new Move("껍질에숨기", 0, PokemonType.Water, 40, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true } }, 100, "껍질에 숨어 몸을 보호하여 자신의 방어를 올린다.", 0, 0, 1, 1);
        All["skull-bash"] = new Move("로켓박치기", 130, PokemonType.Normal, 10, 100, false, 0, false, false, "none", 100, 0, new List<StatChangeEntry>(), 0, "1턴째에 머리를 움츠려 방어를 올린다. 2턴째에 상대를 공격한다.", 0, 0, 1, 1);
        All["bubble"] = new Move("거품", 40, PokemonType.Water, 30, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 10, "매우 많은 거품을 상대에게 내뿜어 공격한다. 상대의 스피드를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["protect"] = new Move("방어", 0, PokemonType.Normal, 10, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "상대의 공격을 전혀 받지 않는다. 연속으로 쓰면 실패하기 쉽다.", 0, 0, 1, 1);
        All["rapid-spin"] = new Move("고속스핀", 50, PokemonType.Normal, 40, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = false } }, 100, "회전해서 상대를 공격한다. 조이기, 김밥말이, 씨뿌리기, 압정뿌리기 등도 날려버린다.", 0, 0, 1, 1);
        All["iron-defense"] = new Move("철벽", 0, PokemonType.Steel, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 2, TargetsSelf = true } }, 100, "피부를 쇠처럼 단단하게 함으로써 자신의 방어를 크게 올린다.", 0, 0, 1, 1);
        All["water-pulse"] = new Move("물의파동", 60, PokemonType.Water, 20, 100, false, 0, false, true, "confusion", 20, 0, new List<StatChangeEntry>(), 0, "물의 진동을 상대에게 가하여 공격한다. 상대를 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["aqua-tail"] = new Move("아쿠아테일", 90, PokemonType.Water, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "세차게 날뛰는 거친 파도와 같이 큰 꼬리를 흔들어서 상대를 공격한다.", 0, 0, 1, 1);
        All["shell-smash"] = new Move("껍질깨기", 0, PokemonType.Normal, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = true }, new StatChangeEntry { Stat = "attack", Change = 2, TargetsSelf = true }, new StatChangeEntry { Stat = "special-attack", Change = 2, TargetsSelf = true }, new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "껍질을 깨서 자신의 방어와 특수방어를 떨어뜨리지만 공격과 특수공격, 스피드를 크게 올린다.", 0, 0, 1, 1);
        All["wave-crash"] = new Move("웨이브태클", 120, PokemonType.Water, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "파도와 함께 상대에게 돌진해 공격한다. 자신도 반동 데미지를 입는다.", 0, 0, 1, 1);
        All["fake-out"] = new Move("속이기", 40, PokemonType.Normal, 10, 100, false, 3, false, false, "none", 0, 100, new List<StatChangeEntry>(), 0, "선제공격으로 상대를 풀죽게 한다. 배틀에 나가서 바로 쓰지 않으면 성공할 수 없다.", 0, 0, 1, 1);
        All["flash-cannon"] = new Move("러스터캐논", 80, PokemonType.Steel, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "몸의 빛을 한곳에 모아서 힘을 쏜다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["aqua-jet"] = new Move("아쿠아제트", 40, PokemonType.Water, 20, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "눈에 보이지 않는 굉장한 속도로 상대에게 돌진한다. 반드시 선제공격할 수 있다.", 0, 0, 1, 1);
        All["string-shot"] = new Move("실뿜기", 0, PokemonType.Bug, 40, 95, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -2, TargetsSelf = false } }, 100, "입에서 뿜어낸 실을 휘감아서 상대의 스피드를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["bug-bite"] = new Move("벌레먹기", 60, PokemonType.Bug, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "물어서 공격한다. 상대가 나무열매를 지니고 있을 때 먹어서 나무열매의 효과를 받을 수 있다.", 0, 0, 1, 1);
        All["harden"] = new Move("단단해지기", 0, PokemonType.Normal, 30, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true } }, 100, "전신에 힘을 담아 몸을 단단하게 해서 자신의 방어를 올린다.", 0, 0, 1, 1);
        All["gust"] = new Move("바람일으키기", 40, PokemonType.Flying, 35, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "날개로 일으킨 격한 바람을 상대에게 부딪쳐서 공격한다.", 0, 0, 1, 1);
        All["supersonic"] = new Move("초음파", 0, PokemonType.Normal, 20, 55, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "특수한 음파를 몸에서 발산하여 상대를 혼란시킨다.", 0, 0, 1, 1);
        All["psybeam"] = new Move("환상빔", 65, PokemonType.Psychic, 20, 100, false, 0, false, true, "confusion", 10, 0, new List<StatChangeEntry>(), 0, "이상한 광선을 상대에게 발사하여 공격한다. 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["stun-spore"] = new Move("저리가루", 0, PokemonType.Grass, 30, 75, false, 0, true, false, "paralysis", 100, 0, new List<StatChangeEntry>(), 0, "저리 가루를 많이 흩뿌려서 상대를 마비 상태로 만든다.", 0, 0, 1, 1);
        All["confusion"] = new Move("염동력", 50, PokemonType.Psychic, 25, 100, false, 0, false, true, "confusion", 10, 0, new List<StatChangeEntry>(), 0, "약한 염동력을 상대에게 보내어 공격한다. 상대를 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["silver-wind"] = new Move("은빛바람", 60, PokemonType.Bug, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = false } }, 10, "바람에 날개 가루를 날려서 상대를 공격한다. 자신의 모든 능력이 올라갈 때가 있다.", 0, 0, 1, 1);
        All["bug-buzz"] = new Move("벌레의야단법석", 90, PokemonType.Bug, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "날개의 진동으로 음파를 일으켜서 공격한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["captivate"] = new Move("유혹", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "수컷은 암컷을 암컷은 수컷을 유혹하여 상대의 특수공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["quiver-dance"] = new Move("나비춤", 0, PokemonType.Bug, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = true } }, 100, "신비롭고 아름다운 춤을 경쾌하게 춘다. 자신의 특수공격과 특수방어와 스피드를 올린다.", 0, 0, 1, 1);
        All["poison-sting"] = new Move("독침", 15, PokemonType.Poison, 35, 100, false, 0, false, false, "poison", 30, 0, new List<StatChangeEntry>(), 0, "독이 있는 침을 상대에게 꿰찔러서 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["fury-attack"] = new Move("마구찌르기", 15, PokemonType.Normal, 20, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "뿔이나 부리로 상대를 찔러서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["twineedle"] = new Move("더블니들", 25, PokemonType.Bug, 20, 100, false, 0, false, false, "poison", 20, 0, new List<StatChangeEntry>(), 0, "2개의 침을 상대에게 꿰찔러 2회 연속으로 데미지를 준다. 독 상태로 만들 때가 있다.", 0, 0, 2, 2);
        All["pin-missile"] = new Move("바늘미사일", 25, PokemonType.Bug, 20, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카로운 침을 상대에게 발사해서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["peck"] = new Move("쪼기", 35, PokemonType.Flying, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카롭고 뾰족한 부리나 뿔로 상대를 쪼아서 공격한다.", 0, 0, 1, 1);
        All["agility"] = new Move("고속이동", 0, PokemonType.Psychic, 30, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "힘을 빼고 몸을 가볍게 해서 고속으로 움직인다. 자신의 스피드를 크게 올린다.", 0, 0, 1, 1);
        All["outrage"] = new Move("역린", 120, PokemonType.Dragon, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "2-3턴 동안 마구 난동 부려서 공격한다. 난동 부린 뒤에는 혼란에 빠진다.", 0, 0, 1, 1);
        All["fury-cutter"] = new Move("연속자르기", 40, PokemonType.Bug, 20, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "낫이나 발톱 등으로 상대를 베어 공격한다. 연속으로 맞히면 위력이 올라간다.", 0, 0, 1, 1);
        All["pursuit"] = new Move("따라가때리기", 40, PokemonType.Dark, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대 포켓몬이 교체될 때 기술을 쓰면 2배의 위력으로 공격할 수 있다.", 0, 0, 1, 1);
        All["assurance"] = new Move("승부굳히기", 60, PokemonType.Dark, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "그 턴에 상대가 이미 데미지를 입었다면 기술의 위력은 2배가 된다.", 0, 0, 1, 1);
        All["poison-jab"] = new Move("독찌르기", 80, PokemonType.Poison, 20, 100, false, 0, false, false, "poison", 30, 0, new List<StatChangeEntry>(), 0, "독에 물든 촉수나 팔로 상대를 꿰찌른다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["venoshock"] = new Move("베놈쇼크", 65, PokemonType.Poison, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "특수한 독액을 끼얹는다. 독 상태의 상대에게는 위력이 2배가 된다.", 0, 0, 1, 1);
        All["fell-stinger"] = new Move("마지막일침", 50, PokemonType.Bug, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "이 기술을 사용하여 상대를 쓰러뜨리면 공격이 크게 오른다.", 0, 0, 1, 1);
        All["razor-wind"] = new Move("칼바람", 80, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "바람의 칼날을 만들어 2턴째에 상대를 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["sand-attack"] = new Move("모래뿌리기", 0, PokemonType.Ground, 15, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "accuracy", Change = -1, TargetsSelf = false } }, 100, "상대의 얼굴에 모래를 뿌려서 명중률을 떨어뜨린다.", 0, 0, 1, 1);
        All["quick-attack"] = new Move("전광석화", 40, PokemonType.Normal, 30, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "눈에 보이지 않는 굉장한 속도로 상대에게 돌진한다. 반드시 선제공격할 수 있다.", 0, 0, 1, 1);
        All["twister"] = new Move("회오리", 40, PokemonType.Dragon, 20, 100, false, 0, false, true, "none", 0, 20, new List<StatChangeEntry>(), 0, "회오리를 일으켜 상대를 끌어들여 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["feather-dance"] = new Move("깃털댄스", 0, PokemonType.Flying, 15, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -2, TargetsSelf = false } }, 100, "깃털을 흩뿌려 상대의 몸에 휘감는다. 상대의 공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["aerial-ace"] = new Move("제비반환", 60, PokemonType.Flying, 20, 100, true, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "재빠른 움직임으로 상대를 농락해 벤다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["roost"] = new Move("날개쉬기", 0, PokemonType.Flying, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "땅에 내려와 몸을 쉬게 한다. 최대 HP의 절반만큼 HP를 회복한다.", 50, 0, 1, 1);
        All["hurricane"] = new Move("폭풍", 110, PokemonType.Flying, 10, 70, false, 0, false, true, "confusion", 30, 0, new List<StatChangeEntry>(), 0, "강렬한 바람으로 상대를 둘러싸서 공격한다. 상대를 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["sky-attack"] = new Move("불새", 140, PokemonType.Flying, 5, 90, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "2턴째에 상대를 공격한다. 가끔 풀죽게 만든다. 급소에도 맞기 쉽다.", 0, 0, 1, 1);
        All["hyper-fang"] = new Move("필살앞니", 80, PokemonType.Normal, 15, 90, false, 0, false, false, "none", 0, 10, new List<StatChangeEntry>(), 0, "날카로운 앞니로 강하게 물어서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["sucker-punch"] = new Move("기습", 70, PokemonType.Dark, 5, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대보다 먼저 공격할 수 있다. 상대가 쓴 기술이 공격기술이 아니면 실패한다.", 0, 0, 1, 1);
        All["swords-dance"] = new Move("칼춤", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 2, TargetsSelf = true } }, 100, "싸움의 춤을 격렬하게 추며 기세를 높인다. 자신의 공격을 크게 올린다.", 0, 0, 1, 1);
        All["drill-peck"] = new Move("회전부리", 80, PokemonType.Flying, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "회전하면서 뾰족한 부리를 상대에게 꿰찔러 공격한다.", 0, 0, 1, 1);
        All["pluck"] = new Move("쪼아대기", 60, PokemonType.Flying, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "부리로 공격한다. 상대가 나무열매를 지니고 있을 때 먹어서 나무열매의 효과를 받을 수 있다.", 0, 0, 1, 1);
        All["drill-run"] = new Move("드릴라이너", 80, PokemonType.Ground, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "드릴처럼 몸을 회전시켜서 상대에게 몸통박치기한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["slam"] = new Move("힘껏치기", 80, PokemonType.Normal, 20, 75, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "긴 꼬리나 덩굴 등을 사용해 상대를 힘껏 쳐서 공격한다.", 0, 0, 1, 1);
        All["wrap"] = new Move("김밥말이", 15, PokemonType.Normal, 20, 90, false, 0, false, false, "trap", 100, 0, new List<StatChangeEntry>(), 0, "긴 몸이나 덩굴 등을 사용해 4-5턴 동안 상대를 휘감아 공격한다.", 0, 0, 1, 1);
        All["acid"] = new Move("용해액", 40, PokemonType.Poison, 30, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "강한 산을 상대에게 끼얹어 공격한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["toxic"] = new Move("맹독", 0, PokemonType.Poison, 10, 90, false, 0, true, false, "poison", 100, 0, new List<StatChangeEntry>(), 0, "상대를 맹독의 상태로 만든다. 턴이 진행될수록 독의 데미지가 증가한다.", 0, 0, 1, 1);
        All["screech"] = new Move("싫은소리", 0, PokemonType.Normal, 40, 85, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -2, TargetsSelf = false } }, 100, "그만 귀를 막아버리고 싶은 싫은 소리를 내어 상대의 방어를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["glare"] = new Move("뱀눈초리", 0, PokemonType.Normal, 30, 100, false, 0, true, false, "paralysis", 100, 0, new List<StatChangeEntry>(), 0, "배의 무늬로 겁을 주어 상대를 마비 상태로 만든다.", 0, 0, 1, 1);
        All["sludge-bomb"] = new Move("오물폭탄", 90, PokemonType.Poison, 10, 100, false, 0, false, true, "poison", 30, 0, new List<StatChangeEntry>(), 0, "더러운 오물을 상대에게 내던져서 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["stockpile"] = new Move("비축하기", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true } }, 100, "힘을 비축해서 자신의 방어와 특수방어를 올린다. 최대 3회까지 비축할 수 있다.", 0, 0, 1, 1);
        All["swallow"] = new Move("꿀꺽", 0, PokemonType.Normal, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "비축된 힘을 꿀꺽해서 자신의 HP를 회복한다. 비축된 만큼 회복한다.", 25, 0, 1, 1);
        All["mud-bomb"] = new Move("진흙폭탄", 65, PokemonType.Ground, 10, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 30, "단단한 진흙구슬을 상대에게 발사하여 공격한다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["gunk-shot"] = new Move("더스트슈트", 120, PokemonType.Poison, 5, 80, false, 0, false, false, "poison", 30, 0, new List<StatChangeEntry>(), 0, "더러운 쓰레기를 상대에게 부딪쳐서 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["coil"] = new Move("똬리틀기", 0, PokemonType.Poison, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true } }, 100, "똬리를 틀어서 집중한다. 자신의 공격과 방어와 명중률을 올린다.", 0, 0, 1, 1);
        All["acid-spray"] = new Move("애시드봄", 40, PokemonType.Poison, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -2, TargetsSelf = false } }, 100, "상대를 녹이는 액체를 토해내서 공격한다. 상대의 특수방어를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["belch"] = new Move("트림", 120, PokemonType.Poison, 10, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 향해 트림을 하여 데미지를 준다. 나무열매를 먹지 않으면 쓸 수 없다.", 0, 0, 1, 1);
        All["thunder-fang"] = new Move("번개엄니", 65, PokemonType.Electric, 15, 95, false, 0, false, false, "paralysis", 10, 10, new List<StatChangeEntry>(), 0, "전기를 모은 이빨로 문다. 상대를 풀죽게 하거나 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["ice-fang"] = new Move("얼음엄니", 65, PokemonType.Ice, 15, 95, false, 0, false, false, "freeze", 10, 10, new List<StatChangeEntry>(), 0, "냉기를 품은 이빨로 문다. 상대를 풀죽게 하거나 얼음 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["double-kick"] = new Move("두번차기", 30, PokemonType.Fighting, 30, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "2개의 다리로 상대를 걷어차서 공격한다. 2회 연속으로 데미지를 준다.", 0, 0, 2, 2);
        All["thunder-shock"] = new Move("전기쇼크", 40, PokemonType.Electric, 30, 100, false, 0, false, true, "paralysis", 10, 0, new List<StatChangeEntry>(), 0, "전기 자극을 상대에게 날려서 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["thunderbolt"] = new Move("10만볼트", 90, PokemonType.Electric, 15, 100, false, 0, false, true, "paralysis", 10, 0, new List<StatChangeEntry>(), 0, "강한 전격을 상대에게 날려서 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["thunder-wave"] = new Move("전기자석파", 0, PokemonType.Electric, 20, 90, false, 0, true, false, "paralysis", 100, 0, new List<StatChangeEntry>(), 0, "약한 전격을 날려서 상대를 마비 상태로 만든다.", 0, 0, 1, 1);
        All["thunder"] = new Move("번개", 110, PokemonType.Electric, 10, 70, false, 0, false, true, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "강한 번개를 상대에게 떨어뜨려 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["double-team"] = new Move("그림자분신", 0, PokemonType.Normal, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "재빠른 움직임으로 분신을 만들어 상대를 혼란시켜 회피율을 올린다.", 0, 0, 1, 1);
        All["swift"] = new Move("스피드스타", 60, PokemonType.Normal, 20, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "별 모양의 빛을 발사해서 상대를 공격한다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["sweet-kiss"] = new Move("천사의키스", 0, PokemonType.Fairy, 10, 75, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "천사처럼 귀엽게 키스하여 상대를 혼란시킨다.", 0, 0, 1, 1);
        All["charm"] = new Move("애교부리기", 0, PokemonType.Fairy, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -2, TargetsSelf = false } }, 100, "귀엽게 바라보고 방심을 유도하여 상대의 공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["spark"] = new Move("스파크", 65, PokemonType.Electric, 20, 100, false, 0, false, false, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "전기를 둘러 상대에게 돌진하여 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["iron-tail"] = new Move("아이언테일", 100, PokemonType.Steel, 15, 75, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 30, "단단한 꼬리로 상대를 힘껏 쳐서 공격한다. 상대의 방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["feint"] = new Move("페인트", 30, PokemonType.Normal, 10, 100, false, 2, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "방어나 판별을 하고 있는 상대에게 공격할 수 있다. 방어 효과를 해제시킨다.", 0, 0, 1, 1);
        All["nasty-plot"] = new Move("나쁜음모", 0, PokemonType.Dark, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 2, TargetsSelf = true } }, 100, "나쁜 일을 생각해서 머리를 활성화시킨다. 자신의 특수공격을 크게 올린다.", 0, 0, 1, 1);
        All["discharge"] = new Move("방전", 80, PokemonType.Electric, 15, 100, false, 0, false, true, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "눈부신 전격으로 자신의 주위에 있는 포켓몬을 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["wild-charge"] = new Move("와일드볼트", 90, PokemonType.Electric, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "전기를 두르고 상대에게 부딪쳐 공격한다. 자신도 조금 데미지를 입는다.", 0, -25, 1, 1);
        All["play-nice"] = new Move("친해지기", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "상대와 친해져서 싸울 마음을 잃게 하여 상대의 공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["nuzzle"] = new Move("볼부비부비", 20, PokemonType.Electric, 20, 100, false, 0, false, false, "paralysis", 100, 0, new List<StatChangeEntry>(), 0, "전기가 흐르는 볼을 비벼서 공격한다. 상대를 마비 상태로 만든다.", 0, 0, 1, 1);
        All["thunder-punch"] = new Move("번개펀치", 75, PokemonType.Electric, 15, 100, false, 0, false, false, "paralysis", 10, 0, new List<StatChangeEntry>(), 0, "전격을 담은 펀치로 상대를 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["earthquake"] = new Move("지진", 100, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "지진의 충격으로 자신의 주위에 있는 포켓몬을 공격한다.", 0, 0, 1, 1);
        All["dig"] = new Move("구멍파기", 80, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 파고들어 2턴째에 상대를 공격한다. 동굴에서 탈출할 수도 있다.", 0, 0, 1, 1);
        All["defense-curl"] = new Move("웅크리기", 0, PokemonType.Normal, 40, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true } }, 100, "몸을 둥글게 웅크려서 자신의 방어를 올린다.", 0, 0, 1, 1);
        All["rollout"] = new Move("구르기", 30, PokemonType.Rock, 20, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 구르기를 반복하여 공격한다. 기술이 맞을 때마다 위력이 올라간다.", 0, 0, 1, 1);
        All["sand-tomb"] = new Move("모래지옥", 35, PokemonType.Ground, 15, 85, false, 0, false, false, "trap", 100, 0, new List<StatChangeEntry>(), 0, "세차게 불어대는 모래바람 속에 4-5턴 동안 상대를 가두어 공격한다.", 0, 0, 1, 1);
        All["bulldoze"] = new Move("땅고르기", 60, PokemonType.Ground, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "땅을 힘껏 밟아 자신의 주위에 있는 포켓몬을 공격한다. 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["crush-claw"] = new Move("브레이크클로", 75, PokemonType.Normal, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 50, "단단하고 날카로운 손톱으로 베어 갈라서 공격한다. 상대의 방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["flatter"] = new Move("부추기기", 0, PokemonType.Dark, 15, 100, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false } }, 100, "상대를 부추겨서 혼란시킨다. 동시에 상대의 특수공격도 올라가 버린다.", 0, 0, 1, 1);
        All["poison-fang"] = new Move("맹독엄니", 50, PokemonType.Poison, 15, 100, false, 0, false, false, "poison", 50, 0, new List<StatChangeEntry>(), 0, "독이 있는 이빨로 상대를 물어서 공격한다. 맹독을 주입할 때가 있다.", 0, 0, 1, 1);
        All["earth-power"] = new Move("대지의힘", 90, PokemonType.Ground, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "상대의 발밑에 대지의 힘을 방출한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["body-slam"] = new Move("누르기", 85, PokemonType.Normal, 15, 100, false, 0, false, false, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "몸 전체로 상대를 덮쳐 눌러 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["superpower"] = new Move("엄청난힘", 120, PokemonType.Fighting, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = true }, new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = true } }, 100, "엄청난 힘을 발휘하여 상대를 공격한다. 자신의 공격과 방어가 떨어진다.", 0, 0, 1, 1);
        All["sludge-wave"] = new Move("오물웨이브", 95, PokemonType.Poison, 10, 100, false, 0, false, true, "poison", 10, 0, new List<StatChangeEntry>(), 0, "오물 파도로 자신의 주위에 있는 포켓몬을 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["chip-away"] = new Move("야금야금", 70, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "틈을 보며 착실하게 공격한다. 상대의 능력 변화에 관계없이 데미지를 준다.", 0, 0, 1, 1);
        All["horn-attack"] = new Move("뿔찌르기", 65, PokemonType.Normal, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카롭고 뾰족한 뿔로 상대를 공격한다.", 0, 0, 1, 1);
        All["thrash"] = new Move("난동부리기", 120, PokemonType.Normal, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "2-3턴 동안 마구 난동 부려서 상대를 공격한다. 난동 부린 뒤에는 혼란에 빠진다.", 0, 0, 1, 1);
        All["megahorn"] = new Move("메가혼", 120, PokemonType.Bug, 10, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단하고 훌륭한 뿔로 마음껏 상대를 꿰찔러서 공격한다.", 0, 0, 1, 1);
        All["pound"] = new Move("막치기", 40, PokemonType.Normal, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "긴 꼬리나 손 등을 사용하여 상대를 때려서 공격한다.", 0, 0, 1, 1);
        All["double-slap"] = new Move("연속뺨치기", 15, PokemonType.Normal, 10, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "연속 뺨치기로 상대를 때려서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["sing"] = new Move("노래하기", 0, PokemonType.Normal, 15, 55, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "기분 좋은 예쁜 노랫소리를 들려주고 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["psychic"] = new Move("사이코키네시스", 90, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "강한 염동력을 상대에게 보내어 공격한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["minimize"] = new Move("작아지기", 0, PokemonType.Normal, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "몸을 축소하여 작게 보임으로써 자신의 회피율을 크게 올린다.", 0, 0, 1, 1);
        All["moonlight"] = new Move("달빛", 0, PokemonType.Fairy, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 HP를 회복한다. 날씨에 따라 회복량이 변한다.", 50, 0, 1, 1);
        All["meteor-mash"] = new Move("코멧펀치", 90, PokemonType.Steel, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false } }, 20, "혜성과 같은 펀치를 날려서 상대를 공격한다. 자신의 공격이 올라갈 때가 있다.", 0, 0, 1, 1);
        All["cosmic-power"] = new Move("코스믹파워", 0, PokemonType.Psychic, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true } }, 100, "우주로부터 신비한 힘을 손에 넣음으로써 자신의 방어와 특수방어를 올린다.", 0, 0, 1, 1);
        All["calm-mind"] = new Move("명상", 0, PokemonType.Psychic, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true } }, 100, "조용히 정신을 통일하고 마음을 가라앉혀서 자신의 특수공격과 특수방어를 올린다.", 0, 0, 1, 1);
        All["wake-up-slap"] = new Move("잠깨움뺨치기", 70, PokemonType.Fighting, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "잠듦 상태의 상대에게 큰 데미지를 준다. 대신 상대는 잠에서 깬다.", 0, 0, 1, 1);
        All["stored-power"] = new Move("어시스트파워", 20, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "축적된 파워로 상대를 공격한다. 자신의 능력이 올라가 있는 만큼 위력이 오른다.", 0, 0, 1, 1);
        All["disarming-voice"] = new Move("차밍보이스", 40, PokemonType.Fairy, 15, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "매혹적인 울음소리를 내어 상대에게 정신적 데미지를 준다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["draining-kiss"] = new Move("드레인키스", 50, PokemonType.Fairy, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "키스로 상대로부터 HP를 흡수한다. 준 데미지의 반 이상 HP를 회복한다.", 0, 75, 1, 1);
        All["fairy-wind"] = new Move("요정의바람", 40, PokemonType.Fairy, 30, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "요정의 바람을 일으켜 상대에게 몰아쳐서 공격한다.", 0, 0, 1, 1);
        All["moonblast"] = new Move("문포스", 95, PokemonType.Fairy, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 30, "달의 파워를 빌려서 상대를 공격한다. 상대의 특수공격을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["baby-doll-eyes"] = new Move("초롱초롱눈동자", 0, PokemonType.Fairy, 30, 100, false, 1, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "초롱초롱한 눈동자로 상대를 바라보며 공격을 떨어뜨린다. 반드시 선제공격할 수 있다.", 0, 0, 1, 1);
        All["life-dew"] = new Move("생명의물방울", 0, PokemonType.Water, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "신비한 물을 흩뿌려서 자신과 배틀에 나와 있는 같은 편의 HP를 회복한다.", 25, 0, 1, 1);
        All["disable"] = new Move("사슬묶기", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "disable", 100, 0, new List<StatChangeEntry>(), 0, "상대의 움직임을 막아 바로 전에 쓴 기술을 4턴 동안 사용할 수 없게 만든다.", 0, 0, 1, 1);
        All["confuse-ray"] = new Move("이상한빛", 0, PokemonType.Ghost, 10, 100, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "이상한 빛을 상대에게 비춰 당황하게 한다. 상대를 혼란시킨다.", 0, 0, 1, 1);
        All["fire-blast"] = new Move("불대문자", 110, PokemonType.Fire, 5, 85, false, 0, false, true, "burn", 10, 0, new List<StatChangeEntry>(), 0, "큰 대자의 불꽃으로 상대를 불태운다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["flame-wheel"] = new Move("화염바퀴", 60, PokemonType.Fire, 25, 100, false, 0, false, false, "burn", 10, 0, new List<StatChangeEntry>(), 0, "불꽃을 둘러 상대에게 돌진하여 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["feint-attack"] = new Move("속여때리기", 60, PokemonType.Dark, 20, 100, true, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "슬금슬금 상대에게 다가가 방심한 틈을 타서 세게 때린다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["will-o-wisp"] = new Move("도깨비불", 0, PokemonType.Fire, 15, 85, false, 0, true, false, "burn", 100, 0, new List<StatChangeEntry>(), 0, "으스스하고 괴상한 불꽃을 쏘아 상대를 화상 상태로 만든다.", 0, 0, 1, 1);
        All["extrasensory"] = new Move("신통력", 80, PokemonType.Psychic, 20, 100, false, 0, false, true, "none", 0, 10, new List<StatChangeEntry>(), 0, "보이지 않는 이상한 힘을 보내어 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["payback"] = new Move("보복", 50, PokemonType.Dark, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "모아서 공격한다. 상대보다 뒤에 공격할 수 있으면 기술의 위력은 2배가 된다.", 0, 0, 1, 1);
        All["hex"] = new Move("병상첨병", 65, PokemonType.Ghost, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "엎친 데 덮친 격으로 공격한다. 상태 이상인 상대에게 큰 데미지를 준다.", 0, 0, 1, 1);
        All["incinerate"] = new Move("불태우기", 60, PokemonType.Fire, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "불꽃으로 상대를 공격한다. 상대가 나무열매 등을 지니고 있을 때 불태워서 쓸 수 없게 만든다.", 0, 0, 1, 1);
        All["hypnosis"] = new Move("최면술", 0, PokemonType.Psychic, 20, 60, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "졸음을 유도하는 암시를 걸어서 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["hyper-voice"] = new Move("하이퍼보이스", 90, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "시끄럽게 울려서 큰 진동을 상대에게 전달하여 공격한다.", 0, 0, 1, 1);
        All["covet"] = new Move("탐내다", 60, PokemonType.Normal, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "귀엽게 애교부리며 상대에게 다가가 지니고 있는 도구를 뺏는다.", 0, 0, 1, 1);
        All["round"] = new Move("돌림노래", 60, PokemonType.Normal, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "노래로 상대를 공격한다. 함께 돌림노래를 하면 계속해서 쓸 수 있고 위력도 올라간다.", 0, 0, 1, 1);
        All["echoed-voice"] = new Move("에코보이스", 40, PokemonType.Normal, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "울리는 목소리로 상대를 공격한다. 매 턴 누군가 기술을 계속해서 쓰면 위력이 올라간다.", 0, 0, 1, 1);
        All["play-rough"] = new Move("치근거리기", 90, PokemonType.Fairy, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 10, "상대에게 치근거리며 공격한다. 상대의 공격을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["absorb"] = new Move("흡수", 20, PokemonType.Grass, 25, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "양분을 흡수하여 공격한다. 상대에게 입힌 데미지의 절반에 해당하는 HP를 회복할 수 있다.", 0, 50, 1, 1);
        All["leech-life"] = new Move("흡혈", 80, PokemonType.Bug, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "피를 빨아서 상대를 공격한다. 준 데미지의 절반을 HP로 회복한다.", 0, 50, 1, 1);
        All["astonish"] = new Move("놀래키기", 30, PokemonType.Ghost, 15, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "큰 소리 등으로 불시에 놀래켜서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["air-cutter"] = new Move("에어커터", 60, PokemonType.Flying, 25, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카로운 바람으로 상대를 베어 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["cross-poison"] = new Move("크로스포이즌", 70, PokemonType.Poison, 20, 100, false, 0, false, false, "poison", 10, 0, new List<StatChangeEntry>(), 0, "독 칼날로 상대를 베어 가른다. 독 상태로 만들 때가 있고 급소에도 맞기 쉽다.", 0, 0, 1, 1);
        All["acrobatics"] = new Move("애크러뱃", 55, PokemonType.Flying, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "경쾌하게 상대를 공격한다. 자신이 도구를 지니고 있지 않을 때 큰 데미지를 준다.", 0, 0, 1, 1);
        All["mega-drain"] = new Move("메가드레인", 40, PokemonType.Grass, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "양분을 흡수하여 공격한다. 상대에게 입힌 데미지의 절반에 해당하는 HP를 회복할 수 있다.", 0, 50, 1, 1);
        All["giga-drain"] = new Move("기가드레인", 75, PokemonType.Grass, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "양분을 흡수하여 공격한다. 입힌 데미지의 절반에 해당하는 HP를 회복할 수 있다.", 0, 50, 1, 1);
        All["spore"] = new Move("버섯포자", 0, PokemonType.Grass, 15, 100, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "최면 효과가 있는 포자를 훌훌 흩뿌려서 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["x-scissor"] = new Move("시저크로스", 80, PokemonType.Bug, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "낫이나 발톱을 가위처럼 교차시키면서 상대를 베어 가른다.", 0, 0, 1, 1);
        All["energy-ball"] = new Move("에너지볼", 90, PokemonType.Grass, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "자연으로부터 모은 생명의 힘을 발사한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["foresight"] = new Move("꿰뚫어보기", 0, PokemonType.Normal, 40, 100, true, 0, true, false, "no-type-immunity", 100, 0, new List<StatChangeEntry>(), 0, "고스트타입에 효과가 없는 기술이나 회피율이 높은 상대라 할지라도 공격이 맞게 된다.", 0, 0, 1, 1);
        All["signal-beam"] = new Move("시그널빔", 75, PokemonType.Bug, 15, 100, false, 0, false, true, "confusion", 10, 0, new List<StatChangeEntry>(), 0, "이상한 빛을 발사해서 공격한다. 상대를 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["zen-headbutt"] = new Move("사념의박치기", 80, PokemonType.Psychic, 15, 90, false, 0, false, false, "none", 0, 20, new List<StatChangeEntry>(), 0, "사념의 힘을 이마에 모아서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["struggle-bug"] = new Move("벌레의저항", 50, PokemonType.Bug, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "저항해서 상대를 공격한다. 상대의 특수공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["mud-slap"] = new Move("진흙뿌리기", 20, PokemonType.Ground, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 100, "상대의 얼굴 등에 진흙을 내던져서 공격한다. 명중률을 떨어뜨린다.", 0, 0, 1, 1);
        All["tri-attack"] = new Move("트라이어택", 80, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 20, 0, new List<StatChangeEntry>(), 0, "3개의 광선으로 공격한다. 마비, 화상 또는 얼음 상태 중 어느 하나로 만들 때가 있다.", 0, 0, 1, 1);
        All["night-slash"] = new Move("깜짝베기", 70, PokemonType.Dark, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "순간적으로 틈을 노려 상대를 베어 버린다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["rototiller"] = new Move("일구기", 0, PokemonType.Ground, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false } }, 100, "땅을 일구어 초목이 자라기 쉽게 한다. 풀타입의 공격과 특수공격이 오른다.", 0, 0, 1, 1);
        All["pay-day"] = new Move("고양이돈받기", 40, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대의 몸에 돈을 세게 던져서 공격한다. 배틀 후에 돈을 받을 수 있다.", 0, 0, 1, 1);
        All["swagger"] = new Move("뽐내기", 0, PokemonType.Normal, 15, 85, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 2, TargetsSelf = false } }, 100, "상대를 화내게 해서 혼란시킨다. 분노로 상대의 공격은 크게 올라가 버린다.", 0, 0, 1, 1);
        All["power-gem"] = new Move("파워젬", 80, PokemonType.Rock, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "보석처럼 반짝이는 빛을 발사하여 상대를 공격한다.", 0, 0, 1, 1);
        All["surf"] = new Move("파도타기", 90, PokemonType.Water, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "큰 파도로 자신의 주위에 있는 포켓몬을 공격한다. 물 위도 헤엄쳐서 나아간다.", 0, 0, 1, 1);
        All["yawn"] = new Move("하품", 0, PokemonType.Normal, 10, 100, true, 0, true, false, "yawn", 100, 0, new List<StatChangeEntry>(), 0, "큰 하품으로 졸음을 유도한다. 다음 턴에 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["karate-chop"] = new Move("태권당수", 50, PokemonType.Fighting, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카로운 당수로 상대를 때려서 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["cross-chop"] = new Move("크로스촙", 100, PokemonType.Fighting, 5, 80, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "양손으로 당수를 상대에게 힘껏 쳐서 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["u-turn"] = new Move("유턴", 70, PokemonType.Bug, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "공격한 뒤 굉장한 스피드로 돌아와서 교대 포켓몬과 교체한다.", 0, 0, 1, 1);
        All["close-combat"] = new Move("인파이트", 120, PokemonType.Fighting, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = true } }, 100, "방어를 포기하고 상대 깊숙이 돌격한다. 자신의 방어와 특수방어가 떨어진다.", 0, 0, 1, 1);
        All["retaliate"] = new Move("원수갚기", 70, PokemonType.Normal, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "쓰러진 같은 편의 원수를 갚는다. 앞 턴에서 같은 편이 쓰러졌다면 위력이 올라간다.", 0, 0, 1, 1);
        All["stomping-tantrum"] = new Move("분함의발구르기", 75, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "분함을 발판삼아 공격한다. 앞 턴에서 기술이 빗나갔다면 위력이 배가 된다.", 0, 0, 1, 1);
        All["rage-fist"] = new Move("분노의주먹", 50, PokemonType.Ghost, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "분노를 주먹에 모아 공격한다. 맞은 횟수가 많을수록 위력이 올라간다.", 0, 0, 1, 1);
        All["odor-sleuth"] = new Move("냄새구별", 0, PokemonType.Normal, 40, 100, true, 0, true, false, "no-type-immunity", 100, 0, new List<StatChangeEntry>(), 0, "고스트타입에 효과가 없는 기술이나 회피율이 높은 상대라 할지라도 공격이 맞게 된다.", 0, 0, 1, 1);
        All["howl"] = new Move("멀리짖기", 0, PokemonType.Normal, 40, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false } }, 100, "큰 소리로 짖고 기합을 높여 자신의 공격을 올린다.", 0, 0, 1, 1);
        All["extreme-speed"] = new Move("신속", 80, PokemonType.Normal, 5, 100, false, 2, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "눈에 보이지 않는 굉장한 속도로 상대에게 돌진하여 공격한다. 반드시 선제공격을 할 수 있다.", 0, 0, 1, 1);
        All["burn-up"] = new Move("불사르기", 130, PokemonType.Fire, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "전신의 불꽃을 모두 태워서 큰 데미지를 준다. 자신의 불꽃타입이 없어진다.", 0, 0, 1, 1);
        All["mud-shot"] = new Move("머드샷", 55, PokemonType.Ground, 15, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "진흙 덩어리를 상대에게 내던져서 공격한다. 동시에 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["submission"] = new Move("지옥의바퀴", 80, PokemonType.Fighting, 20, 80, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "땅에 자신과 함께 상대를 내던져 공격한다. 자신도 조금 데미지를 입는다.", 0, -25, 1, 1);
        All["dynamic-punch"] = new Move("폭발펀치", 100, PokemonType.Fighting, 5, 50, false, 0, false, false, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "혼신의 힘으로 펀치를 날려서 공격한다. 상대를 반드시 혼란시킨다.", 0, 0, 1, 1);
        All["bulk-up"] = new Move("벌크업", 0, PokemonType.Fighting, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true } }, 100, "몸에 힘을 담아 근육을 두껍게 해서 자신의 공격과 방어를 올린다.", 0, 0, 1, 1);
        All["circle-throw"] = new Move("배대뒤치기", 60, PokemonType.Fighting, 10, 90, false, -6, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 내던져서 교대할 포켓몬을 끌어낸다. 야생의 경우에는 배틀이 끝난다.", 0, 0, 1, 1);
        All["recover"] = new Move("HP회복", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "세포를 재생시켜 자신의 최대 HP의 절반만큼 HP를 회복한다.", 50, 0, 1, 1);
        All["kinesis"] = new Move("숟가락휘기", 0, PokemonType.Psychic, 15, 80, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "숟가락을 휘어서 주의를 끌어 상대의 명중률을 낮춘다.", 0, 0, 1, 1);
        All["flash"] = new Move("플래시", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "눈이 부신 빛으로 상대의 명중률을 떨어뜨린다.", 0, 0, 1, 1);
        All["future-sight"] = new Move("미래예지", 120, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "기술을 사용한 2턴 뒤에 상대에게 염동력의 덩어리를 보내어 공격한다.", 0, 0, 1, 1);
        All["miracle-eye"] = new Move("미라클아이", 0, PokemonType.Psychic, 40, 100, true, 0, true, false, "no-type-immunity", 100, 0, new List<StatChangeEntry>(), 0, "악타입에 효과가 없는 기술이나 회피율이 높은 상대라 할지라도 공격이 맞게 된다.", 0, 0, 1, 1);
        All["psycho-cut"] = new Move("사이코커터", 70, PokemonType.Psychic, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "실체화시킨 마음의 칼날로 상대를 베어 가른다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["psyshock"] = new Move("사이코쇼크", 80, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "이상한 염력파를 실체화하여 상대를 공격한다. 물리적인 데미지를 준다.", 0, 0, 1, 1);
        All["barrier"] = new Move("배리어", 0, PokemonType.Psychic, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 2, TargetsSelf = true } }, 100, "튼튼한 장막을 만들어 자신의 방어를 크게 올린다.", 0, 0, 1, 1);
        All["strength"] = new Move("괴력", 80, PokemonType.Normal, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "혼신의 힘으로 상대를 세게 때려서 공격한다. 무거운 돌을 밀 수도 있다.", 0, 0, 1, 1);
        All["mach-punch"] = new Move("마하펀치", 40, PokemonType.Fighting, 30, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "눈에 보이지 않는 굉장한 속도로 펀치를 날린다. 반드시 선제공격을 할 수 있다.", 0, 0, 1, 1);
        All["vital-throw"] = new Move("받아던지기", 70, PokemonType.Fighting, 10, 100, true, -1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대보다 나중에 공격한다. 그 대신 자신의 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["rock-smash"] = new Move("바위깨기", 40, PokemonType.Fighting, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 50, "펀치로 공격한다. 상대의 방어를 떨어뜨릴 때가 있다. 바위를 깰 수도 있다.", 0, 0, 1, 1);
        All["revenge"] = new Move("리벤지", 60, PokemonType.Fighting, 10, 100, false, -4, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대에게 기술을 받으면 그 상대에게 주는 데미지가 2배가 된다.", 0, 0, 1, 1);
        All["brick-break"] = new Move("깨뜨리다", 75, PokemonType.Fighting, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "수도로 기세 좋게 내려쳐서 상대를 공격한다. 빛의장막이나 리플렉터도 파괴할 수 있다.", 0, 0, 1, 1);
        All["knock-off"] = new Move("탁쳐서떨구기", 65, PokemonType.Dark, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대의 지닌 물건을 탁 쳐서 떨어뜨려 배틀이 끝날 때까지 사용할 수 없게 한다. 물건을 가진 상대에게는 데미지를 더 준다.", 0, 0, 1, 1);
        All["bullet-punch"] = new Move("불릿펀치", 40, PokemonType.Steel, 30, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "탄환처럼 빠르고 단단한 펀치를 상대에게 날린다. 반드시 선제공격을 할 수 있다.", 0, 0, 1, 1);
        All["double-hit"] = new Move("더블어택", 35, PokemonType.Normal, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "꼬리 등을 써서 상대를 때려 공격한다. 2회 연속으로 데미지를 준다.", 0, 0, 2, 2);
        All["low-sweep"] = new Move("로킥", 65, PokemonType.Fighting, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "재빠른 움직임으로 상대의 다리를 노려 공격한다. 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["dual-chop"] = new Move("더블촙", 40, PokemonType.Dragon, 15, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "몸의 단단한 부분으로 상대를 때려 공격한다. 2회 연속으로 데미지를 준다.", 0, 0, 2, 2);
        All["drain-punch"] = new Move("드레인펀치", 75, PokemonType.Fighting, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "주먹으로 상대의 힘을 흡수한다. 입힌 데미지의 절반에 해당하는 HP를 회복할 수 있다.", 0, 50, 1, 1);
        All["leaf-blade"] = new Move("리프블레이드", 90, PokemonType.Grass, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "잎사귀를 칼처럼 이용해 상대를 베어 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["leaf-storm"] = new Move("리프스톰", 130, PokemonType.Grass, 5, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "뾰족한 잎사귀로 상대에게 바람을 일으킨다. 사용하면 반동으로 자신의 특수공격이 크게 떨어진다.", 0, 0, 1, 1);
        All["clear-smog"] = new Move("클리어스모그", 50, PokemonType.Poison, 15, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "특수한 진흙 덩어리를 상대에게 내던져서 공격한다. 능력 변화를 원래대로 돌린다.", 0, 0, 1, 1);
        All["leaf-tornado"] = new Move("그래스믹서", 65, PokemonType.Grass, 10, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 50, "날카로운 잎사귀로 상대를 둘러싸서 공격한다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["constrict"] = new Move("휘감기", 10, PokemonType.Normal, 35, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 10, "촉수나 덩굴 등을 휘감아서 공격한다. 상대의 스피드를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["acid-armor"] = new Move("녹기", 0, PokemonType.Poison, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 2, TargetsSelf = true } }, 100, "세포의 변화로 액체가 되어 자신의 방어를 크게 올린다.", 0, 0, 1, 1);
        All["brine"] = new Move("소금물", 65, PokemonType.Water, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대가 HP의 절반 정도 상처를 입고 있으면 기술의 위력이 2배가 된다.", 0, 0, 1, 1);
        All["rock-throw"] = new Move("돌떨구기", 50, PokemonType.Rock, 15, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "작은 바위를 들어올려 상대에게 내던져서 공격한다.", 0, 0, 1, 1);
        All["self-destruct"] = new Move("자폭", 200, PokemonType.Normal, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "폭발을 일으켜서 자신의 주위에 있는 포켓몬을 공격한다. 쓰고 나서 기절하게 된다.", 0, 0, 1, 1);
        All["explosion"] = new Move("대폭발", 250, PokemonType.Normal, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "큰 폭발로 자신의 주위에 있는 포켓몬을 공격한다. 쓰고 나서는 기절한다.", 0, 0, 1, 1);
        All["rock-slide"] = new Move("스톤샤워", 75, PokemonType.Rock, 10, 90, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "큰 바위를 세차게 부딪쳐서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["rock-blast"] = new Move("록블라스트", 25, PokemonType.Rock, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단한 암석을 상대에게 발사하여 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["rock-polish"] = new Move("록커트", 0, PokemonType.Rock, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "자신의 몸을 갈아 공기의 저항을 적게 한다. 스피드를 크게 올릴 수 있다.", 0, 0, 1, 1);
        All["stone-edge"] = new Move("스톤에지", 100, PokemonType.Rock, 5, 80, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "뾰족한 바위를 상대에게 꿰찔러서 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["smack-down"] = new Move("떨어뜨리기", 50, PokemonType.Rock, 15, 100, false, 0, false, false, "none", 100, 0, new List<StatChangeEntry>(), 0, "돌이나 구슬을 던져서 날고 있는 상대를 공격한다. 맞은 상대는 땅에 떨어진다.", 0, 0, 1, 1);
        All["mega-punch"] = new Move("메가톤펀치", 80, PokemonType.Normal, 20, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "힘을 담은 펀치로 상대를 공격한다.", 0, 0, 1, 1);
        All["steamroller"] = new Move("하드롤러", 65, PokemonType.Bug, 20, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "둥글게 뭉친 몸을 회전하여 상대를 뭉개 버린다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["stomp"] = new Move("짓밟기", 65, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "큰 발로 상대를 짓밟아서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["bounce"] = new Move("뛰어오르기", 85, PokemonType.Flying, 5, 85, false, 0, false, false, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "하늘 높이 뛰어올라 2턴째에 상대를 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["flame-charge"] = new Move("니트로차지", 50, PokemonType.Fire, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = false } }, 100, "불꽃을 둘러 상대를 공격한다. 힘을 모아서 자신의 스피드를 올린다.", 0, 0, 1, 1);
        All["smart-strike"] = new Move("스마트혼", 70, PokemonType.Steel, 10, 100, true, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "뾰족한 뿔로 상대를 꿰찔러서 공격한다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["slack-off"] = new Move("게으름피우기", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "게으름 피우며 쉰다. 자신의 HP를 최대 HP의 절반만큼 회복한다.", 50, 0, 1, 1);
        All["heal-pulse"] = new Move("치유파동", 0, PokemonType.Psychic, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "치유파동을 날려서 최대 HP의 절반만큼 상대의 HP를 회복한다.", 50, 0, 1, 1);
        All["zap-cannon"] = new Move("전자포", 120, PokemonType.Electric, 5, 50, false, 0, false, true, "paralysis", 100, 0, new List<StatChangeEntry>(), 0, "대포처럼 전기를 발사해서 공격한다. 상대를 마비 상태로 만든다.", 0, 0, 1, 1);
        All["metal-sound"] = new Move("금속음", 0, PokemonType.Steel, 40, 85, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -2, TargetsSelf = false } }, 100, "금속을 긁을 때 나는 듯한 싫은 소리를 들려준다. 상대의 특수방어를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["mirror-shot"] = new Move("미러샷", 65, PokemonType.Steel, 10, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 30, "갈고 닦은 몸에서 섬광의 힘을 상대에게 쏜다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["magnet-bomb"] = new Move("마그넷봄", 60, PokemonType.Steel, 20, 100, true, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대에게 달라붙는 강철의 폭탄을 발사한다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["cut"] = new Move("풀베기", 50, PokemonType.Normal, 30, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "낫이나 발톱 등으로 상대를 베어 공격한다. 가느다란 나무도 자를 수 있다.", 0, 0, 1, 1);
        All["false-swipe"] = new Move("칼등치기", 40, PokemonType.Normal, 40, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대의 HP가 반드시 1만큼 남도록 조절하여 공격한다.", 0, 0, 1, 1);
        All["brave-bird"] = new Move("브레이브버드", 120, PokemonType.Flying, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날개를 접어 저공비행으로 돌격한다. 자신도 상당한 데미지를 입는다.", 0, -33, 1, 1);
        All["jump-kick"] = new Move("점프킥", 100, PokemonType.Fighting, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "높이 점프해서 킥으로 상대를 공격한다. 빗나가면 자신이 데미지를 입는다.", 0, 0, 1, 1);
        All["uproar"] = new Move("소란피기", 90, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "3턴 동안 소란 피워 공격한다. 그 동안은 아무도 잠들지 않게 된다.", 0, 0, 1, 1);
        All["lunge"] = new Move("덤벼들기", 80, PokemonType.Bug, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "전력으로 상대에게 덤벼들며 공격한다. 상대의 공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["ice-beam"] = new Move("냉동빔", 90, PokemonType.Ice, 10, 100, false, 0, false, true, "freeze", 10, 0, new List<StatChangeEntry>(), 0, "냉동빔을 상대에게 발사하여 공격한다. 얼음 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["aurora-beam"] = new Move("오로라빔", 65, PokemonType.Ice, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 10, "무지개색의 빔을 상대에게 발사하여 공격한다. 공격을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["waterfall"] = new Move("폭포오르기", 80, PokemonType.Water, 15, 100, false, 0, false, false, "none", 0, 20, new List<StatChangeEntry>(), 0, "굉장한 기세로 상대에게 돌진한다. 상대를 풀죽게 만들 때가 있다. 폭포도 거슬러 올라갈 수 있다.", 0, 0, 1, 1);
        All["icy-wind"] = new Move("얼어붙은바람", 55, PokemonType.Ice, 15, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "차가운 냉기를 상대에게 내뿜어 공격한다. 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["dive"] = new Move("다이빙", 80, PokemonType.Water, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 잠수했다가 2턴째에 떠올라 공격한다.", 0, 0, 1, 1);
        All["ice-shard"] = new Move("얼음뭉치", 40, PokemonType.Ice, 30, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "얼음 덩어리를 순식간에 만들어 상대에게 빠르게 쏜다. 반드시 선제공격을 할 수 있다.", 0, 0, 1, 1);
        All["smog"] = new Move("스모그", 30, PokemonType.Poison, 20, 70, false, 0, false, true, "poison", 40, 0, new List<StatChangeEntry>(), 0, "더러운 가스를 상대에게 내뿜어 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["sludge"] = new Move("오물공격", 65, PokemonType.Poison, 20, 100, false, 0, false, true, "poison", 30, 0, new List<StatChangeEntry>(), 0, "더러운 오물을 상대에게 내던져서 공격한다. 독 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["poison-gas"] = new Move("독가스", 0, PokemonType.Poison, 40, 90, false, 0, true, false, "poison", 100, 0, new List<StatChangeEntry>(), 0, "독가스를 상대의 얼굴에 내뿜어 독 상태로 만든다.", 0, 0, 1, 1);
        All["memento"] = new Move("추억의선물", 0, PokemonType.Dark, 10, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -2, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "자신은 기절하게 되지만 그 대신 상대의 공격과 특수공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["venom-drench"] = new Move("베놈트랩", 0, PokemonType.Poison, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "특수한 독액을 끼얹는다. 독 상태인 상대는 공격, 특수공격, 스피드가 떨어진다.", 0, 0, 1, 1);
        All["clamp"] = new Move("껍질끼우기", 35, PokemonType.Water, 15, 85, false, 0, false, false, "trap", 100, 0, new List<StatChangeEntry>(), 0, "매우 튼튼하고 두꺼운 껍질에 4-5턴 동안 상대를 끼워서 공격한다.", 0, 0, 1, 1);
        All["whirlpool"] = new Move("바다회오리", 35, PokemonType.Water, 15, 85, false, 0, false, true, "trap", 100, 0, new List<StatChangeEntry>(), 0, "세차게 소용돌이치는 물속에 4-5턴 동안 상대를 가두어 공격한다.", 0, 0, 1, 1);
        All["icicle-spear"] = new Move("고드름침", 25, PokemonType.Ice, 30, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카로운 고드름을 상대에게 발사하여 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["razor-shell"] = new Move("셸블레이드", 75, PokemonType.Water, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 50, "날카로운 조개껍질로 베어 공격한다. 상대의 방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["spike-cannon"] = new Move("가시대포", 20, PokemonType.Normal, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카로운 침을 상대에게 발사해서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["icicle-crash"] = new Move("고드름떨구기", 85, PokemonType.Ice, 10, 90, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "큰 고드름을 격렬하게 부딪쳐서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["lick"] = new Move("핥기", 30, PokemonType.Ghost, 30, 100, false, 0, false, false, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "긴 혀로 상대를 핥아서 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["dream-eater"] = new Move("꿈먹기", 100, PokemonType.Psychic, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "잠자고 있는 상대의 꿈을 먹어 공격한다. 데미지의 절반을 HP로 회복한다.", 0, 50, 1, 1);
        All["nightmare"] = new Move("악몽", 0, PokemonType.Ghost, 15, 100, false, 0, true, false, "nightmare", 100, 0, new List<StatChangeEntry>(), 0, "잠듦 상태의 상대에게 악몽을 꾸게 하여 매 턴 조금씩 HP를 떨어뜨려 간다.", 0, 0, 1, 1);
        All["shadow-ball"] = new Move("섀도볼", 80, PokemonType.Ghost, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 20, "까만 그림자의 덩어리를 내던져서 공격한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["dark-pulse"] = new Move("악의파동", 80, PokemonType.Dark, 15, 100, false, 0, false, true, "none", 0, 20, new List<StatChangeEntry>(), 0, "몸에서 악의로 가득한 무서운 오라를 발한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["shadow-punch"] = new Move("섀도펀치", 60, PokemonType.Ghost, 20, 100, true, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "그림자에 섞여 펀치를 날린다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["perish-song"] = new Move("멸망의노래", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "perish-song", 100, 0, new List<StatChangeEntry>(), 0, "노래를 들은 포켓몬은 3턴이 지나면 기절한다. 교체되면 효과가 없어진다.", 0, 0, 1, 1);
        All["bind"] = new Move("조이기", 15, PokemonType.Normal, 20, 85, false, 0, false, false, "trap", 100, 0, new List<StatChangeEntry>(), 0, "긴 몸이나 덩굴 등을 써서 4-5턴 동안 상대를 조여 공격한다.", 0, 0, 1, 1);
        All["rock-tomb"] = new Move("암석봉인", 60, PokemonType.Rock, 15, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "암석을 내던져서 공격한다. 상대의 움직임을 봉인함으로써 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["high-horsepower"] = new Move("10만마력", 95, PokemonType.Ground, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "온몸을 써서 상대에게 맹렬히 어택한다.", 0, 0, 1, 1);
        All["meditate"] = new Move("요가포즈", 0, PokemonType.Psychic, 40, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true } }, 100, "잠들어 있는 힘을 몸속에서 끌어내어 자신의 공격을 올린다.", 0, 0, 1, 1);
        All["synchronoise"] = new Move("싱크로노이즈", 120, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "이상한 전파로 주위에 있는 자신과 같은 타입의 포켓몬에게 데미지를 준다.", 0, 0, 1, 1);
        All["vice-grip"] = new Move("찝기", 55, PokemonType.Normal, 30, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 양쪽에서 집어서 데미지를 준다.", 0, 0, 1, 1);
        All["crabhammer"] = new Move("집게해머", 100, PokemonType.Water, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "큰 집게를 상대에게 내리쳐서 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["hammer-arm"] = new Move("암해머", 100, PokemonType.Fighting, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = true } }, 100, "강하고 무거운 주먹을 휘둘러 데미지를 준다. 자신의 스피드가 떨어진다.", 0, 0, 1, 1);
        All["charge"] = new Move("충전", 0, PokemonType.Electric, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true } }, 100, "다음 턴에 쓸 전기타입 기술의 위력을 올린다. 자신의 특수방어도 올라간다.", 0, 0, 1, 1);
        All["charge-beam"] = new Move("차지빔", 50, PokemonType.Electric, 10, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false } }, 70, "전격의 다발을 상대에게 발사한다. 전기를 모아서 자신의 특수공격을 올릴 때가 있다.", 0, 0, 1, 1);
        All["eerie-impulse"] = new Move("괴전파", 0, PokemonType.Electric, 15, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "몸에서 괴전파를 내어 상대에게 쏨으로써 특수공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["magnetic-flux"] = new Move("자기장조작", 0, PokemonType.Electric, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = false } }, 100, "자기장 조작으로 인해 특성 플러스와 마이너스의 방어, 특수방어가 오른다.", 0, 0, 1, 1);
        All["barrage"] = new Move("구슬던지기", 15, PokemonType.Normal, 20, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "둥근 것을 상대에게 내던져서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["bullet-seed"] = new Move("씨기관총", 25, PokemonType.Grass, 30, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "씨앗을 기세 좋게 상대에게 발사하여 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["egg-bomb"] = new Move("알폭탄", 100, PokemonType.Normal, 10, 75, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "큰 알을 온 힘을 다해 상대에게 내던져서 공격한다.", 0, 0, 1, 1);
        All["wood-hammer"] = new Move("우드해머", 120, PokemonType.Grass, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단한 몸통을 상대에게 부딪쳐서 공격한다. 자신도 상당한 데미지를 입는다.", 0, -33, 1, 1);
        All["bone-club"] = new Move("뼈다귀치기", 65, PokemonType.Ground, 20, 85, false, 0, false, false, "none", 0, 10, new List<StatChangeEntry>(), 0, "손에 들고 있는 뼈로 상대를 세게 때려서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["bonemerang"] = new Move("뼈다귀부메랑", 50, PokemonType.Ground, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "손에 들고 있는 뼈를 상대에게 날려서 날아갈 때와 돌아올 때 2회 연속 데미지를 준다.", 0, 0, 2, 2);
        All["bone-rush"] = new Move("본러시", 25, PokemonType.Ground, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단한 뼈로 상대를 세게 때려서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["mega-kick"] = new Move("메가톤킥", 120, PokemonType.Normal, 5, 75, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "굉장한 힘을 담은 킥으로 상대를 걷어차서 공격한다.", 0, 0, 1, 1);
        All["rolling-kick"] = new Move("돌려차기", 60, PokemonType.Fighting, 15, 85, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "몸을 재빨리 회전시키며 걷어차서 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["high-jump-kick"] = new Move("무릎차기", 130, PokemonType.Fighting, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "점프해서 무릎차기로 상대를 공격한다. 빗나가면 자신이 데미지를 입는다.", 0, 0, 1, 1);
        All["facade"] = new Move("객기", 70, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신이 독, 마비, 화상 상태일 때 날리면 기술의 위력이 2배가 된다.", 0, 0, 1, 1);
        All["blaze-kick"] = new Move("블레이즈킥", 85, PokemonType.Fire, 10, 90, false, 0, false, false, "burn", 10, 0, new List<StatChangeEntry>(), 0, "공격한 상대를 화상 상태로 만들 때가 있다. 급소에도 맞기 쉽다.", 0, 0, 1, 1);
        All["axe-kick"] = new Move("발꿈치찍기", 120, PokemonType.Fighting, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "도끼를 내려찍듯 발차기로 상대를 공격한다. 빗나가면 자신이 데미지를 입는다.", 0, 0, 1, 1);
        All["comet-punch"] = new Move("연속펀치", 18, PokemonType.Normal, 15, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "노도 같은 펀치로 상대를 세게 때려서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["fire-punch"] = new Move("불꽃펀치", 75, PokemonType.Fire, 15, 100, false, 0, false, false, "burn", 10, 0, new List<StatChangeEntry>(), 0, "불꽃을 담은 펀치로 상대를 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["ice-punch"] = new Move("냉동펀치", 75, PokemonType.Ice, 15, 100, false, 0, false, false, "freeze", 10, 0, new List<StatChangeEntry>(), 0, "냉기를 담은 펀치로 상대를 공격한다. 얼음 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["dizzy-punch"] = new Move("잼잼펀치", 70, PokemonType.Normal, 10, 100, false, 0, false, false, "confusion", 20, 0, new List<StatChangeEntry>(), 0, "리드미컬한 펀치를 날려 상대를 공격한다. 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["detect"] = new Move("판별", 0, PokemonType.Fighting, 5, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "상대의 공격을 전혀 받지 않는다. 연속으로 쓰면 실패하기 쉽다.", 0, 0, 1, 1);
        All["focus-punch"] = new Move("힘껏펀치", 150, PokemonType.Fighting, 20, 100, false, -3, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "정신력을 높여 펀치를 날린다. 기술을 쓰기 전에 공격을 받으면 실패한다.", 0, 0, 1, 1);
        All["sky-uppercut"] = new Move("스카이어퍼", 85, PokemonType.Fighting, 15, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "하늘을 향하는 듯한 높은 업퍼로 상대를 밀어올려 공격한다.", 0, 0, 1, 1);
        All["vacuum-wave"] = new Move("진공파", 40, PokemonType.Fighting, 30, 100, false, 1, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "주먹을 흔들어 진공의 파도를 일으킨다. 반드시 선제공격을 할 수 있다.", 0, 0, 1, 1);
        All["power-up-punch"] = new Move("그로우펀치", 40, PokemonType.Fighting, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false } }, 100, "반복하여 때리면 점점 주먹이 단단해진다. 상대를 때리면 공격이 오른다.", 0, 0, 1, 1);
        All["giga-impact"] = new Move("기가임팩트", 150, PokemonType.Normal, 5, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "가진 힘을 모두 사용해서 상대에게 돌격한다. 다음 턴은 움직일 수 없다.", 0, 0, 1, 1);
        All["soft-boiled"] = new Move("알낳기", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 최대 HP 절반을 회복한다. 동료에게 HP를 나누어 줄 수도 있다.", 50, 0, 1, 1);
        All["last-resort"] = new Move("비장의무기", 140, PokemonType.Normal, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "배틀 중에 기억하고 있는 기술을 모두 사용하면 그때부터 쓸 수 있는 필살기이다.", 0, 0, 1, 1);
        All["ancient-power"] = new Move("원시의힘", 60, PokemonType.Rock, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = false } }, 10, "원시의 힘으로 공격한다. 자신의 모든 능력이 오를 때가 있다.", 0, 0, 1, 1);
        All["ingrain"] = new Move("뿌리박기", 0, PokemonType.Grass, 20, 100, true, 0, true, false, "ingrain", 100, 0, new List<StatChangeEntry>(), 0, "대지에 뿌리를 박아 매 턴마다 자신의 HP를 회복한다. 뿌리 박고 있으므로 교체할 수 없다.", 0, 0, 1, 1);
        All["tickle"] = new Move("간지르기", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 100, "몸을 간질여 웃게 만들어서 상대의 공격과 방어를 떨어뜨린다.", 0, 0, 1, 1);
        All["dragon-dance"] = new Move("용의춤", 0, PokemonType.Dragon, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = true } }, 100, "신비롭고 힘센 춤을 격렬하게 춘다. 자신의 공격과 스피드를 올린다.", 0, 0, 1, 1);
        All["dragon-pulse"] = new Move("용의파동", 85, PokemonType.Dragon, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "큰 입으로 충격파를 일으켜서 상대를 공격한다.", 0, 0, 1, 1);
        All["teeter-dance"] = new Move("흔들흔들댄스", 0, PokemonType.Normal, 20, 100, false, 0, true, false, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "흔들흔들 댄스를 춰서 주위에 있는 포켓몬을 혼란 상태로 만든다.", 0, 0, 1, 1);
        All["magical-leaf"] = new Move("매지컬리프", 60, PokemonType.Grass, 20, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 추적하는 이상한 잎사귀를 흩뿌린다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["dazzling-gleam"] = new Move("매지컬샤인", 80, PokemonType.Fairy, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "강력한 빛을 내어 상대에게 데미지를 준다.", 0, 0, 1, 1);
        All["blizzard"] = new Move("눈보라", 110, PokemonType.Ice, 5, 70, false, 0, false, true, "freeze", 10, 0, new List<StatChangeEntry>(), 0, "세찬 눈보라를 상대에게 내뿜어 공격한다. 얼음 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["lovely-kiss"] = new Move("악마의키스", 0, PokemonType.Normal, 10, 75, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "무서운 얼굴로 키스한다. 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["powder-snow"] = new Move("눈싸라기", 40, PokemonType.Ice, 25, 100, false, 0, false, true, "freeze", 10, 0, new List<StatChangeEntry>(), 0, "차가운 가랑눈을 상대에게 내뿜어 공격한다. 얼음 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["fake-tears"] = new Move("거짓울음", 0, PokemonType.Dark, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -2, TargetsSelf = false } }, 100, "우는 척을 하며 눈물을 흘린다. 난처하게 만들어 상대의 특수방어를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["avalanche"] = new Move("눈사태", 60, PokemonType.Ice, 10, 100, false, -4, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대로부터 기술을 받으면 그 상대에 대해서 기술의 위력이 2배가 된다.", 0, 0, 1, 1);
        All["heart-stamp"] = new Move("하트스탬프", 60, PokemonType.Psychic, 25, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "귀여운 모습으로 방심시켜서 강렬한 일격을 날린다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["shock-wave"] = new Move("전격파", 60, PokemonType.Electric, 20, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "전격을 재빠르게 상대에게 날린다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["hyper-beam"] = new Move("파괴광선", 150, PokemonType.Normal, 5, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "강한 광선을 상대에게 발사하여 공격한다. 다음 턴은 움직일 수 없다.", 0, 0, 1, 1);
        All["lava-plume"] = new Move("분연", 80, PokemonType.Fire, 15, 100, false, 0, false, true, "burn", 30, 0, new List<StatChangeEntry>(), 0, "새빨간 불꽃으로 자신의 주위에 있는 포켓몬을 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["storm-throw"] = new Move("업어후리기", 60, PokemonType.Fighting, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "강렬한 일격을 상대에게 날린다. 공격은 반드시 급소에 맞는다.", 0, 0, 1, 1);
        All["work-up"] = new Move("분발", 0, PokemonType.Normal, 30, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = true } }, 100, "스스로 분발해서 공격과 특수공격을 올린다.", 0, 0, 1, 1);
        All["raging-bull"] = new Move("레이징불", 90, PokemonType.Normal, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "뿔로 상대를 세차게 공격한다. 리플렉터와 빛의장막을 파괴한다.", 0, 0, 1, 1);
        All["dragon-tail"] = new Move("드래곤테일", 60, PokemonType.Dragon, 10, 90, false, -6, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대를 튕겨내서 교대할 포켓몬을 끌어낸다. 야생의 경우에는 배틀이 끝난다.", 0, 0, 1, 1);
        All["muddy-water"] = new Move("탁류", 90, PokemonType.Water, 10, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 30, "탁해진 물을 상대에게 발사하여 공격한다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["sharpen"] = new Move("각지기", 0, PokemonType.Normal, 30, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true } }, 100, "몸의 각을 늘려서 더욱 각지게 하여 자신의 공격을 올린다.", 0, 0, 1, 1);
        All["liquidation"] = new Move("아쿠아브레이크", 85, PokemonType.Water, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 20, "물의 힘으로 상대에게 부딪쳐서 공격한다. 상대의 방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["fly"] = new Move("공중날기", 90, PokemonType.Flying, 15, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 하늘을 날아 2턴째에 상대를 공격한다. 알고 있는 도시로 날아갈 수 있다.", 0, 0, 1, 1);
        All["iron-head"] = new Move("아이언헤드", 80, PokemonType.Steel, 15, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "강철과 같은 단단한 머리로 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["sky-drop"] = new Move("프리폴", 60, PokemonType.Flying, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 상대를 하늘로 끌고 가서 2턴째에 떨어뜨려 공격한다. 끌려간 상대는 움직일 수 없다.", 0, 0, 1, 1);
        All["snore"] = new Move("코골기", 50, PokemonType.Normal, 15, 100, false, 0, false, true, "none", 0, 30, new List<StatChangeEntry>(), 0, "자신이 잠들어있을 때 소음을 내어 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["freeze-dry"] = new Move("프리즈드라이", 70, PokemonType.Ice, 20, 100, false, 0, false, true, "freeze", 10, 0, new List<StatChangeEntry>(), 0, "상대를 급격히 차갑게 하여 얼음 상태로 만들 때가 있다. 물타입 포켓몬에게도 효과가 굉장해진다.", 0, 0, 1, 1);
        All["overheat"] = new Move("오버히트", 130, PokemonType.Fire, 5, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "풀 파워로 상대를 공격한다. 쓰면 반동으로 자신의 특수공격이 크게 떨어진다.", 0, 0, 1, 1);
        All["dragon-rush"] = new Move("드래곤다이브", 100, PokemonType.Dragon, 10, 75, false, 0, false, false, "none", 0, 20, new List<StatChangeEntry>(), 0, "굉장한 살기로 위압하면서 몸통박치기한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["aura-sphere"] = new Move("파동탄", 80, PokemonType.Fighting, 20, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "몸속에서 파동의 힘을 끌어내 쏜다. 공격은 반드시 명중한다.", 0, 0, 1, 1);
        All["psystrike"] = new Move("사이코브레이크", 100, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "이상한 염력파를 실체화하여 상대를 공격한다. 물리적인 데미지를 준다.", 0, 0, 1, 1);
        All["eruption"] = new Move("분화", 150, PokemonType.Fire, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "분노를 폭발시켜 상대를 공격한다. 자신의 HP가 적을수록 기술의 위력이 떨어진다.", 0, 0, 1, 1);
        All["defog"] = new Move("안개제거", 0, PokemonType.Flying, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "강한 바람으로 상대의 리플렉터나 빛의장막 등을 제거한다. 회피율도 떨어뜨린다.", 0, 0, 1, 1);
        All["shadow-sneak"] = new Move("야습", 40, PokemonType.Ghost, 30, 100, false, 1, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "그림자를 늘려 상대의 배후에서 공격한다. 반드시 선제공격할 수 있다.", 0, 0, 1, 1);
        All["infestation"] = new Move("엉겨붙기", 20, PokemonType.Bug, 20, 100, false, 0, false, true, "trap", 100, 0, new List<StatChangeEntry>(), 0, "4-5턴 동안 상대에게 엉겨 붙어서 공격한다. 그동안 상대는 도망갈 수 없다.", 0, 0, 1, 1);
        All["toxic-thread"] = new Move("독실", 0, PokemonType.Poison, 20, 100, false, 0, true, false, "poison", 100, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "독이 섞인 실을 뿜어낸다. 상대를 독 상태로 만들고 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["ominous-wind"] = new Move("괴상한바람", 60, PokemonType.Ghost, 5, 100, false, 0, false, true, "none", 10, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "speed", Change = 1, TargetsSelf = false } }, 10, "소름이 끼칠 만한 돌풍으로 상대를 공격한다. 자신의 모든 능력이 올라갈 때가 있다.", 0, 0, 1, 1);
        All["cotton-spore"] = new Move("목화포자", 0, PokemonType.Grass, 40, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -2, TargetsSelf = false } }, 100, "솜처럼 폭신폭신한 포자를 착 달라붙게 해서 상대의 스피드를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["cotton-guard"] = new Move("코튼가드", 0, PokemonType.Grass, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 3, TargetsSelf = true } }, 100, "푹신푹신한 솜털로 자신의 몸을 둘러싸서 지킨다. 방어를 매우 크게 올린다.", 0, 0, 1, 1);
        All["head-smash"] = new Move("양날박치기", 150, PokemonType.Rock, 5, 80, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "목숨을 걸고 혼신의 힘으로 상대에게 박치기를 한다. 자신도 굉장한 데미지를 입는다.", 0, -50, 1, 1);
        All["tearful-look"] = new Move("눈물그렁그렁", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "눈물을 그렁그렁거려 상대의 전의를 상실하게 한다. 상대의 공격과 특수공격이 떨어진다.", 0, 0, 1, 1);
        All["grass-whistle"] = new Move("풀피리", 0, PokemonType.Grass, 15, 55, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "기분 좋은 피리 소리를 들려주어 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["flower-shield"] = new Move("플라워가드", 0, PokemonType.Fairy, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false } }, 100, "이상한 힘을 사용하여 배틀에 나와있는 모든 풀타입 포켓몬의 방어를 올린다.", 0, 0, 1, 1);
        All["morning-sun"] = new Move("아침햇살", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 HP를 회복한다. 날씨에 따라 회복량이 변한다.", 50, 0, 1, 1);
        All["foul-play"] = new Move("속임수", 95, PokemonType.Dark, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "상대의 힘을 이용한다. 싸우고 있는 상대의 공격이 높을수록 데미지가 올라간다.", 0, 0, 1, 1);
        All["snarl"] = new Move("바크아웃", 55, PokemonType.Dark, 15, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "호되게 호통을 쳐서 상대의 특수공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["torment"] = new Move("트집", 0, PokemonType.Dark, 15, 100, false, 0, true, false, "torment", 100, 0, new List<StatChangeEntry>(), 0, "상대에게 트집을 잡아서 똑같은 기술을 2회 연속으로 쓸 수 없게 한다.", 0, 0, 1, 1);
        All["hidden-power"] = new Move("잠재파워", 60, PokemonType.Normal, 15, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "기술을 쓴 포켓몬에 따라 기술의 타입이 바뀐다.", 0, 0, 1, 1);
        All["twin-beam"] = new Move("트윈빔", 40, PokemonType.Psychic, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "두 줄기의 신비한 빛을 발사해 상대를 공격한다.", 0, 0, 1, 1);
        All["autotomize"] = new Move("바디퍼지", 0, PokemonType.Steel, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "몸의 쓸모없는 부분을 깎는다. 자신의 스피드를 크게 올리고 체중도 가벼워진다.", 0, 0, 1, 1);
        All["hyper-drill"] = new Move("하이퍼드릴", 100, PokemonType.Normal, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "회전하는 뿔로 상대를 꿰뚫어 공격한다.", 0, 0, 1, 1);
        All["poison-tail"] = new Move("포이즌테일", 50, PokemonType.Poison, 25, 100, false, 0, false, false, "poison", 10, 0, new List<StatChangeEntry>(), 0, "꼬리로 때린다. 독 상태로 만들 때가 있고 급소에도 맞기 쉽다.", 0, 0, 1, 1);
        All["arm-thrust"] = new Move("손바닥치기", 15, PokemonType.Fighting, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "펼친 양손으로 상대를 번갈아 쳐서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["throat-chop"] = new Move("지옥찌르기", 80, PokemonType.Dark, 15, 100, false, 0, false, false, "silence", 100, 0, new List<StatChangeEntry>(), 0, "이 기술에 맞은 상대는 지옥의 고통 때문에 2턴 동안 소리 기술을 낼 수 없다.", 0, 0, 1, 1);
        All["hone-claws"] = new Move("손톱갈기", 0, PokemonType.Dark, 15, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true } }, 100, "손톱을 갈아 날카롭게 한다. 자신의 공격과 명중률을 올린다.", 0, 0, 1, 1);
        All["octazooka"] = new Move("대포무노포", 65, PokemonType.Water, 10, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 50, "상대의 얼굴 등에 먹물을 내뿜어 공격한다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["steel-wing"] = new Move("강철날개", 70, PokemonType.Steel, 25, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = false } }, 10, "단단한 날개를 상대에게 부딪쳐서 공격한다. 자신의 방어가 올라갈 때가 있다.", 0, 0, 1, 1);
        All["embargo"] = new Move("금제", 0, PokemonType.Dark, 15, 100, false, 0, true, false, "embargo", 100, 0, new List<StatChangeEntry>(), 0, "지니게 한 도구를 쓸 수 없게 한다. 트레이너도 그 포켓몬에게는 도구를 쓸 수 없다.", 0, 0, 1, 1);
        All["comeuppance"] = new Move("앙갚음", 1, PokemonType.Dark, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "마지막으로 받은 데미지가 클수록 큰 데미지를 준다.", 0, 0, 1, 1);
        All["psyshield-bash"] = new Move("배리어러시", 70, PokemonType.Psychic, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "염동력으로 만든 갑옷을 두르고 상대에게 돌진해 공격한다.", 0, 0, 1, 1);
        All["triple-kick"] = new Move("트리플킥", 10, PokemonType.Fighting, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "3회 연속으로 킥을 날려 공격한다. 기술이 맞을 때마다 위력이 올라간다.", 0, 0, 3, 3);
        All["milk-drink"] = new Move("우유마시기", 0, PokemonType.Normal, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 최대 HP 절반을 회복한다. 동료에게 HP를 나누어 줄 수도 있다.", 50, 0, 1, 1);
        All["sacred-fire"] = new Move("성스러운불꽃", 100, PokemonType.Fire, 5, 95, false, 0, false, false, "burn", 50, 0, new List<StatChangeEntry>(), 0, "신비한 불꽃으로 상대를 태워서 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["aeroblast"] = new Move("에어로블라스트", 100, PokemonType.Flying, 5, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "공기의 소용돌이를 발사하여 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["weather-ball"] = new Move("웨더볼", 50, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "사용했을 때의 날씨에 따라서 기술 타입과 위력이 바뀐다.", 0, 0, 1, 1);
        All["heal-block"] = new Move("회복봉인", 0, PokemonType.Psychic, 15, 100, false, 0, true, false, "heal-block", 100, 0, new List<StatChangeEntry>(), 0, "5턴 동안 기술이나 특성, 지니고 있는 도구에 의한 HP 회복을 할 수 없게 한다.", 0, 0, 1, 1);
        All["leafage"] = new Move("나뭇잎", 40, PokemonType.Grass, 40, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "잎을 상대에 맞춰 공격한다.", 0, 0, 1, 1);
        All["thief"] = new Move("도둑질", 60, PokemonType.Dark, 25, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "공격과 동시에 도구를 훔친다. 자신이 도구를 지니고 있을 경우에는 훔칠 수 없다.", 0, 0, 1, 1);
        All["attract"] = new Move("헤롱헤롱", 0, PokemonType.Normal, 15, 100, false, 0, true, false, "infatuation", 100, 0, new List<StatChangeEntry>(), 0, "수컷은 암컷을 암컷은 수컷을 유혹하여 헤롱헤롱하게 만든다. 상대가 기술을 쓰기 어려워진다.", 0, 0, 1, 1);
        All["mystical-fire"] = new Move("매지컬플레임", 75, PokemonType.Fire, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "입에서 내뱉는 아주 뜨거운 불꽃으로 공격한다. 상대의 특수공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["force-palm"] = new Move("발경", 60, PokemonType.Fighting, 10, 100, false, 0, false, false, "paralysis", 30, 0, new List<StatChangeEntry>(), 0, "상대의 몸에 충격파를 부딪쳐 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["phantom-force"] = new Move("고스트다이브", 90, PokemonType.Ghost, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 어디론가 사라져서 2턴째에 상대를 공격한다. 기술 방어를 무시하고 공격할 수 있다.", 0, 0, 1, 1);
        All["boomburst"] = new Move("폭음파", 140, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "무시무시한 폭음의 파괴력으로 주위에 있는 포켓몬을 공격한다.", 0, 0, 1, 1);
        All["smelling-salts"] = new Move("정신차리기", 70, PokemonType.Normal, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "마비 상태의 상대에게는 위력이 2배가 되지만 대신 상대의 마비가 풀린다.", 0, 0, 1, 1);
        All["headlong-rush"] = new Move("들이받기", 120, PokemonType.Ground, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "온몸으로 상대에게 돌진해 강하게 들이받는다.", 0, 0, 1, 1);
        All["tail-glow"] = new Move("반딧불", 0, PokemonType.Bug, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 3, TargetsSelf = true } }, 100, "깜빡거리는 빛을 바라보고 자신의 정신을 통일하여 특수공격을 매우 크게 올린다.", 0, 0, 1, 1);
        All["water-spout"] = new Move("해수스파우팅", 150, PokemonType.Water, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "바닷물을 내뿜어 공격한다. 자신의 HP가 적을수록 기술의 위력이 떨어진다.", 0, 0, 1, 1);
        All["noble-roar"] = new Move("부르짖기", 0, PokemonType.Normal, 30, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "우렁차게 부르짖어서 상대를 위협하여 상대의 공격과 특수공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["needle-arm"] = new Move("바늘팔", 60, PokemonType.Grass, 15, 100, false, 0, false, false, "none", 0, 30, new List<StatChangeEntry>(), 0, "바늘팔을 세차게 흔들어 공격한다. 상대를 풀죽게 만들 때가 있다.", 0, 0, 1, 1);
        All["power-trip"] = new Move("기어오르기", 20, PokemonType.Dark, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "자신의 강함을 뻐기고 공격한다. 자신의 능력이 올라가 있는 만큼 위력이 오른다.", 0, 0, 1, 1);
        All["dragon-hammer"] = new Move("드래곤해머", 90, PokemonType.Dragon, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "몸을 해머처럼 써서 상대를 덮쳐 데미지를 준다.", 0, 0, 1, 1);
        All["frost-breath"] = new Move("얼음숨결", 60, PokemonType.Ice, 10, 90, false, 0, false, true, "none", 100, 0, new List<StatChangeEntry>(), 0, "차가운 숨결을 상대에게 내뿜어 공격한다. 반드시 급소에 맞는다.", 0, 0, 1, 1);
        All["ice-ball"] = new Move("아이스볼", 30, PokemonType.Ice, 20, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 구르기를 반복하여 상대를 공격한다. 기술이 맞을 때마다 위력이 올라간다.", 0, 0, 1, 1);
        All["dual-wingbeat"] = new Move("더블윙", 40, PokemonType.Flying, 10, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날개를 상대에게 부딪쳐서 공격한다. 2회 연속으로 데미지를 준다.", 0, 0, 2, 2);
        All["mist-ball"] = new Move("미스트볼", 95, PokemonType.Psychic, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 50, "안개의 깃털로 둘러싸 공격한다. 상대의 특수공격을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["luster-purge"] = new Move("러스터퍼지", 95, PokemonType.Psychic, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 50, "눈부신 빛을 발산하여 공격한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["origin-pulse"] = new Move("근원의파동", 110, PokemonType.Water, 10, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "파랗게 빛나는 무수한 광선으로 상대를 공격한다.", 0, 0, 1, 1);
        All["precipice-blades"] = new Move("단애의칼", 120, PokemonType.Ground, 10, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "대지의 힘을 칼날로 바꿔 상대를 공격한다.", 0, 0, 1, 1);
        All["dragon-ascent"] = new Move("화룡점정", 120, PokemonType.Flying, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 100, "넓은 하늘에서 급속으로 강하하여 상대를 공격한다. 자신의 방어와 특수방어가 떨어진다.", 0, 0, 1, 1);
        All["doom-desire"] = new Move("파멸의소원", 140, PokemonType.Steel, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "기술을 사용한 2턴 뒤에 무수한 빛의 다발이 상대를 공격한다.", 0, 0, 1, 1);
        All["psycho-boost"] = new Move("사이코부스트", 140, PokemonType.Psychic, 5, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -2, TargetsSelf = false } }, 100, "풀 파워로 상대를 공격한다. 쓰면 반동으로 자신의 특수공격이 크게 떨어진다.", 0, 0, 1, 1);
        All["raging-fury"] = new Move("대격분", 120, PokemonType.Fire, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "격렬한 분노를 불꽃으로 내뿜어 상대를 공격한다.", 0, 0, 1, 1);
        All["volt-switch"] = new Move("볼트체인지", 70, PokemonType.Electric, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "공격한 뒤 굉장한 스피드로 돌아와서 교대 포켓몬과 교체한다.", 0, 0, 1, 1);
        All["attack-order"] = new Move("공격지령", 90, PokemonType.Bug, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "부하를 불러내어 상대를 향해서 공격시킨다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["defend-order"] = new Move("방어지령", 0, PokemonType.Bug, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = true } }, 100, "부하를 불러내어 자신의 몸을 뒤덮게 한다. 방어와 특수방어를 올릴 수 있다.", 0, 0, 1, 1);
        All["heal-order"] = new Move("회복지령", 0, PokemonType.Bug, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "부하를 불러내어 상처를 회복한다. 최대 HP의 절반만큼 자신의 HP를 회복한다.", 50, 0, 1, 1);
        All["aromatic-mist"] = new Move("아로마미스트", 0, PokemonType.Fairy, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = 1, TargetsSelf = false } }, 100, "신비한 아로마 향으로 같은 편의 특수방어를 올린다.", 0, 0, 1, 1);
        All["strength-sap"] = new Move("힘흡수", 0, PokemonType.Grass, 10, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "상대 공격력과 동일하게 자신의 HP를 회복한다. 그리고 상대의 공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["chatter"] = new Move("수다", 65, PokemonType.Flying, 20, 100, false, 0, false, true, "confusion", 100, 0, new List<StatChangeEntry>(), 0, "기억한 말로 음파를 일으켜서 공격한다. 상대를 혼란시킨다.", 0, 0, 1, 1);
        All["parting-shot"] = new Move("막말내뱉기", 0, PokemonType.Dark, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "막말을 내뱉어 상대를 위협하여 공격과 특수공격을 떨어뜨린 후 교대 포켓몬과 교체한다.", 0, 0, 1, 1);
        All["confide"] = new Move("비밀이야기", 0, PokemonType.Normal, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = -1, TargetsSelf = false } }, 100, "비밀 이야기를 하면서 상대의 집중력을 잃게 하여 상대의 특수공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["rock-wrecker"] = new Move("암석포", 150, PokemonType.Rock, 5, 90, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "거대한 바위를 상대에게 발사하여 공격한다. 다음 턴은 움직일 수 없게 된다.", 0, 0, 1, 1);
        All["sacred-sword"] = new Move("성스러운칼", 90, PokemonType.Fighting, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "긴 뿔로 베어 공격한다. 상대의 능력 변화에 관계없이 데미지를 준다.", 0, 0, 1, 1);
        All["aqua-cutter"] = new Move("아쿠아커터", 70, PokemonType.Water, 20, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "물의 칼날을 만들어 상대를 베어 공격한다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["body-press"] = new Move("바디프레스", 80, PokemonType.Fighting, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "몸을 부딪쳐서 공격한다. 방어가 높을수록 주는 데미지가 올라간다.", 0, 0, 1, 1);
        All["mystical-power"] = new Move("신비의힘", 70, PokemonType.Psychic, 10, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "신비한 힘을 모아 상대를 공격한다. 사용 장소에 따라 효과가 달라진다.", 0, 0, 1, 1);
        All["roar-of-time"] = new Move("시간의포효", 150, PokemonType.Dragon, 5, 90, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "시간이 뒤틀릴 정도의 힘을 사용해서 상대를 공격한다. 다음 턴은 움직일 수 없다.", 0, 0, 1, 1);
        All["spacial-rend"] = new Move("공간절단", 100, PokemonType.Dragon, 5, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "주위의 공간과 더불어 상대를 찢어서 데미지를 준다. 급소에 맞기 쉽다.", 0, 0, 1, 1);
        All["magma-storm"] = new Move("마그마스톰", 100, PokemonType.Fire, 5, 75, false, 0, false, true, "trap", 100, 0, new List<StatChangeEntry>(), 0, "세차게 타오르는 불꽃 속에 4-5턴 동안 상대를 가두어 공격한다.", 0, 0, 1, 1);
        All["shadow-force"] = new Move("섀도다이브", 120, PokemonType.Ghost, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "1턴째에 모습을 감춰 2턴째에 상대를 공격한다. 방어하고 있어도 공격은 맞는다.", 0, 0, 1, 1);
        All["dark-void"] = new Move("다크홀", 0, PokemonType.Dark, 10, 50, false, 0, true, false, "sleep", 100, 0, new List<StatChangeEntry>(), 0, "암흑의 세계로 끌고 가서 떨어뜨려 상대를 잠듦 상태로 만든다.", 0, 0, 1, 1);
        All["seed-flare"] = new Move("시드플레어", 120, PokemonType.Grass, 5, 85, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -2, TargetsSelf = false } }, 40, "몸속에서 충격파를 발생시킨다. 상대의 특수방어를 크게 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["judgment"] = new Move("심판의뭉치", 100, PokemonType.Normal, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "무수한 광탄을 상대에게 방출한다. 자신이 가지고 있는 플레이트에 따라 타입이 바뀐다.", 0, 0, 1, 1);
        All["searing-shot"] = new Move("화염탄", 100, PokemonType.Fire, 5, 100, false, 0, false, true, "burn", 30, 0, new List<StatChangeEntry>(), 0, "새빨간 불꽃으로 자신의 주위에 있는 포켓몬을 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["v-create"] = new Move("V제너레이트", 180, PokemonType.Fire, 5, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false }, new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "작열하는 불꽃을 이마에서 발생시켜 이판사판으로 몸통박치기한다. 방어, 특수방어, 스피드가 떨어진다.", 0, 0, 1, 1);
        All["scald"] = new Move("열탕", 80, PokemonType.Water, 15, 100, false, 0, false, true, "burn", 30, 0, new List<StatChangeEntry>(), 0, "뜨겁게 끓어오르는 물을 상대에게 발사해서 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["secret-power"] = new Move("비밀의힘", 70, PokemonType.Normal, 20, 100, false, 0, false, false, "none", 30, 0, new List<StatChangeEntry>(), 0, "비밀의 힘으로 상대를 공격한다. 사용 장소에 따라 추가 효과가 변화한다.", 0, 0, 1, 1);
        All["rock-climb"] = new Move("록클라임", 90, PokemonType.Normal, 20, 85, false, 0, false, false, "confusion", 20, 0, new List<StatChangeEntry>(), 0, "굉장한 기세로 상대에게 돌진하여 공격한다. 상대를 혼란시킬 때가 있다.", 0, 0, 1, 1);
        All["night-daze"] = new Move("나이트버스트", 85, PokemonType.Dark, 10, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 40, "암흑의 충격파를 날려서 상대를 공격한다. 명중률을 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["tail-slap"] = new Move("스위프뺨치기", 25, PokemonType.Normal, 10, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "단단한 꼬리로 상대를 때려서 공격한다. 2-5회 동안 연속으로 쓴다.", 0, 0, 2, 5);
        All["horn-leech"] = new Move("우드혼", 75, PokemonType.Grass, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "뿔을 꿰찔러서 상대의 양분을 흡수한다. 입힌 데미지의 절반에 해당하는 HP를 회복할 수 있다.", 0, 50, 1, 1);
        All["electroweb"] = new Move("일렉트릭네트", 55, PokemonType.Electric, 15, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "전기 네트로 상대를 붙잡아서 공격한다. 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["shift-gear"] = new Move("기어체인지", 0, PokemonType.Steel, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = true }, new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "톱니바퀴를 돌려서 자신의 공격을 올리는 것뿐만 아니라 스피드도 크게 올린다.", 0, 0, 1, 1);
        All["gear-grind"] = new Move("기어소서", 50, PokemonType.Steel, 15, 85, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "강철의 기어를 상대에게 던져서 공격한다. 2회 연속으로 데미지를 준다.", 0, 0, 2, 2);
        All["gear-up"] = new Move("어시스트기어", 0, PokemonType.Steel, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = 1, TargetsSelf = false }, new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false } }, 100, "기어를 넣는 것으로 특성 플러스와 마이너스의 공격과 특수공격이 올라간다.", 0, 0, 1, 1);
        All["breaking-swipe"] = new Move("와이드브레이커", 60, PokemonType.Dragon, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "attack", Change = -1, TargetsSelf = false } }, 100, "강인한 꼬리를 세차게 휘둘러서 상대를 공격한다. 상대의 공격을 떨어뜨린다.", 0, 0, 1, 1);
        All["water-shuriken"] = new Move("물수리검", 15, PokemonType.Water, 20, 100, false, 1, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "점액으로 만든 수리검을 2-5회 동안 연속으로 던진다. 반드시 선제공격할 수 있다.", 0, 0, 2, 5);
        All["head-charge"] = new Move("아프로브레이크", 120, PokemonType.Normal, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "굉장한 아프로 머리로 상대에게 돌진하여 공격한다. 자신도 조금 데미지를 입는다.", 0, -25, 1, 1);
        All["fire-lash"] = new Move("불꽃채찍", 80, PokemonType.Fire, 15, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = -1, TargetsSelf = false } }, 100, "불타는 채찍으로 상대를 친다. 공격을 받은 상대는 방어가 떨어진다.", 0, 0, 1, 1);
        All["fiery-dance"] = new Move("불꽃춤", 80, PokemonType.Fire, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 1, TargetsSelf = false } }, 50, "불꽃을 둘러 날개를 쳐서 공격한다. 자신의 특수공격이 오를 때가 있다.", 0, 0, 1, 1);
        All["bleakwind-storm"] = new Move("찬바람폭풍", 100, PokemonType.Flying, 10, 80, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "세찬 바람을 폭풍으로 일으켜 상대를 공격한다. 상대의 스피드를 낮출 때가 있다.", 0, 0, 1, 1);
        All["wildbolt-storm"] = new Move("번개폭풍", 100, PokemonType.Electric, 10, 80, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "번개를 동반한 폭풍으로 상대를 공격한다. 상대를 마비시킬 때가 있다.", 0, 0, 1, 1);
        All["blue-flare"] = new Move("푸른불꽃", 130, PokemonType.Fire, 5, 85, false, 0, false, true, "burn", 20, 0, new List<StatChangeEntry>(), 0, "아름다우면서도 격렬한 푸른불꽃으로 상대를 둘러싸서 공격한다. 화상 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["fusion-flare"] = new Move("크로스플레임", 100, PokemonType.Fire, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "거대한 불꽃을 내리친다. 거대한 천둥의 영향을 받아 기술의 위력이 올라간다.", 0, 0, 1, 1);
        All["bolt-strike"] = new Move("뇌격", 130, PokemonType.Electric, 5, 85, false, 0, false, false, "paralysis", 20, 0, new List<StatChangeEntry>(), 0, "방대한 전기를 몸에 둘러 상대에게 돌진해서 공격한다. 마비 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["fusion-bolt"] = new Move("크로스썬더", 100, PokemonType.Electric, 5, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "거대한 천둥을 내리친다. 거대한 불꽃의 영향을 받아 기술의 위력이 올라간다.", 0, 0, 1, 1);
        All["sandsear-storm"] = new Move("열사의폭풍", 100, PokemonType.Ground, 10, 80, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "뜨거운 모래바람을 일으켜 상대를 공격한다. 상대를 화상 입힐 때가 있다.", 0, 0, 1, 1);
        All["glaciate"] = new Move("얼어붙은세계", 65, PokemonType.Ice, 10, 95, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "speed", Change = -1, TargetsSelf = false } }, 100, "차가운 냉기를 상대에게 내뿜어 공격한다. 상대의 스피드를 떨어뜨린다.", 0, 0, 1, 1);
        All["secret-sword"] = new Move("신비의칼", 85, PokemonType.Fighting, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "긴 뿔로 베어 공격한다. 뿔이 머금은 이상한 힘은 물리적인 데미지를 준다.", 0, 0, 1, 1);
        All["relic-song"] = new Move("옛노래", 75, PokemonType.Normal, 10, 100, false, 0, false, true, "sleep", 10, 0, new List<StatChangeEntry>(), 0, "옛노래를 상대에게 들려주고 마음에 호소하여 공격한다. 잠듦 상태로 만들 때가 있다.", 0, 0, 1, 1);
        All["techno-blast"] = new Move("테크노버스터", 120, PokemonType.Normal, 5, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "광탄을 상대에게 방출한다. 자신이 지니고 있는 카세트에 의해 타입이 바뀐다.", 0, 0, 1, 1);
        All["parabolic-charge"] = new Move("파라볼라차지", 65, PokemonType.Electric, 20, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "주위에 있는 모든 포켓몬에게 데미지를 준다. 준 데미지의 절반을 자신이 회복한다.", 0, 50, 1, 1);
        All["flying-press"] = new Move("플라잉프레스", 100, PokemonType.Fighting, 10, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "공중에서 상대에게 다이브한다. 이 기술은 격투타입임과 동시에 비행타입이기도 하다.", 0, 0, 1, 1);
        All["branch-poke"] = new Move("가지찌르기", 40, PokemonType.Grass, 40, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "날카롭고 뾰족한 가지로 상대를 찔러서 공격한다.", 0, 0, 1, 1);
        All["geomancy"] = new Move("지오컨트롤", 0, PokemonType.Fairy, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-attack", Change = 2, TargetsSelf = true }, new StatChangeEntry { Stat = "special-defense", Change = 2, TargetsSelf = true }, new StatChangeEntry { Stat = "speed", Change = 2, TargetsSelf = true } }, 100, "1턴째에 에너지를 흡수하여 2턴째에 특수공격, 특수방어, 스피드를 크게 올린다.", 0, 0, 1, 1);
        All["focus-blast"] = new Move("기합구슬", 120, PokemonType.Fighting, 5, 70, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "special-defense", Change = -1, TargetsSelf = false } }, 10, "기합을 높여서 혼신의 힘을 방출한다. 상대의 특수방어를 떨어뜨릴 때가 있다.", 0, 0, 1, 1);
        All["oblivion-wing"] = new Move("데스윙", 80, PokemonType.Flying, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "조준한 상대로부터 HP를 흡수한다. 준 데미지의 반 이상 HP를 회복한다.", 0, 75, 1, 1);
        All["thousand-arrows"] = new Move("사우전드애로", 90, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 100, 0, new List<StatChangeEntry>(), 0, "떠 있는 포켓몬도 맞힐 수 있다. 떠 있던 상대는 맞아서 땅에 떨어진다.", 0, 0, 1, 1);
        All["thousand-waves"] = new Move("사우전드웨이브", 90, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "땅 위를 뻗어 나가는 파도로 공격한다. 파도에 휩쓸린 상대는 전투에서 도망칠 수 없게 된다.", 0, 0, 1, 1);
        All["lands-wrath"] = new Move("그라운드포스", 90, PokemonType.Ground, 10, 100, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "대지의 힘을 모으고 그 힘을 상대에게 집중시켜서 데미지를 준다.", 0, 0, 1, 1);
        All["core-enforcer"] = new Move("코어퍼니셔", 100, PokemonType.Dragon, 10, 100, false, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "데미지를 준 상대가 이미 행동을 끝냈다면 상대의 특성을 없애버린다.", 0, 0, 1, 1);
        All["diamond-storm"] = new Move("다이아스톰", 100, PokemonType.Rock, 5, 95, false, 0, false, false, "none", 0, 0, new List<StatChangeEntry> { new StatChangeEntry { Stat = "defense", Change = 2, TargetsSelf = false } }, 50, "다이아 폭풍을 일으켜 데미지를 준다. 자신의 방어를 올릴 때가 있다.", 0, 0, 1, 1);
        All["hyperspace-hole"] = new Move("이차원홀", 80, PokemonType.Psychic, 5, 100, true, 0, false, true, "none", 0, 0, new List<StatChangeEntry>(), 0, "다른차원홀로 갑자기 상대 바로 옆에 나타나 공격한다. 방어나 판별도 무시할 수 있다.", 0, 0, 1, 1);
        All["steam-eruption"] = new Move("스팀버스트", 110, PokemonType.Water, 5, 95, false, 0, false, true, "burn", 30, 0, new List<StatChangeEntry>(), 0, "상대에게 굉장히 뜨거운 증기를 뿜는다. 상대는 화상을 입기도 한다.", 0, 0, 1, 1);
        All["reflect"] = new Move("리플렉터", 0, PokemonType.Psychic, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 물리 기술의 데미지를 줄인다.", 0, 0, 1, 1);
        All["light-screen"] = new Move("빛의장막", 0, PokemonType.Psychic, 30, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 특수 기술의 데미지를 줄인다.", 0, 0, 1, 1);
        All["aurora-veil"] = new Move("오로라베일", 0, PokemonType.Ice, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 받는 데미지를 줄인다.", 0, 0, 1, 1);
        All["endure"] = new Move("버티기", 0, PokemonType.Normal, 10, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "이번 턴에 쓰러질 공격을 받아도 HP를 1 남기고 버틴다.", 0, 0, 1, 1);
        All["substitute"] = new Move("대타출동", 0, PokemonType.Normal, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "최대 HP의 4분의 1을 사용해 자신의 대타를 만든다.", 0, 0, 1, 1);
        All["trick-room"] = new Move("트릭룸", 0, PokemonType.Psychic, 5, 100, true, -7, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "5턴 동안 느린 포켓몬부터 움직인다.", 0, 0, 1, 1);
        All["gravity"] = new Move("중력", 0, PokemonType.Psychic, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "5턴 동안 중력을 강하게 해 모든 포켓몬을 땅에 붙인다.", 0, 0, 1, 1);
        All["counter"] = new Move("카운터", 0, PokemonType.Fighting, 20, 100, true, -5, false, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "그 턴에 받은 물리 공격의 2배를 되돌려준다.", 0, 0, 1, 1);
        All["mirror-coat"] = new Move("미러코트", 0, PokemonType.Psychic, 20, 100, true, -5, false, true, "none", 0, 0, new List<StatChangeEntry>(), 100, "그 턴에 받은 특수 공격의 2배를 되돌려준다.", 0, 0, 1, 1);
        All["trick"] = new Move("트릭", 0, PokemonType.Psychic, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "자신과 상대의 도구를 바꾼다.", 0, 0, 1, 1);
        All["switcheroo"] = new Move("바꿔치기", 0, PokemonType.Dark, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "자신과 상대의 도구를 바꾼다.", 0, 0, 1, 1);
        All["stealth-rock"] = new Move("스텔스록", 0, PokemonType.Rock, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "상대 진영에 뾰족한 돌을 설치해 교체해 나온 포켓몬을 공격한다.", 0, 0, 1, 1);
        All["spikes"] = new Move("압정뿌리기", 0, PokemonType.Ground, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "상대 진영에 압정을 설치한다. 최대 3번까지 겹칠 수 있다.", 0, 0, 1, 1);
        All["toxic-spikes"] = new Move("독압정", 0, PokemonType.Poison, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "상대 진영에 독압정을 설치한다. 최대 2번까지 겹칠 수 있다.", 0, 0, 1, 1);
        All["sticky-web"] = new Move("끈적끈적네트", 0, PokemonType.Bug, 20, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 100, "상대 진영에 끈적한 거미줄을 설치해 교체해 나온 포켓몬의 스피드를 낮춘다.", 0, 0, 1, 1);
        All["baneful-bunker"] = new Move("독가시방벽", 0, PokemonType.Poison, 10, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "상대의 공격을 막고 접촉한 상대를 독 상태로 만든다.", 0, 0, 1, 1);
        All["spiky-shield"] = new Move("가시방벽", 0, PokemonType.Grass, 10, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "상대의 공격을 막고 접촉한 상대에게 상처를 입힌다.", 0, 0, 1, 1);
        All["obstruct"] = new Move("완강한거부", 0, PokemonType.Dark, 10, 100, true, 4, true, false, "protect", 100, 0, new List<StatChangeEntry>(), 0, "상대의 공격을 막고 접촉한 상대의 방어를 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["taunt"] = new Move("도발", 0, PokemonType.Dark, 20, 100, false, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "3턴 동안 상대는 변화 기술을 사용할 수 없게 된다.", 0, 0, 1, 1);
        All["kings-shield"] = new Move("킹실드", 0, PokemonType.Steel, 10, 100, true, 4, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "공격을 막고 접촉한 상대의 공격을 크게 떨어뜨린다.", 0, 0, 1, 1);
        All["sunny-day"] = new Move("쾌청", 0, PokemonType.Fire, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 햇살이 강해진다.", 0, 0, 1, 1);
        All["rain-dance"] = new Move("비바라기", 0, PokemonType.Water, 5, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 비가 내린다.", 0, 0, 1, 1);
        All["sandstorm"] = new Move("모래바람", 0, PokemonType.Rock, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 모래바람이 분다.", 0, 0, 1, 1);
        All["hail"] = new Move("싸라기눈", 0, PokemonType.Ice, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 싸라기눈이 내린다.", 0, 0, 1, 1);
        All["grassy-terrain"] = new Move("그래스필드", 0, PokemonType.Grass, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 땅이 초록빛으로 물든다.", 0, 0, 1, 1);
        All["electric-terrain"] = new Move("일렉트릭필드", 0, PokemonType.Electric, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 땅에 전기가 흐른다.", 0, 0, 1, 1);
        All["psychic-terrain"] = new Move("사이코필드", 0, PokemonType.Psychic, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 신비한 힘이 땅을 감싼다.", 0, 0, 1, 1);
        All["misty-terrain"] = new Move("미스트필드", 0, PokemonType.Fairy, 10, 100, true, 0, true, false, "none", 0, 0, new List<StatChangeEntry>(), 0, "5턴 동안 안개가 땅을 감싼다.", 0, 0, 1, 1);

        var statChangeCorrections = new Dictionary<string, List<StatChangeEntry>>
        {
            ["ancient-power"] = new()
            {
                new() { Stat = "attack", Change = 1, TargetsSelf = true },
                new() { Stat = "defense", Change = 1, TargetsSelf = true },
                new() { Stat = "special-attack", Change = 1, TargetsSelf = true },
                new() { Stat = "special-defense", Change = 1, TargetsSelf = true },
                new() { Stat = "speed", Change = 1, TargetsSelf = true }
            },
            ["charge-beam"] = new() { new() { Stat = "special-attack", Change = 1, TargetsSelf = true } },
            ["coil"] = new()
            {
                new() { Stat = "attack", Change = 1, TargetsSelf = true },
                new() { Stat = "defense", Change = 1, TargetsSelf = true },
                new() { Stat = "accuracy", Change = 1, TargetsSelf = true }
            },
            ["defog"] = new() { new() { Stat = "evasion", Change = -1, TargetsSelf = false } },
            ["double-team"] = new() { new() { Stat = "evasion", Change = 1, TargetsSelf = true } },
            ["dragon-ascent"] = new()
            {
                new() { Stat = "defense", Change = -1, TargetsSelf = true },
                new() { Stat = "special-defense", Change = -1, TargetsSelf = true }
            },
            ["fiery-dance"] = new() { new() { Stat = "special-attack", Change = 1, TargetsSelf = true } },
            ["flash"] = new() { new() { Stat = "accuracy", Change = -1, TargetsSelf = false } },
            ["hone-claws"] = new()
            {
                new() { Stat = "attack", Change = 1, TargetsSelf = true },
                new() { Stat = "accuracy", Change = 1, TargetsSelf = true }
            },
            ["howl"] = new() { new() { Stat = "attack", Change = 1, TargetsSelf = true } },
            ["kinesis"] = new() { new() { Stat = "accuracy", Change = -1, TargetsSelf = false } },
            ["leaf-storm"] = new() { new() { Stat = "special-attack", Change = -2, TargetsSelf = true } },
            ["metal-claw"] = new() { new() { Stat = "attack", Change = 1, TargetsSelf = true } },
            ["minimize"] = new() { new() { Stat = "evasion", Change = 2, TargetsSelf = true } },
            ["mud-slap"] = new() { new() { Stat = "accuracy", Change = -1, TargetsSelf = false } },
            ["overheat"] = new() { new() { Stat = "special-attack", Change = -2, TargetsSelf = true } },
            ["psycho-boost"] = new() { new() { Stat = "special-attack", Change = -2, TargetsSelf = true } },
            ["rapid-spin"] = new() { new() { Stat = "speed", Change = 1, TargetsSelf = true } },
            ["sand-attack"] = new() { new() { Stat = "accuracy", Change = -1, TargetsSelf = false } },
            ["silver-wind"] = new()
            {
                new() { Stat = "attack", Change = 1, TargetsSelf = true },
                new() { Stat = "defense", Change = 1, TargetsSelf = true },
                new() { Stat = "special-attack", Change = 1, TargetsSelf = true },
                new() { Stat = "special-defense", Change = 1, TargetsSelf = true },
                new() { Stat = "speed", Change = 1, TargetsSelf = true }
            },
            ["steel-wing"] = new() { new() { Stat = "defense", Change = 1, TargetsSelf = true } },
            ["sweet-scent"] = new() { new() { Stat = "evasion", Change = -2, TargetsSelf = false } },
            ["v-create"] = new()
            {
                new() { Stat = "defense", Change = -1, TargetsSelf = true },
                new() { Stat = "special-defense", Change = -1, TargetsSelf = true },
                new() { Stat = "speed", Change = -1, TargetsSelf = true }
            }
        };
        foreach (var correction in statChangeCorrections)
            All[correction.Key].StatChanges = correction.Value;

        var statChangeChanceCorrections = new Dictionary<string, int>
        {
            ["ancient-power"] = 10,
            ["charge-beam"] = 70,
            ["coil"] = 100,
            ["defog"] = 100,
            ["double-team"] = 100,
            ["dragon-ascent"] = 100,
            ["fiery-dance"] = 50,
            ["flash"] = 100,
            ["hone-claws"] = 100,
            ["howl"] = 100,
            ["kinesis"] = 100,
            ["leaf-storm"] = 100,
            ["metal-claw"] = 10,
            ["minimize"] = 100,
            ["mud-slap"] = 100,
            ["overheat"] = 100,
            ["psycho-boost"] = 100,
            ["rapid-spin"] = 100,
            ["sand-attack"] = 100,
            ["silver-wind"] = 10,
            ["steel-wing"] = 10,
            ["sweet-scent"] = 100,
            ["v-create"] = 100
        };
        foreach (var correction in statChangeChanceCorrections)
            All[correction.Key].StatChangeChance = correction.Value;
    }
}
