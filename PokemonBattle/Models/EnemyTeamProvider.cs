namespace PokemonBattle.Models;

public static class EnemyTeamProvider
{
    private static readonly Random rng = new Random();

    private static readonly HashSet<string> OffensiveAbilities = new()
    {
        "근성", "독폭주", "우격다짐", "이판사판", "철주먹", "테크니션",
        "색안경", "노가드", "의욕", "적응력", "자기과신", "천하장사",
        "순수한힘", "단단한발톱", "메가런처", "옹골찬턱", "프리즈스킨",
        "페어리스킨", "스나이퍼", "투쟁심", "승기", "피뢰침", "마중물",
        "선파워", "맹화", "급류", "벌레의알림", "가뭄", "모래의힘",
        "다운로드"
    };

    private static readonly HashSet<string> PhysicalOffensiveAbilities = new()
    {
        "근성", "독폭주", "우격다짐", "이판사판", "철주먹", "테크니션",
        "의욕", "천하장사", "순수한힘", "단단한발톱", "옹골찬턱", "투쟁심"
    };

    private static readonly HashSet<string> SpecialOffensiveAbilities = new()
    {
        "색안경", "적응력", "승기", "피뢰침", "마중물", "선파워", "맹화",
        "급류", "벌레의알림", "모래의힘", "다운로드", "메가런처"
    };

    private static readonly HashSet<string> DefensiveAbilities = new()
    {
        "멀티스케일", "재생력", "포이즌힐", "매직가드", "옹골참", "필터",
        "하드록", "퍼코트", "이상한비늘", "두꺼운지방", "내열", "저수",
        "축전", "자연회복", "촉촉바디", "아이스바디", "수의베일", "면역",
        "불면", "마이페이스", "유연", "조가비갑옷", "전투무장", "방진",
        "부유", "에어록", "날씨부정", "풀모피", "하얀연기", "클리어바디",
        "괴력집게", "방탄", "불굴의마음"
    };

    //전설/환상 포켓몬 도감번호. 진행률 100% 달성 전까지 일반 랜덤 조우에서 제외
    private static readonly HashSet<int> LegendaryIds = new()
    {
        144,145,146,150,151,243,244,245,249,250,251,377,378,379,380,381,382,383,384,385,386,
        480,481,482,483,484,485,486,487,488,489,490,491,492,493,494,
        638,639,640,641,642,643,644,645,646,647,648,649,716,717,718
    };

    public static bool IsLegendary(int pokemonId) => LegendaryIds.Contains(pokemonId);

    public static bool ContainsLegendary(IEnumerable<int> pokemonIds) =>
        pokemonIds.Any(IsLegendary);

    public static int GetTeamSizeForRound(int round)
    {
        int safeRound = Math.Max(1, round);
        return Math.Clamp(1 + (safeRound - 1) / 2, 1, 6);
    }

    private static readonly HashSet<int> FirstStageIds = new()
    {
        1, 4, 7, 10, 13, 16, 19, 21, 23, 25, 27, 29, 32, 35, 37, 39, 41, 43, 46, 48,
        50, 52, 54, 56, 58, 60, 63, 66, 69, 72, 74, 77, 79, 81, 84, 86, 88, 90, 92,
        95, 96, 98, 100, 102, 104, 108, 109, 111, 116, 118, 120, 129, 133, 138, 140,
        147, 152, 155, 158, 161, 163, 165, 167, 170, 172, 173, 174, 175, 177, 179,
        183, 187, 190, 191, 194, 198, 200, 209, 211, 213, 214, 216, 218, 220, 223,
        225, 227, 228, 231, 234, 236, 238, 239, 240, 246, 252, 255, 258, 261, 263,
        265, 270, 273, 276, 278, 280, 285, 287, 290, 293, 296, 298, 300, 302, 304,
        309, 311, 312, 313, 314, 316, 318, 320, 322, 324, 325, 327, 328, 331, 333,
        335, 336, 337, 338, 339, 341, 343, 345, 347, 349, 351, 352, 353, 355, 357,
        358, 359, 360, 361, 363, 366, 368, 369, 370, 371, 374, 387, 390, 393, 396,
        399, 401, 403, 406, 408, 410, 412, 415, 417, 418, 420, 422, 425, 427, 428,
        431, 433, 434, 436, 438, 439, 440, 443, 446, 447, 449, 451, 453, 455, 456,
        459, 461, 462, 463, 464, 466, 467, 469, 470, 471, 472, 473, 474, 475, 476,
        477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491,
        492, 493, 495, 498, 501, 504, 506, 509, 511, 513, 515, 517, 519, 522, 524,
        527, 529, 531, 532, 535, 538, 539, 540, 543, 546, 548, 550, 551, 554, 557,
        559, 562, 564, 566, 568, 570, 572, 574, 577, 580, 582, 585, 587, 588, 590,
        592, 594, 595, 597, 599, 602, 605, 607, 610, 613, 615, 616, 618, 619, 621,
        622, 624, 626, 627, 629, 631, 632, 633, 636, 641, 642, 645, 646, 647, 648,
        649, 650, 653, 656, 659, 661, 664, 667, 669, 672, 674, 677, 679, 682, 684,
        686, 688, 690, 692, 694, 696, 698, 700, 701, 702, 703, 704, 707, 708, 710,
        712, 714, 716, 717, 718, 719, 720, 721
    };

