namespace PokemonBattle.Services;

//현재 이 화면(탭)에 로그인한 유저 정보. Scoped라서 유저마다 각자 따로 가짐
public class CurrentUserService
{
    public string? Username { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsLoggedIn => Username != null;

    public void SignIn(string username, bool isAdmin)
    {
        Username = username;
        IsAdmin = isAdmin;
    }

    public void SignOut()
    {
        Username = null;
        IsAdmin = false;
    }
}
