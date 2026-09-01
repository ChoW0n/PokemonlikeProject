using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

//현재 로그인한 유저의 포켓몬 해금 상태를 관리
public class UnlockService
{
    private readonly DatabaseContextExecutor _database;
    private readonly CurrentUserService _currentUser;

    //처음 가입한 유저에게 기본으로 쥐어줄 스타터 3마리 (이상해씨/파이리/꼬부기 도감번호)
    private static readonly int[] StarterIds = { 1, 4, 7 };

    [ActivatorUtilitiesConstructor]
    public UnlockService(DatabaseContextExecutor database, CurrentUserService currentUser)
    {
        _database = database;
        _currentUser = currentUser;
    }

    public UnlockService(AppDbContext db, CurrentUserService currentUser)
        : this(new DatabaseContextExecutor(db), currentUser)
    {
    }

    public async Task<HashSet<int>> GetUnlockedIds() //로그인한 유저가 해금한 포켓몬 도감번호 전체 조회
    {
        if (!_currentUser.IsLoggedIn) return new HashSet<int>();

        return await _database.ExecuteAsync("unlocks.load", async db =>
        {
            var owned = await db.UnlockedPokemons
                .Where(u => u.Username == _currentUser.Username)
                .Select(u => u.PokemonId)
                .ToListAsync();

            if (owned.Count < StarterIds.Length) //해금한 포켓몬이 3마리 미만인 신규 유저에게 스타터 보충
            {
                await EnsureStarters(db, _currentUser.Username!, owned);
                owned = owned
                    .Concat(StarterIds)
                    .Distinct()
                    .ToList();
            }

            return owned.ToHashSet();
        });
    }

    private static async Task EnsureStarters(
        AppDbContext db,
        string username,
        IEnumerable<int> ownedIds)
    {
        var owned = ownedIds.ToHashSet();
        foreach (var id in StarterIds)
        {
            if (!owned.Contains(id))
            {
                db.UnlockedPokemons.Add(new UnlockedPokemon { Username = username, PokemonId = id });
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task Unlock(int pokemonId) //특정 포켓몬 해금 (승리 보상, 진화 해금 등에서 호출)
    {
        if (!_currentUser.IsLoggedIn) return;

        await _database.ExecuteAsync("unlocks.unlock", async db =>
        {
            bool already = await db.UnlockedPokemons
                .AnyAsync(u => u.Username == _currentUser.Username && u.PokemonId == pokemonId);
            if (already) return;

            db.UnlockedPokemons.Add(new UnlockedPokemon
            {
                Username = _currentUser.Username!,
                PokemonId = pokemonId
            });
            await db.SaveChangesAsync();
        });
    }
}
