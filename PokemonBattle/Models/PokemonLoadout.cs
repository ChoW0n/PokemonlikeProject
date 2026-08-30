namespace PokemonBattle.Models;

public class PokemonLoadout
{
    public int PokemonId;
    public List<string> ChosenMoveNames = new();
    public string ChosenAbility = "";
    public string ChosenItem = "없음";
    public int Level = 1; //이 런(run) 안에서 누적된 레벨. 승리할 때마다 오르고, 패배하면 초기화됨
}