    public static List<KeyValuePair<int, PokemonData>> GetRandomTeam(
        int count,
        int poolSize,
        bool firstStageOnly,
        HashSet<int> excludeIds,
        bool legendaryUnlocked = false,
        int round = 1,
        int skillAdjustment = 0)
    {
        var candidates = PokemonDatabase.All
            .Where(p => p.Key <= poolSize)
            .Where(p => !excludeIds.Contains(p.Key))
            .Where(p => legendaryUnlocked || !LegendaryIds.Contains(p.Key));

        if (firstStageOnly)
        {
            candidates = candidates.Where(p => FirstStageIds.Contains(p.Key));
        }

        var pool = candidates.ToList();

        if (pool.Count < count)
        {
            pool = PokemonDatabase.All
                .Where(p => p.Key <= poolSize)
                .Where(p => !excludeIds.Contains(p.Key))
                .Where(p => legendaryUnlocked || !LegendaryIds.Contains(p.Key))
                .ToList();
            if (firstStageOnly) pool = pool.Where(p => FirstStageIds.Contains(p.Key)).ToList();
        }

        if (pool.Count == 0 || count <= 0) return new();

        //라운드와 레이팅이 낮을 때는 거의 균등하게 뽑아 약한 종도 계속 등장하게 한다.
        //진행할수록 종족값 상위 후보의 가중치가 커지지만, 최고 후보만 고정하지는 않는다.
        int minBaseStatTotal = pool.Min(entry => GetBaseStatTotal(entry.Value));
        int maxBaseStatTotal = pool.Max(entry => GetBaseStatTotal(entry.Value));
        var remaining = pool.ToList();
        var team = new List<KeyValuePair<int, PokemonData>>(Math.Min(count, pool.Count));
        bool legendaryAlreadyChosen = false;

        while (team.Count < count && remaining.Count > 0)
        {
            var eligible = legendaryUnlocked && legendaryAlreadyChosen
                ? remaining.Where(entry => !LegendaryIds.Contains(entry.Key)).ToList()
                : remaining;
            if (eligible.Count == 0) break;

            var chosen = WeightedPick(
                eligible,
                entry => GetSpeciesSelectionWeight(
                    entry.Value,
                    minBaseStatTotal,
                    maxBaseStatTotal,
                    round,
                    skillAdjustment));
            team.Add(chosen);
            remaining.Remove(chosen);
            legendaryAlreadyChosen |= LegendaryIds.Contains(chosen.Key);
        }

        return team.OrderBy(_ => rng.Next()).ToList();
    }

    public static int GetBaseStatTotal(PokemonData data) =>
        data.BaseHp + data.BaseAtk + data.BaseDef
        + data.BaseSpAtk + data.BaseSpDef + data.BaseSpd;

    public static double GetSpeciesSelectionWeight(
        PokemonData data,
        int poolMinimumBaseStatTotal,
        int poolMaximumBaseStatTotal,
        int round = 1,
        int skillAdjustment = 0)
    {
        double normalized = poolMaximumBaseStatTotal <= poolMinimumBaseStatTotal
            ? 0.5
            : Math.Clamp(
                (GetBaseStatTotal(data) - poolMinimumBaseStatTotal)
                    / (double)(poolMaximumBaseStatTotal - poolMinimumBaseStatTotal),
                0,
                1);

        int safeRound = Math.Max(1, round);
        double roundPressure = Math.Clamp((safeRound - 1) / 12.0, 0, 1);
        double ratingPressure = Math.Clamp(skillAdjustment / 5.0, -0.35, 1);
        double pressure = Math.Clamp(
            0.25 + roundPressure * 0.8 + ratingPressure * 0.45,
            0.2,
            1.7);

        return 1 + Math.Pow(normalized, 1.7) * pressure;
    }

