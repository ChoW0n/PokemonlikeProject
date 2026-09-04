namespace PokemonBattle.Data;

public class BattleResult
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRivalBattle { get; set; }
    public int RivalNumber { get; set; }
    public bool Won { get; set; }
    public int Round { get; set; } = 1;
    public int Turns { get; set; }
    public double PlayerHpRatio { get; set; }
    public int DifficultyAdjustment { get; set; }
    public double SkillRating { get; set; } = 1000;
}