namespace PokemonBattle.Services;

//전투 종료 시 새 점수가 기존 최고 기록보다 높은지 계산하는 보조 인터페이스.
//사용자별 최고 기록의 영속화는 RunStore가 담당한다.
public interface IScoreStore
{
    int GetHighScore(); //현재 회로에서 계산된 최고 기록 조회
    void SaveIfHigher(int score); //현재 점수가 현재 회로의 최고 기록보다 높으면 갱신
}
