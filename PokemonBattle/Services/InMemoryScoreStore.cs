namespace PokemonBattle.Services;

//IScoreStore의 메모리 기반 구현체: 서버가 켜져있는 동안만 최고 기록 유지
public class InMemoryScoreStore : IScoreStore
{
    private int _highScore; //메모리에만 저장되는 최고 기록 (서버 재시작 시 초기화됨)

    public int GetHighScore() //저장된 최고 기록 반환
    {
        return _highScore;
    }

    public void SaveIfHigher(int score) //새 점수가 더 높을 때만 갱신
    {
        if (score > _highScore)
        {
            _highScore = score;
        }
    }
}
