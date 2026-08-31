namespace PokemonBattle.Models;

//한 라운드가 끝났을 때 저장하는 실력 지표
public class RunRoundPerformance
{
    public bool Cleared { get; set; }
    public double PlayerHpRatio { get; set; }
    public int Turns { get; set; }
}

public sealed record RunPerformanceSummary(
    int ClearedRounds,
    int TotalRounds,
    double AverageHpRatio,
    double AverageTurns,
    bool Won);