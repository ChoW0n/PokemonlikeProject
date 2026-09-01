namespace PokemonBattle.Models;

public sealed class RunMetaState
{
    public List<string> LegacyIds { get; set; } = new();
    public List<string> PendingLegacyChoices { get; set; } = new();
    public int LegacyClaimsRemaining { get; set; }

    public string? BattlefieldImprintId { get; set; }
    public int BattlefieldImprintStage { get; set; }

    public string? RiskCovenantId { get; set; }
    public int RiskCovenantStage { get; set; }
    public bool RiskCovenantDecisionMade { get; set; }
    public bool RiskCovenantAccepted { get; set; }
    public int BonusLegacyClaims { get; set; }

    public List<StolenMoveRecord> StolenMoves { get; set; } = new();
    public List<StolenMoveOption> PendingStolenMoveChoices { get; set; } = new();

    public RunMetaState Clone()
    {
        return new RunMetaState
        {
            LegacyIds = LegacyIds.ToList(),
            PendingLegacyChoices = PendingLegacyChoices.ToList(),
            LegacyClaimsRemaining = LegacyClaimsRemaining,
            BattlefieldImprintId = BattlefieldImprintId,
            BattlefieldImprintStage = BattlefieldImprintStage,
            RiskCovenantId = RiskCovenantId,
            RiskCovenantStage = RiskCovenantStage,
            RiskCovenantDecisionMade = RiskCovenantDecisionMade,
            RiskCovenantAccepted = RiskCovenantAccepted,
            BonusLegacyClaims = BonusLegacyClaims,
            StolenMoves = StolenMoves.Select(move => move.Clone()).ToList(),
            PendingStolenMoveChoices = PendingStolenMoveChoices
                .Select(option => option.Clone())
                .ToList()
        };
    }
}

public sealed class StolenMoveRecord
{
    public int PokemonId { get; set; }
    public string MoveKey { get; set; } = "";

    public StolenMoveRecord Clone() => new()
    {
        PokemonId = PokemonId,
        MoveKey = MoveKey
    };
}

public sealed class StolenMoveOption
{
    public int SourcePokemonId { get; set; }
    public string MoveKey { get; set; } = "";

    public StolenMoveOption Clone() => new()
    {
        SourcePokemonId = SourcePokemonId,
        MoveKey = MoveKey
    };
}

public sealed record RunLegacyDefinition(
    string Id,
    string Name,
    string Description,
    RunLegacyEffect Effect);

public enum RunLegacyEffect
{
    FirstStrikePower,
    AfflictedTargetPower,
    HighHpDefense,
    EndTurnRecovery
}

public sealed record BattlefieldImprintDefinition(
    string Id,
    string Name,
    string Description,
    string Weather,
    string Field);

public sealed record RiskCovenantDefinition(
    string Id,
    string Name,
    string Description,
    string RewardDescription,
    int EnemyLevelBonus,
    int BonusLegacyClaims);

public static class RunMetaCatalog
{
    public static readonly IReadOnlyList<RunLegacyDefinition> Legacies =
    [
        new("first-strike", "선혈의 선봉",
            "선공으로 사용하는 공격 기술의 위력이 20% 증가합니다.",
            RunLegacyEffect.FirstStrikePower),
        new("affliction", "상처의 공명",
            "상태 이상에 걸린 상대를 공격할 때 위력이 20% 증가합니다.",
            RunLegacyEffect.AfflictedTargetPower),
        new("iron-vitality", "철의 생명력",
            "HP가 75% 이상일 때 받는 공격 피해가 20% 감소합니다.",
            RunLegacyEffect.HighHpDefense),
        new("last-breath", "마지막 불씨",
            "턴 종료 시 HP를 1/16 회복합니다.",
            RunLegacyEffect.EndTurnRecovery)
    ];

