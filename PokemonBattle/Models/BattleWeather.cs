namespace PokemonBattle.Models;

//현재 배틀의 날씨 상태 (전역 하나만 존재, 가뭄/잔비 특성으로 배틀 시작 시 결정됨)
public static class BattleWeather
{
    public static string Current = "맑음"; //맑음 / 쾌청 / 비
}
