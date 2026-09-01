namespace PokemonBattle.Models;

public static class AbilityDatabase
{
    public static Dictionary<string, AbilityInfo> All = new Dictionary<string, AbilityInfo>();

    // DataGen owns the descriptions, while this set records the runtime rules that
    // are intentionally supported by the battle simulator.
    private static readonly HashSet<string> ImplementedAbilities = new()
    {
        "심록", "엽록소", "맹화", "선파워", "급류", "젖은접시", "인분", "탈피",
        "복안", "색안경", "벌레의알림", "근성", "위협", "정전기", "피뢰침",
        "모래숨기", "모래헤치기", "독가시", "매직가드", "타오르는불꽃", "가뭄",
        "승기", "정신력", "악취", "포자", "건조피부", "테크니션", "유연",
        "투쟁심", "헤롱헤롱바디", "둔감", "틈새포착", "습기", "트레이스", "프레셔",
        "통찰", "배짱", "틀깨기", "주눅", "천진", "무기력",
        "의기양양", "저수", "싱크로", "노가드", "클리어바디", "옹골참",
        "마이페이스", "일찍기상", "두꺼운지방", "촉촉바디", "스킬링크", "부유",
        "저주받은바디", "불면", "괴력집게", "방음", "이판사판", "철주먹",
        "자연회복", "하늘의은총", "리프가드", "쓱쓱", "수의베일", "필터",
        "불꽃몸", "자기과신", "적응력", "축전", "속보", "다운로드", "면역",
        "눈숨기", "이상한비늘", "멀티스케일", "우격다짐", "의욕", "천하장사",
        "가속", "승리의별", "방진", "마그마의무장", "불굴의마음", "모래날림", "재생력",
        "잔비", "포이즌힐", "게으름", "불가사의부적", "노말스킨", "시간벌기",
        "순수한힘", "까칠한피부", "하드록", "하얀연기", "독폭주", "마중물",
        "아이스바디", "내열", "모래의힘", "눈퍼뜨리기", "전기엔진", "슬로스타트",
        "심술꾸러기", "부풀린가슴", "깨어진갑옷", "독수", "짓궂은마음", "초식",
        "철가시", "눈치우기", "오기", "정의의마음", "방탄", "매지션",
        "질풍날개", "퍼코트", "단단한발톱", "메가런처", "옹골찬턱",
        "프리즈스킨", "페어리스킨", "에어록", "날씨부정", "풀모피",
        "스나이퍼", "분노의경혈", "조가비갑옷", "전투무장", "대운",
        "먹보", "수확", "볼주머니", "긴장감", "픽업", "개미지옥", "그림자밟기", "돌머리",
        "달마모드", "배틀스위치", "멀티타입", "변환자재", "변색", "괴짜", "유폭", "미라", "단순",
        "도주", "자력", "흡반", "플러스", "마이너스", "텔레파시", "치유의마음",
        "플라워기프트", "프렌드가드", "플라워베일", "공생", "아로마베일", "스위트베일",
        "날카로운눈", "갈지자걸음", "발광", "미라클스킨"
        , "해감액", "예지몽", "화학변화가스", "곡예", "바람타기", "위험예지"
        , "기분파", "라이트메탈", "꿀모으기", "서투름", "나쁜손버릇", "예리함"
        , "일루전", "점착", "터보블레이즈", "테라볼티지"
        , "다크오라", "페어리오라", "오라브레이크", "매직미러", "나이트메어"
    };

    public static bool IsImplemented(string ability) => ImplementedAbilities.Contains(ability);

