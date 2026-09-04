namespace PokemonBattle.Data;

public class BattleResult
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRivalBattle { get; set; }
    public bool IsLegendaryBattle { get; set; }
    public int RivalNumber { get; set; }
    public bool Won { get; set; }
    public string EndReason { get; set; } = "";
    public int Round { get; set; } = 1;
    public int Turns { get; set; }
    public double PlayerHpRatio { get; set; }
    public double EnemyHpRatio { get; set; }
    public int DifficultyAdjustment { get; set; }
    public double SkillRating { get; set; } = 1000;
    public int UnlockedCount { get; set; }
    public int RunSeq { get; set; }
}