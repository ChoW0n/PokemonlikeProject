namespace PokemonBattle.Services;

//점수 저장 방식을 추상화하는 인터페이스. 지금은 메모리 저장이지만, 나중에 DB 구현체로 교체 가능
public interface IScoreStore
{
    int GetHighScore(); //저장된 최고 기록 조회
    void SaveIfHigher(int score); //현재 점수가 기존 최고 기록보다 높으면 갱신
}