    public static string GetImplementationGroup(string ability) => ability switch
    {
        "스나이퍼" or "대운" or "분노의경혈" or "조가비갑옷" or "전투무장"
            => "급소 규칙",
        "먹보" or "수확" or "볼주머니" or "긴장감" or "픽업"
            => "나무열매·도구 규칙",
        "도주" or "개미지옥" or "그림자밟기" or "자력" or "흡반"
            => "교체·포획 규칙",
        "달마모드" or "배틀스위치" or "멀티타입" or "변환자재" or "변색" or "괴짜"
            => "폼·타입 변화",
        "플러스" or "마이너스" or "텔레파시" or "치유의마음" or "플라워기프트"
            or "프렌드가드" or "플라워베일" or "공생" or "아로마베일" or "스위트베일"
            => "아군·더블배틀",
        "날카로운눈" or "갈지자걸음" or "발광" or "미라클스킨" or "승리의별"
            => "명중·회피 규칙",
        "날씨부정" or "에어록" or "풀모피" or "페어리오라" or "다크오라" or "오라브레이크"
            => "필드·날씨 규칙",
        "매직미러" or "나이트메어" => "상태·반사 규칙",
        "투쟁심" or "헤롱헤롱바디" or "둔감"
            => "성별·멘탈 규칙",
        "화학변화가스" or "터보블레이즈" or "테라볼티지"
            => "특성 무효화 규칙",
        "예지몽" or "위험예지" or "통찰"
            => "정보 규칙",
        "곡예" or "바람타기" or "라이트메탈" or "서투름" or "예리함"
            => "아이템·기술 규칙",
        "기분파" or "일루전"
            => "폼 변화 규칙",
        "해감액" or "나쁜손버릇" or "점착"
            => "피해·아이템 규칙",
        _ => "기타 전투 규칙"
    };

