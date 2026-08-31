namespace PokemonBattle.Data;

public class AdminAuditLog
{
    public int Id { get; set; }
    public string AdminUsername { get; set; } = "";
    public string Action { get; set; } = "";
    public string TargetUsername { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
}