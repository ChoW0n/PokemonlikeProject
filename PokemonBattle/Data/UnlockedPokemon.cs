namespace PokemonBattle.Data;

//어떤 유저가 어떤 포켓몬을 해금했는지 기록하는 테이블
public class UnlockedPokemon
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int PokemonId { get; set; }
}
