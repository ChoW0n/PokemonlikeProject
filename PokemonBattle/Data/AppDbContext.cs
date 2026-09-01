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

    public DbSet<PlayerSkillRating> PlayerSkillRatings => Set<PlayerSkillRating>();

    public DbSet<PlayerProgression> PlayerProgressions => Set<PlayerProgression>();

    public DbSet<MailboxMessage> MailboxMessages => Set<MailboxMessage>();

    public DbSet<TechnicalMachineInventory> TechnicalMachines => Set<TechnicalMachineInventory>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreset>()
            .HasIndex(preset => new { preset.Username, preset.Name })
            .IsUnique();

        modelBuilder.Entity<PlayerSkillRating>()
            .HasIndex(rating => rating.Username)
            .IsUnique();

        modelBuilder.Entity<PlayerProgression>()
            .HasIndex(profile => profile.Username)
            .IsUnique();

        modelBuilder.Entity<MailboxMessage>()
            .HasIndex(message => new { message.Username, message.DeduplicationKey })
            .IsUnique();

        modelBuilder.Entity<TechnicalMachineInventory>()
            .HasIndex(machine => new { machine.Username, machine.MoveKey })
            .IsUnique();
    }
}
