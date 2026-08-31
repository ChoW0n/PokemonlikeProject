using Microsoft.EntityFrameworkCore;

namespace PokemonBattle.Data;

//EF Core가 이 클래스를 보고 실제 DB 테이블을 자동으로 만들어줌
public class AppDbContext : DbContext
{
    public DbSet<UnlockedPokemon> UnlockedPokemons => Set<UnlockedPokemon>();

    public DbSet<PlayerRun> PlayerRuns => Set<PlayerRun>();

    public DbSet<UserPreset> UserPresets => Set<UserPreset>();

    public DbSet<UserAccount> Users => Set<UserAccount>();

    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreset>()
            .HasIndex(preset => new { preset.Username, preset.Name })
            .IsUnique();
    }
}
