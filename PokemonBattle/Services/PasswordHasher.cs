using System.Security.Cryptography;

namespace PokemonBattle.Services;

//비밀번호를 평문으로 저장하지 않고 안전하게 해시(암호화된 형태)로 변환
public static class PasswordHasher
{
    public static string Hash(string password) //회원가입 시: 비밀번호 -> 해시 문자열
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string stored) //로그인 시: 입력한 비밀번호가 맞는지 확인
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
