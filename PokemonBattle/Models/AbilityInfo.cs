namespace PokemonBattle.Models;

//특성 이름 + 설명. DataGen이 덮어쓰는 AbilityDatabase.cs와 분리해서, 재실행해도 이 클래스 정의는 안전하게 유지됨
public class AbilityInfo
{
    public string Name;
    public string Description;

    public AbilityInfo(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
