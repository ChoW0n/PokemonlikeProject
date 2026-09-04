namespace PokemonBattle.Data;

//유저별 실력 평점. PlayerRuns와 분리해 런을 초기화해도 누적 실력을 보존한다.
public class PlayerSkillRating
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public double Rating { get; set; } = 1000;
    public int CompletedRuns { get; set; }
    public double PeakRating { get; set; } = 1000;
    public int PeakRound { get; set; }
    public DateTimeOffset? PeakAchievedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}