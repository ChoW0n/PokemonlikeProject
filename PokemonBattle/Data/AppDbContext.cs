using Microsoft.EntityFrameworkCore;

namespace PokemonBattle.Data;

//EF Core가 이 클래스를 보고 실제 DB 테이블을 자동으로 만들어줌
public class AppDbContext : DbContext
{
    public DbSet<UnlockedPokemon> UnlockedPokemons => Set<UnlockedPokemon>();

    public DbSet<PlayerRun> PlayerRuns => Set<PlayerRun>();

    public DbSet<UserAccount> Users => Set<UserAccount>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
