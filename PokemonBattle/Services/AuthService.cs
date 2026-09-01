using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
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

        bool exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists) return (false, "이미 사용 중인 아이디입니다.");

        _db.Users.Add(new UserAccount
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            IsAdmin = false
        });
        await _db.SaveChangesAsync();

        return (true, "가입 완료! 로그인해주세요.");
    }

    public async Task<(bool success, string message, bool isAdmin)> Login(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return (false, "존재하지 않는 아이디입니다.", false);
        if (!PasswordHasher.Verify(password, user.PasswordHash)) return (false, "비밀번호가 올바르지 않습니다.", false);

        return (true, "로그인 성공!", user.IsAdmin);
    }
}
