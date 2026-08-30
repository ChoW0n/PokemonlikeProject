namespace PokemonBattle.Models;

//주요 상태이상 (혼란은 별도 - 다른 상태이상과 동시에 걸릴 수 있어서 따로 관리)
public enum StatusCondition { None, Burn, Poison, Paralysis, Sleep, Freeze }