    //프로급 기술 선택: 자속(STAB) 우선 + 서로 다른 속성으로 커버리지 확보 + 변화기 최소 1개 포함
    public static List<string> PickProMoveset(PokemonData data)
    {
        var candidates = data.MoveNames
            .Where(MoveDatabase.All.ContainsKey)
            .Select(k => (Key: k, Move: MoveDatabase.All[k]))
            .ToList();

        double Score((string Key, Move Move) c)
        {
            var m = c.Move;
            bool stab = m.Type == data.Type1 || data.Type2 == m.Type;

            if (m.IsStatus)
            {
                double s = 25;
                if (m.StatChanges.Count > 0) s += 15;
                if (m.AilmentName != "none") s += 15;
                if (m.Priority > 0) s += 5;
                return s;
            }

            double acc = m.AlwaysHits ? 100 : m.Accuracy;
            double power = m.Power * (stab ? 1.5 : 1.0) * (acc / 100.0);
            if (m.Priority > 0) power += 10;
            return power;
        }

        var ranked = candidates.OrderByDescending(Score).ToList();
        var chosen = new List<string>();
        var usedTypes = new HashSet<PokemonType>();

        //1차: 서로 다른 속성의 강력한 공격기 위주로 최대 3개
        foreach (var c in ranked)
        {
            if (chosen.Count >= 3) break;
            if (c.Move.IsStatus) continue;
            if (usedTypes.Contains(c.Move.Type)) continue;
            chosen.Add(c.Key);
            usedTypes.Add(c.Move.Type);
        }

        //2차: 변화기(상태이상/랭크업 등) 하나 확보 시도
        var statusPick = ranked.FirstOrDefault(c => c.Move.IsStatus && !chosen.Contains(c.Key));
        if (statusPick.Key != null && chosen.Count < 4)
        {
            chosen.Add(statusPick.Key);
        }

        //3차: 그래도 4개 안 차면 점수순으로 채움
        foreach (var c in ranked)
        {
            if (chosen.Count >= 4) break;
            if (!chosen.Contains(c.Key)) chosen.Add(c.Key);
        }

        return chosen.Take(4).ToList();
    }

    public static string PickProAbility(
        PokemonData data,
        IEnumerable<string> moveKeys,
        int skillAdjustment = 0)
    {
        var abilities = data.AbilityNames
            .Where(AbilityDatabase.IsImplemented)
            .Distinct()
            .ToList();

        if (abilities.Count == 0)
        {
            return data.AbilityNames.FirstOrDefault() ?? "";
        }

        var profile = AnalyzeMoveset(moveKeys);
        return WeightedPick(
            abilities,
            ability => AbilityWeight(ability, profile, skillAdjustment));
    }

    public static string PickProItem(
        IEnumerable<string> moveKeys,
        IEnumerable<Item> availableItems,
        ISet<string>? usedItemNames = null,
        int skillAdjustment = 0)
    {
        var available = availableItems
            .Where(item => usedItemNames == null || !usedItemNames.Contains(item.Name))
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (available.Count == 0)
        {
            return "없음";
        }

        var profile = AnalyzeMoveset(moveKeys);
        var weightedItems = available
            .Select(item => (
                Item: item,
                Weight: ItemWeight(item.Name, profile, skillAdjustment)))
            .Where(item => item.Weight > 0)
            .ToList();

        return weightedItems.Count == 0
            ? "없음"
            : WeightedPick(weightedItems, item => item.Weight).Item.Name;
    }

