namespace PokemonBattle.Data;

//계정별로 포켓몬의 승리 기여 횟수를 영구 저장하는 테이블
public class PokemonMastery
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public int PokemonId { get; set; }
    public int VictoryContributions { get; set; }
}