    public static readonly IReadOnlyList<BattlefieldImprintDefinition> BattlefieldImprints =
    [
        new("ember-wastes", "잿빛 황무지",
            "뜨거운 기류가 불꽃을 키우고 물의 힘을 약화합니다.",
            BattleWeather.Sun, BattleField.None),
        new("storm-garden", "폭풍의 정원",
            "전기가 흐르는 초원이 전기 기술을 강화합니다.",
            BattleWeather.Rain, BattleField.Electric),
        new("moonlit-marsh", "달빛 습지",
            "안개가 드리운 땅에서 드래곤의 기세가 꺾입니다.",
            BattleWeather.Clear, BattleField.Misty),
        new("iron-dust", "철가루 평원",
            "모래바람이 불어 바위·땅·강철의 힘이 솟습니다.",
            BattleWeather.Sand, BattleField.None)
    ];

    public static readonly IReadOnlyList<RiskCovenantDefinition> RiskCovenants =
    [
        new("blood-debt", "피의 빚",
            "이번 스테이지의 상대 레벨이 3 상승합니다.",
            "다음 승리에서 관록의 유산을 1개 더 선택할 수 있습니다.",
            3, 1)
    ];

    private static readonly HashSet<string> ExcludedStolenMoves = new(StringComparer.Ordinal)
    {
        "aeroblast", "spacial-rend", "roar-of-time", "shadow-force", "seed-flare",
        "judgment", "v-create", "blue-flare", "bolt-strike", "sacred-fire",
        "psycho-boost", "lunar-dance", "magma-storm", "dark-void", "hyperspace-hole",
        "oblivion-wing", "origin-pulse", "precipice-blades", "dragon-ascent",
        "thousand-arrows", "thousand-waves", "lands-wrath", "core-enforcer",
        "sunsteel-strike", "moongeist-beam", "mind-blown", "glacial-lance",
        "astral-barrage", "behemoth-blade", "behemoth-bash", "dynamax-cannon"
    };

    public static RunLegacyDefinition? Legacy(string id) =>
        Legacies.FirstOrDefault(legacy => legacy.Id == id);

    public static BattlefieldImprintDefinition? Battlefield(string? id) =>
        BattlefieldImprints.FirstOrDefault(imprint => imprint.Id == id);

    public static RiskCovenantDefinition? Covenant(string? id) =>
        RiskCovenants.FirstOrDefault(covenant => covenant.Id == id);

    public static bool IsStolenMoveEligible(string moveKey, int sourcePokemonId)
    {
        return IsStolenMoveKeyEligible(moveKey)
            && !EnemyTeamProvider.IsLegendary(sourcePokemonId);
    }

    public static bool IsStolenMoveKeyEligible(string moveKey) =>
        MoveDatabase.All.TryGetValue(moveKey, out var move)
        && !ExcludedStolenMoves.Contains(moveKey)
        && !move.IsStatus;

    public static RunMetaState Normalize(RunMetaState? state)
    {
        state ??= new RunMetaState();
        state.LegacyIds = state.LegacyIds
            .Where(id => Legacy(id) != null)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        state.PendingLegacyChoices = state.PendingLegacyChoices
            .Where(id => Legacy(id) != null && !state.LegacyIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .Take(3)
            .ToList();
        state.LegacyClaimsRemaining = Math.Clamp(state.LegacyClaimsRemaining, 0, 4);
        state.BattlefieldImprintId = Battlefield(state.BattlefieldImprintId)?.Id;
        state.BattlefieldImprintStage = Math.Max(0, state.BattlefieldImprintStage);
        state.RiskCovenantId = Covenant(state.RiskCovenantId)?.Id;
        state.RiskCovenantStage = Math.Max(0, state.RiskCovenantStage);
        state.BonusLegacyClaims = Math.Clamp(state.BonusLegacyClaims, 0, 2);
        state.StolenMoves = state.StolenMoves
            .Where(move => PokemonDatabase.All.ContainsKey(move.PokemonId)
                && IsStolenMoveKeyEligible(move.MoveKey))
            .GroupBy(move => $"{move.PokemonId}:{move.MoveKey}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        state.PendingStolenMoveChoices = state.PendingStolenMoveChoices
            .Where(option => PokemonDatabase.All.ContainsKey(option.SourcePokemonId)
                && IsStolenMoveEligible(option.MoveKey, option.SourcePokemonId))
            .GroupBy(option => $"{option.SourcePokemonId}:{option.MoveKey}", StringComparer.Ordinal)
            .Select(group => group.First())
            .Take(6)
            .ToList();
        return state;
    }
}