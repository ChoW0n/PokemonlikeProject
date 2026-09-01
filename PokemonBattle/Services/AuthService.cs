using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public class AuthService
{
    private readonly DatabaseContextExecutor _database;

    [ActivatorUtilitiesConstructor]
    public AuthService(DatabaseContextExecutor database)
    {
        _database = database;
    }

    public AuthService(AppDbContext db) : this(new DatabaseContextExecutor(db))
    {
    }

    public async Task<(bool success, string message)> Register(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "아이디와 비밀번호를 입력해주세요.");
        }
        if (password.Length < 6)
        {
            return (false, "비밀번호는 6자 이상 입력해주세요.");
        }

        try
        {
            return await _database.ExecuteAsync("auth.register", async db =>
            {
                bool exists = await db.Users.AnyAsync(u => u.Username == username);
                if (exists) return (false, "이미 사용 중인 아이디입니다.");

                db.Users.Add(new UserAccount
                {
                    Username = username,
                    PasswordHash = PasswordHasher.Hash(password),
                    IsAdmin = false
                });
                await db.SaveChangesAsync();

                return (true, "가입 완료! 로그인해주세요.");
            });
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return (false, "이미 사용 중인 아이디입니다.");
        }
    }

    public async Task<(bool success, string message, bool isAdmin)> Login(string username, string password)
    {
        return await _database.ExecuteAsync("auth.login", async db =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return (false, "존재하지 않는 아이디입니다.", false);
            if (!PasswordHasher.Verify(password, user.PasswordHash))
                return (false, "비밀번호가 올바르지 않습니다.", false);

            return (true, "로그인 성공!", user.IsAdmin);
        });
    }
}
