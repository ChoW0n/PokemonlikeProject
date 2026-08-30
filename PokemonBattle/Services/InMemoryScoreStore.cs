namespace PokemonBattle.Services;

//IScoreStore의 메모리 기반 구현체: GameState의 현재 회로에서만 최고 기록을 계산하는 보조 저장소
public class InMemoryScoreStore : IScoreStore
{
    private int _highScore;

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
