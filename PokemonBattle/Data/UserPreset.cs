namespace PokemonBattle.Data;

// 사용자별 팀 구성 프리셋. 실제 구성은 JSON 스냅샷으로 저장하며 런의 레벨과 분리된다.
public class UserPreset
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string LoadoutsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }
}