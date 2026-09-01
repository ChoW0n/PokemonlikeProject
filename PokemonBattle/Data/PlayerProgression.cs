namespace PokemonBattle.Data;

public class PlayerProgression
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int CompletedBattles { get; set; }
    public bool RivalPending { get; set; }
    public int RivalNumber { get; set; }
    public string LatestLoadoutsJson { get; set; } = "[]";
    public string MovePreferencesJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class MailboxMessage
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DeduplicationKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TechnicalMachineInventory
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string MoveKey { get; set; } = "";
    public int Quantity { get; set; }
}