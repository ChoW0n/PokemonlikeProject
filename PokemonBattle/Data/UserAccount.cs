namespace PokemonBattle.Data;

//DB에 저장되는 계정 정보 (게임 데이터의 PokemonData 등과는 별개의 영역)
public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsAdmin { get; set; }
}
