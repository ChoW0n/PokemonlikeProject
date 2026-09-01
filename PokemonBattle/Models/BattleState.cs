namespace PokemonBattle.Models;

public enum BattleGimmickKind
{
    None,
    MegaEvolution,
    Dynamax,
    Gigantamax,
    Terastal
}

/// <summary>
/// 상태가 포켓몬이 아니라 팀 전체에 귀속되는 전투 상태.
/// 1v1에서도 교체 후 유지되어야 하는 장애물과 기믹 사용 여부를 보관한다.
/// </summary>
public sealed class BattleSideState
{
    public bool StealthRock { get; private set; }
    public int SpikesLayers { get; private set; }
    public int ToxicSpikesLayers { get; private set; }
    public bool StickyWeb { get; private set; }
    public bool GimmickUsed { get; private set; }

    public bool HasHazards =>
        StealthRock || SpikesLayers > 0 || ToxicSpikesLayers > 0 || StickyWeb;

    public void SetStealthRock() => StealthRock = true;
    public bool AddSpikes()
    {
        if (SpikesLayers >= 3) return false;
        SpikesLayers++;
        return true;
    }

    public bool AddToxicSpikes()
    {
        if (ToxicSpikesLayers >= 2) return false;
        ToxicSpikesLayers++;
        return true;
    }

    public void SetStickyWeb() => StickyWeb = true;

    public void ClearHazards()
    {
        StealthRock = false;
        SpikesLayers = 0;
        ToxicSpikesLayers = 0;
        StickyWeb = false;
    }

    public void ClearToxicSpikes() => ToxicSpikesLayers = 0;

    public bool TryUseGimmick()
    {
        if (GimmickUsed) return false;
        GimmickUsed = true;
        return true;
    }

    public void Reset()
    {
        ClearHazards();
        GimmickUsed = false;
    }
}