    private static double AbilityWeight(
        string ability,
        MoveProfile profile,
        int skillAdjustment)
    {
        double weight = 1.0;
        bool physicalFocused = profile.PhysicalCount > profile.SpecialCount;
        bool specialFocused = profile.SpecialCount > profile.PhysicalCount;

        if (OffensiveAbilities.Contains(ability))
        {
            weight += 2.0;
        }

        if (physicalFocused && PhysicalOffensiveAbilities.Contains(ability))
        {
            weight += 2.5;
        }

        if (specialFocused && SpecialOffensiveAbilities.Contains(ability))
        {
            weight += 2.5;
        }

        if (DefensiveAbilities.Contains(ability))
        {
            weight += profile.IsSurvivalFocused ? 3.5 : 0.5;
        }

        if (profile.HasStatusMove && ability == "짓궂은마음")
        {
            weight += 3.0;
        }

        return ApplySkillAdjustment(weight, skillAdjustment);
    }

    private static double ItemWeight(
        string itemName,
        MoveProfile profile,
        int skillAdjustment)
    {
        if (itemName is "구애머리띠" or "구애안경" or "구애스카프")
        {
            if (profile.HasStatusMove)
            {
                return 0;
            }

            if (profile.IsPurePhysical)
            {
                return ApplySkillAdjustment(itemName switch
                {
                    "구애머리띠" => 10,
                    "구애스카프" => 6,
                    _ => 0
                }, skillAdjustment);
            }

            if (profile.IsPureSpecial)
            {
                return ApplySkillAdjustment(itemName switch
                {
                    "구애안경" => 10,
                    "구애스카프" => 6,
                    _ => 0
                }, skillAdjustment);
            }

            return 0;
        }

        if (profile.HasStatusMove)
        {
            return ApplySkillAdjustment(itemName switch
            {
                "생명의구슬" => 7,
                "기합의띠" => 6,
                "먹다남은음식" => 4,
                "자뭉열매" or "오랭열매" or "무화열매" or "리샘열매" => 2.5,
                "없음" => 0.5,
                _ => 1
            }, skillAdjustment);
        }

        if (profile.IsPurePhysical || profile.IsPureSpecial)
        {
            return ApplySkillAdjustment(itemName switch
            {
                "생명의구슬" => 4,
                "기합의띠" => 3,
                "먹다남은음식" => 2,
                "없음" => 0.5,
                _ => 1
            }, skillAdjustment);
        }

        return ApplySkillAdjustment(itemName switch
        {
            "생명의구슬" => 4,
            "기합의띠" => 3,
            "먹다남은음식" => 2,
            "없음" => 0.5,
            _ => 1
        }, skillAdjustment);
    }

    private static double ApplySkillAdjustment(double weight, int skillAdjustment)
    {
        if (weight <= 0) return weight;

        int boundedAdjustment = Math.Clamp(skillAdjustment, -3, 5);
        double skillFactor = 1 + boundedAdjustment * 0.2;
        return 1 + (weight - 1) * skillFactor;
    }

    private static MoveProfile AnalyzeMoveset(IEnumerable<string> moveKeys)
    {
        var moves = moveKeys
            .Select(key => MoveDatabase.All.TryGetValue(key, out var move) ? move : null)
            .Where(move => move != null)
            .Cast<Move>()
            .ToList();

        int physicalCount = moves.Count(move => !move.IsStatus && !move.IsSpecial && move.Power > 0);
        int specialCount = moves.Count(move => !move.IsStatus && move.IsSpecial && move.Power > 0);
        bool hasStatus = moves.Any(move => move.IsStatus);
        bool hasRecovery = moves.Any(move => move.HealingPercent > 0 || move.DrainPercent > 0);

        return new MoveProfile(
            physicalCount,
            specialCount,
            hasStatus,
            hasRecovery);
    }

    private static T WeightedPick<T>(IReadOnlyList<T> values, Func<T, double> weightSelector)
    {
        double totalWeight = values.Sum(value => weightSelector(value));
        double roll = rng.NextDouble() * totalWeight;

        foreach (var value in values)
        {
            roll -= weightSelector(value);
            if (roll <= 0) return value;
        }

        return values[^1];
    }

    private sealed record MoveProfile(
        int PhysicalCount,
        int SpecialCount,
        bool HasStatusMove,
        bool HasRecoveryMove)
    {
        public bool IsPurePhysical =>
            !HasStatusMove && PhysicalCount > 0 && SpecialCount == 0;

        public bool IsPureSpecial =>
            !HasStatusMove && SpecialCount > 0 && PhysicalCount == 0;

        public bool IsSurvivalFocused =>
            HasStatusMove || HasRecoveryMove || PhysicalCount + SpecialCount <= 1;
    }
}