    static AbilityDatabase()
    {
        All["심록"] = new AbilityInfo("심록", "위급할 때 풀타입의 위력이 올라간다.");
        All["엽록소"] = new AbilityInfo("엽록소", "맑을 때 스피드가 올라간다.");
        All["맹화"] = new AbilityInfo("맹화", "위급할 때 불꽃타입의 위력이 올라간다.");
        All["선파워"] = new AbilityInfo("선파워", "맑으면 HP가 줄지만 특수공격이 올라간다.");
        All["급류"] = new AbilityInfo("급류", "위급할 때 물타입의 위력이 올라간다.");
        All["젖은접시"] = new AbilityInfo("젖은접시", "비가 올 때 조금씩 HP를 회복한다.");
        All["인분"] = new AbilityInfo("인분", "기술의 추가 효과를 받지 않는다.");
        All["도주"] = new AbilityInfo("도주", "야생 포켓몬으로부터 반드시 도망칠 수 있다.");
        All["탈피"] = new AbilityInfo("탈피", "상태 이상을 회복할 때가 있다.");
        All["복안"] = new AbilityInfo("복안", "기술의 명중률이 올라간다.");
        All["색안경"] = new AbilityInfo("색안경", "효과가 별로인 기술이 강해진다.");
        All["벌레의알림"] = new AbilityInfo("벌레의알림", "위급할 때 벌레타입의 위력이 올라간다.");
        All["스나이퍼"] = new AbilityInfo("스나이퍼", "급소에 맞혔을 때 위력이 올라간다.");
        All["날카로운눈"] = new AbilityInfo("날카로운눈", "명중률이 떨어지지 않는다.");
        All["갈지자걸음"] = new AbilityInfo("갈지자걸음", "혼란에 빠져있으면 회피하기 쉬워진다.");
        All["근성"] = new AbilityInfo("근성", "상태 이상이 되면 공격이 올라간다.");
        All["위협"] = new AbilityInfo("위협", "상대의 공격을 떨어뜨린다.");
        All["정전기"] = new AbilityInfo("정전기", "접촉한 상대를 마비시킬 때가 있다.");
        All["피뢰침"] = new AbilityInfo("피뢰침", "전기를 끌어모아 특수공격을 올린다.");
        All["모래숨기"] = new AbilityInfo("모래숨기", "모래바람으로 회피율이 올라간다.");
        All["모래헤치기"] = new AbilityInfo("모래헤치기", "모래바람으로 스피드가 올라간다.");
        All["독가시"] = new AbilityInfo("독가시", "접촉한 상대를 중독시킬 때가 있다.");
        All["투쟁심"] = new AbilityInfo("투쟁심", "상대와 성별이 같으면 강해진다.");
        All["헤롱헤롱바디"] = new AbilityInfo("헤롱헤롱바디", "스치면 헤롱헤롱 상태가 될 때가 있다.");
        All["매직가드"] = new AbilityInfo("매직가드", "공격 이외에는 데미지를 입지 않는다.");
        All["타오르는불꽃"] = new AbilityInfo("타오르는불꽃", "불꽃을 받으면 불꽃 기술이 강해진다.");
        All["가뭄"] = new AbilityInfo("가뭄", "배틀에 나가면 햇살이 강해진다.");
        All["승기"] = new AbilityInfo("승기", "능력이 떨어지면 특수공격이 올라간다.");
        All["정신력"] = new AbilityInfo("정신력", "풀죽지 않는다.");
        All["틈새포착"] = new AbilityInfo("틈새포착", "상대의 벽을 뚫고 공격한다.");
        All["악취"] = new AbilityInfo("악취", "악취 때문에 상대가 풀죽을 때가 있다.");
        All["포자"] = new AbilityInfo("포자", "스치면 독, 마비, 잠듦 상태가 될 때가 있다.");
        All["건조피부"] = new AbilityInfo("건조피부", "더우면 HP가 줄어든다. 물로 HP를 회복한다.");
        All["개미지옥"] = new AbilityInfo("개미지옥", "배틀에서 상대를 도망칠 수 없게 한다.");
        All["픽업"] = new AbilityInfo("픽업", "도구를 주워올 때가 있다.");
        All["테크니션"] = new AbilityInfo("테크니션", "약한 기술의 위력이 올라간다.");
        All["유연"] = new AbilityInfo("유연", "마비 상태가 되지 않는다.");
        All["습기"] = new AbilityInfo("습기", "누구도 폭발 할 수 없게 된다.");
        All["날씨부정"] = new AbilityInfo("날씨부정", "날씨의 영향이 없어진다.");
        All["의기양양"] = new AbilityInfo("의기양양", "잠듦 상태가 되지 않는다.");
        All["분노의경혈"] = new AbilityInfo("분노의경혈", "급소에 맞으면 공격이 올라간다.");
        All["저수"] = new AbilityInfo("저수", "물을 받으면 회복한다.");
        All["싱크로"] = new AbilityInfo("싱크로", "독, 마비, 화상을 상대에게 옮긴다.");
        All["노가드"] = new AbilityInfo("노가드", "서로의 기술이 반드시 맞는다.");
        All["먹보"] = new AbilityInfo("먹보", "나무열매를 여느 때보다 빨리 사용한다.");
        All["클리어바디"] = new AbilityInfo("클리어바디", "상대가 능력을 떨어뜨릴 수 없다.");
        All["해감액"] = new AbilityInfo("해감액", "흡수한 상대의 HP를 줄인다.");
        All["돌머리"] = new AbilityInfo("돌머리", "부딪쳐도 반동을 받지 않는다.");
        All["옹골참"] = new AbilityInfo("옹골참", "일격으로 쓰러지지 않는다.");
        All["둔감"] = new AbilityInfo("둔감", "헤롱헤롱이나 도발 상태가 되지 않는다.");
        All["마이페이스"] = new AbilityInfo("마이페이스", "혼란 상태가 되지 않는다.");
        All["자력"] = new AbilityInfo("자력", "강철의 포켓몬을 도망칠 수 없게 한다.");
        All["일찍기상"] = new AbilityInfo("일찍기상", "잠듦 상태에서 빨리 깨어난다.");
        All["두꺼운지방"] = new AbilityInfo("두꺼운지방", "불꽃과 얼음타입의 기술에 강하다.");
        All["촉촉바디"] = new AbilityInfo("촉촉바디", "비가 오면 상태 이상이 회복된다.");
        All["점착"] = new AbilityInfo("점착", "달라붙어서 도구를 지킨다.");
        All["조가비갑옷"] = new AbilityInfo("조가비갑옷", "상대의 공격이 급소에 맞지 않는다.");
        All["스킬링크"] = new AbilityInfo("스킬링크", "연속 기술을 많이 쓸 수 있다.");
        All["부유"] = new AbilityInfo("부유", "땅타입의 기술을 받지 않는다.");
        All["저주받은바디"] = new AbilityInfo("저주받은바디", "공격받으면 가끔 상대를 사슬묶기 상태로 만든다.");
        All["불면"] = new AbilityInfo("불면", "잠듦 상태가 되지 않는다.");
        All["예지몽"] = new AbilityInfo("예지몽", "상대가 지닌 기술을 꿰뚫어볼 수 있다.");
        All["괴력집게"] = new AbilityInfo("괴력집게", "상대가 공격을 떨어뜨리지 못한다.");
        All["방음"] = new AbilityInfo("방음", "소리 기술을 받지 않는다.");
        All["수확"] = new AbilityInfo("수확", "사용한 나무열매를 몇 번이고 만들어 낸다.");
        All["이판사판"] = new AbilityInfo("이판사판", "반동 데미지를 받는 기술이 강해진다.");
        All["철주먹"] = new AbilityInfo("철주먹", "펀치를 사용하는 기술의 위력이 올라간다.");
        All["화학변화가스"] = new AbilityInfo("화학변화가스", "화학변화가스를 가진 포켓몬이 배틀에 나와 있으면 모든 포켓몬이 가진 특성의 효과가 사라지거나 발동하지 않게 된다.");
        All["자연회복"] = new AbilityInfo("자연회복", "배틀에서 일단 물러나면 상태 이상이 회복된다.");
        All["하늘의은총"] = new AbilityInfo("하늘의은총", "기술의 추가 효과가 나오기 쉽다.");
        All["리프가드"] = new AbilityInfo("리프가드", "맑을 때는 상태 이상이 되지 않는다.");
        All["배짱"] = new AbilityInfo("배짱", "고스트타입에 노말 기술이 맞는다.");
        All["쓱쓱"] = new AbilityInfo("쓱쓱", "비가 올 때 스피드가 올라간다.");
        All["수의베일"] = new AbilityInfo("수의베일", "화상 상태가 되지 않는다.");
        All["발광"] = new AbilityInfo("발광", "야생 포켓몬과 만나기 쉬워진다.");
        All["필터"] = new AbilityInfo("필터", "효과가 굉장한 기술의 위력을 약하게 한다.");
        All["불꽃몸"] = new AbilityInfo("불꽃몸", "접촉한 상대에게 화상을 입힐 때가 있다.");
        All["틀깨기"] = new AbilityInfo("틀깨기", "특성에 관계없이 상대에게 기술을 쓸 수 있다.");
        All["주눅"] = new AbilityInfo("주눅", "주눅이 들어 스피드가 올라가는 타입이 있다.");
        All["자기과신"] = new AbilityInfo("자기과신", "상대를 쓰러뜨리면 공격이 올라간다.");
        All["괴짜"] = new AbilityInfo("괴짜", "눈앞의 포켓몬으로 변신해버린다.");
        All["적응력"] = new AbilityInfo("적응력", "타입이 같은 기술의 위력이 올라간다.");
        All["축전"] = new AbilityInfo("축전", "전기를 받으면 회복한다.");
        All["속보"] = new AbilityInfo("속보", "상태 이상이 되면 스피드가 올라간다.");
        All["트레이스"] = new AbilityInfo("트레이스", "상대와 같은 특성이 된다.");
        All["다운로드"] = new AbilityInfo("다운로드", "상대의 능력을 보고 능력치를 바꾼다.");
        All["전투무장"] = new AbilityInfo("전투무장", "상대의 공격이 급소에 맞지 않는다.");
        All["프레셔"] = new AbilityInfo("프레셔", "상대가 사용하는 기술의 PP를 많이 줄인다.");
        All["면역"] = new AbilityInfo("면역", "독 상태가 되지 않는다.");
        All["눈숨기"] = new AbilityInfo("눈숨기", "날씨가 싸라기눈일 때 회피율이 올라간다.");
        All["이상한비늘"] = new AbilityInfo("이상한비늘", "상태 이상이 되면 방어가 올라간다.");
        All["멀티스케일"] = new AbilityInfo("멀티스케일", "HP가 꽉 찼을 때 데미지가 줄어든다.");
        All["긴장감"] = new AbilityInfo("긴장감", "상대를 긴장시켜 나무열매를 먹지 못하게 한다.");
        All["우격다짐"] = new AbilityInfo("우격다짐", "힘이 강해지지만 추가 효과가 없어진다.");
        All["의욕"] = new AbilityInfo("의욕", "공격은 높지만 빗나가기 쉽다.");
        All["플러스"] = new AbilityInfo("플러스", "플러스나 마이너스가 있으면 특수공격이 올라간다.");
        All["치유의마음"] = new AbilityInfo("치유의마음", "같은 편의 상태 이상을 가끔 회복시킨다.");
        All["천하장사"] = new AbilityInfo("천하장사", "물리공격의 위력이 올라간다.");
        All["가속"] = new AbilityInfo("가속", "조금씩 스피드가 높아진다.");
        All["매직미러"] = new AbilityInfo("매직미러", "변화 기술을 되받아칠 수 있다.");
        All["나이트메어"] = new AbilityInfo("나이트메어", "잠든 상대가 턴 종료마다 고통받는다.");
        All["대운"] = new AbilityInfo("대운", "상대의 급소에 공격이 맞기 쉽다.");
        All["그림자밟기"] = new AbilityInfo("그림자밟기", "상대의 그림자를 밟아 도망칠 수 없게 한다.");
        All["텔레파시"] = new AbilityInfo("텔레파시", "같은 편의 공격의 낌새를 읽고 기술을 받지 않는다.");
        All["방진"] = new AbilityInfo("방진", "먼지나 가루를 막는다.");
        All["마그마의무장"] = new AbilityInfo("마그마의무장", "얼음 상태가 되지 않는다.");
        All["흡반"] = new AbilityInfo("흡반", "교체시키는 기술이나 도구의 효과를 받지 않는다.");
        All["통찰"] = new AbilityInfo("통찰", "상대가 지닌 물건을 알 수 있다.");
        All["불굴의마음"] = new AbilityInfo("불굴의마음", "풀죽을 때마다 스피드가 올라간다.");
        All["모래날림"] = new AbilityInfo("모래날림", "배틀에서 모래바람을 일으킨다.");
        All["재생력"] = new AbilityInfo("재생력", "볼에 넣으면 HP가 조금 회복된다.");
        All["곡예"] = new AbilityInfo("곡예", "도구가 없어지면 스피드가 올라간다.");
        All["바람타기"] = new AbilityInfo("바람타기", "");
        All["잔비"] = new AbilityInfo("잔비", "배틀에 나가면 비를 내린다.");
        All["포이즌힐"] = new AbilityInfo("포이즌힐", "독 상태가 되면 HP를 회복한다.");
        All["게으름"] = new AbilityInfo("게으름", "연속으로 공격할 수 없다.");
        All["불가사의부적"] = new AbilityInfo("불가사의부적", "효과가 굉장한 기술밖에 맞지 않는다.");
        All["노말스킨"] = new AbilityInfo("노말스킨", "쓴 기술이 모두 노말타입이 된다.");
        All["시간벌기"] = new AbilityInfo("시간벌기", "상대보다 재빨라도 행동이 느려진다.");
        All["순수한힘"] = new AbilityInfo("순수한힘", "물리공격의 위력이 올라간다.");
        All["마이너스"] = new AbilityInfo("마이너스", "플러스나 마이너스가 있으면 특수공격이 올라간다.");
        All["까칠한피부"] = new AbilityInfo("까칠한피부", "접촉한 상대에게 상처를 입힌다.");
        All["단순"] = new AbilityInfo("단순", "능력 변화가 여느 때보다 심하다.");
        All["하드록"] = new AbilityInfo("하드록", "효과가 굉장한 기술의 위력을 약하게 한다.");
        All["하얀연기"] = new AbilityInfo("하얀연기", "상대가 능력을 떨어뜨릴 수 없다.");
        All["독폭주"] = new AbilityInfo("독폭주", "독 상태일 때 물리공격의 위력이 올라간다.");
        All["위험예지"] = new AbilityInfo("위험예지", "상대가 지닌 위험한 기술을 감지한다.");
        All["마중물"] = new AbilityInfo("마중물", "물을 끌어모아 특수공격을 올린다.");
        All["기분파"] = new AbilityInfo("기분파", "날씨에 따라 캐스퐁이 변화한다.");
        All["변색"] = new AbilityInfo("변색", "받은 기술의 타입으로 변화한다.");
        All["변환자재"] = new AbilityInfo("변환자재", "사용한 기술과 같은 타입으로 변화한다.");
        All["아이스바디"] = new AbilityInfo("아이스바디", "싸라기눈일 때 HP를 조금씩 회복한다.");
        All["라이트메탈"] = new AbilityInfo("라이트메탈", "자신의 무게가 절반이 된다.");
        All["에어록"] = new AbilityInfo("에어록", "날씨의 영향이 없어진다.");
        All["천진"] = new AbilityInfo("천진", "상대의 능력 변화를 무시한다.");
        All["꿀모으기"] = new AbilityInfo("꿀모으기", "달콤한꿀을 모아서 올 때가 있다.");
        All["플라워기프트"] = new AbilityInfo("플라워기프트", "맑을 때 자신과 같은 편이 강해진다.");
        All["유폭"] = new AbilityInfo("유폭", "기절할 때 스친 상대에게 데미지를 준다.");
        All["서투름"] = new AbilityInfo("서투름", "지니고 있는 도구를 쓸 수 없다.");
        All["내열"] = new AbilityInfo("내열", "불꽃 기술의 위력을 약하게 한다.");
        All["모래의힘"] = new AbilityInfo("모래의힘", "모래바람으로 위력이 올라가는 기술이 있다.");
        All["눈퍼뜨리기"] = new AbilityInfo("눈퍼뜨리기", "배틀에 나가면 싸라기눈을 내리게 한다.");
        All["나쁜손버릇"] = new AbilityInfo("나쁜손버릇", "닿은 상대로부터 도구를 훔친다.");
        All["전기엔진"] = new AbilityInfo("전기엔진", "전기를 받으면 스피드가 올라간다.");
        All["예리함"] = new AbilityInfo("예리함", "");
        All["슬로스타트"] = new AbilityInfo("슬로스타트", "공격과 스피드가 잠시 동안 절반이 된다.");
        All["멀티타입"] = new AbilityInfo("멀티타입", "지니고 있는 플레이트에 따라 타입이 바뀐다.");
        All["승리의별"] = new AbilityInfo("승리의별", "자신과 같은 편의 명중률이 올라간다.");
        All["심술꾸러기"] = new AbilityInfo("심술꾸러기", "능력의 변화가 역전된다.");
        All["부풀린가슴"] = new AbilityInfo("부풀린가슴", "방어를 떨어뜨리는 공격을 받지 않는다.");
        All["깨어진갑옷"] = new AbilityInfo("깨어진갑옷", "물리 기술을 받으면 방어가 떨어지고 스피드가 올라간다.");
        All["독수"] = new AbilityInfo("독수", "접촉하기만 해도 상대를 독 상태로 만들 때가 있다.");
        All["짓궂은마음"] = new AbilityInfo("짓궂은마음", "변화 기술을 먼저 쓸 수 있다.");
        All["달마모드"] = new AbilityInfo("달마모드", "위급할 때 모습이 변화한다.");
        All["미라클스킨"] = new AbilityInfo("미라클스킨", "변화 기술을 받기 어려운 몸으로 되어 있다.");
        All["미라"] = new AbilityInfo("미라", "상대의 기술을 받으면 상대를 미라로 만든다.");
        All["무기력"] = new AbilityInfo("무기력", "HP가 절반이 되면 능력이 떨어진다.");
        All["일루전"] = new AbilityInfo("일루전", "뒤의 포켓몬으로 둔갑하여 나온다.");
        All["초식"] = new AbilityInfo("초식", "풀 기술을 받으면 공격이 올라간다.");
        All["철가시"] = new AbilityInfo("철가시", "접촉한 상대에게 상처를 입힌다.");
        All["눈치우기"] = new AbilityInfo("눈치우기", "날씨가 싸라기눈일 때 스피드가 올라간다.");
        All["오기"] = new AbilityInfo("오기", "능력이 떨어지면 공격이 올라간다.");
        All["정의의마음"] = new AbilityInfo("정의의마음", "악 기술을 받으면 공격이 올라간다.");
        All["터보블레이즈"] = new AbilityInfo("터보블레이즈", "특성에 관계없이 상대에게 기술을 쓸 수 있다.");
        All["테라볼티지"] = new AbilityInfo("테라볼티지", "특성에 관계없이 상대에게 기술을 쓸 수 있다.");
        All["방탄"] = new AbilityInfo("방탄", "구슬이나 폭탄에 맞지 않는다.");
        All["매지션"] = new AbilityInfo("매지션", "기술을 맞은 상대의 도구를 빼앗아 버린다.");
        All["볼주머니"] = new AbilityInfo("볼주머니", "나무열매를 먹으면 HP도 회복한다.");
        All["질풍날개"] = new AbilityInfo("질풍날개", "비행타입의 기술이 먼저 나오게 된다.");
        All["프렌드가드"] = new AbilityInfo("프렌드가드", "같은 편의 데미지를 줄일 수 있다.");
        All["플라워베일"] = new AbilityInfo("플라워베일", "같은 편의 풀타입 포켓몬은 능력이 떨어지지 않는다.");
        All["공생"] = new AbilityInfo("공생", "같은 편에게 도구를 건넬 수 있게 된다.");
        All["풀모피"] = new AbilityInfo("풀모피", "그래스필드일 때 방어가 올라간다.");
        All["퍼코트"] = new AbilityInfo("퍼코트", "물리 기술의 데미지가 절반이 된다.");
        All["배틀스위치"] = new AbilityInfo("배틀스위치", "배틀모드에서 모습이 바뀐다.");
        All["아로마베일"] = new AbilityInfo("아로마베일", "같은 편으로 향하는 멘탈 공격을 막는다.");
        All["스위트베일"] = new AbilityInfo("스위트베일", "같은 편의 포켓몬이 잠들지 않게 된다.");
        All["단단한발톱"] = new AbilityInfo("단단한발톱", "접촉하는 기술의 위력이 올라간다.");
        All["메가런처"] = new AbilityInfo("메가런처", "파동 기술의 위력이 크다.");
        All["옹골찬턱"] = new AbilityInfo("옹골찬턱", "턱이 튼튼하여 무는 힘이 강하다.");
        All["프리즈스킨"] = new AbilityInfo("프리즈스킨", "노말타입의 기술이 얼음타입이 된다.");
        All["페어리스킨"] = new AbilityInfo("페어리스킨", "노말타입의 기술이 페어리타입이 된다.");
        All["페어리오라"] = new AbilityInfo("페어리오라", "전원의 페어리타입 기술이 강해진다.");
        All["다크오라"] = new AbilityInfo("다크오라", "전원의 악타입 기술이 강해진다.");
        All["오라브레이크"] = new AbilityInfo("오라브레이크", "오라의 효과가 반대가 된다.");
    }
}
