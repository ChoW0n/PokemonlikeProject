namespace PokemonBattle.Models;

//게임의 현재 화면 상태를 나타내는 enum
public enum GameScreen
{
    Start,        //타이틀 화면
    EnemyPreview, //상대 라인업 미리보기
    TeamSelect,   //포켓몬 도감/선택 화면
    Battle,       //전투 화면
    Result        //승패 결과 화면
}
