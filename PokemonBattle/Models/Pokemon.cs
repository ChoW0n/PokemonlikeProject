namespace PokemonBattle.Models;

//실전 배틀용 인스턴스: 도감 데이터를 기반으로 만들어지지만 HP는 배틀 중 변함
public class Pokemon
{
    public PokemonData Data; //원본 도감 데이터 참조
    public int CurrentHp; //현재 HP (깎이는 값)
    public bool IsFainted; //기절(사망) 여부

    public Pokemon(PokemonData data) //도감 데이터를 받아 배틀용 인스턴스 생성
    {
        Data = data;
        CurrentHp = data.BaseHp; //시작 HP는 도감 기본 HP와 동일
        IsFainted = false;
    }

    public void TakeDamage(int amount) //데미지 처리 및 기절 판정
    {
        CurrentHp -= amount;
        if (CurrentHp <= 0)
        {
            CurrentHp = 0;
            IsFainted = true;
        }
    }
}