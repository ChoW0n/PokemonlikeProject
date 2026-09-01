namespace PokemonBattle.Data;

//유저별 진행 중인 런(현재 점수 + 최고 기록 + 팀 구성/레벨)을 통째로 백업해두는 테이블
public class PlayerRun
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int CurrentScore { get; set; }
    public int HighScore { get; set; }
    public string LoadoutsJson { get; set; } = "[]"; //팀 구성 전체를 JSON 문자열로 직렬화해서 저장 (간단하고 안전한 방식)
    public int LegendaryProgressPercent { get; set; }
    public string LegendaryEncounterHistoryJson { get; set; } = "[]";
    public int DifficultyAdjustment { get; set; }
    public string RoundPerformancesJson { get; set; } = "[]";
    public string RunMetaStateJson { get; set; } = "{}";
}