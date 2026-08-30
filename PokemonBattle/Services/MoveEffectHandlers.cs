using PokemonBattle.Models;

namespace PokemonBattle.Services;

/// <summary>
/// Applies effects described by Move without adding move-specific branches to the UI.
/// A new data-backed move effect can be introduced by adding another handler to the
/// BattleEngine handler list.
/// </summary>
public sealed class MoveEffectHandler : IBattleEffectHandler
{
    public int Order => 100;

    public async Task AfterDamageResultAsync(BattleEffectContext context)
    {
        if (context.Move.DrainPercent > 0 && !context.Attacker.IsFainted)
        {
            int heal = Math.Max(1, context.TotalDamage * context.Move.DrainPercent / 100);
            context.Attacker.CurrentHp = Math.Min(context.Attacker.MaxHp, context.Attacker.CurrentHp + heal);
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) HP를 흡수했다!");
        }
        else if (context.Move.DrainPercent < 0 && !context.Attacker.IsFainted)
        {
            int recoilDamage = Math.Max(1, context.TotalDamage * Math.Abs(context.Move.DrainPercent) / 100);
            context.Attacker.CurrentHp = Math.Max(0, context.Attacker.CurrentHp - recoilDamage);
            if (context.Attacker.CurrentHp == 0) context.Attacker.IsFainted = true;
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 반동으로 데미지를 입었다!");
        }
    }

    public async Task AfterMoveAsync(BattleEffectContext context)
    {
        var move = context.Move;
        var attacker = context.Attacker;
        var defender = context.Defender;

        if (move.IsStatus && move.HealingPercent > 0)
        {
            int heal = attacker.MaxHp * move.HealingPercent / 100;
            attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
            await context.ShowMessage($"{attacker.Data.Name}은(는) HP를 회복했다!");
        }

        if (move.AilmentName != "none" && !defender.IsFainted && context.Random.Next(100) < move.AilmentChance)
        {
            if (move.AilmentName == "confusion")
            {
                if (!defender.IsConfused && !defender.IsImmuneToConfusion())
                {
                    defender.ApplyConfusion();
                    await context.ShowMessage($"{defender.Data.Name}은(는) 혼란에 빠졌다!");
                }
            }
            else if (defender.Status == StatusCondition.None && !defender.IsImmuneToAilment(move.AilmentName))
            {
                defender.ApplyAilment(move.AilmentName);
                await context.ShowMessage($"{defender.Data.Name}은(는) {AilmentKor(move.AilmentName)} 상태가 되었다!");
            }
        }

        if (move.FlinchChance > 0 && !defender.IsFainted && context.Random.Next(100) < move.FlinchChance)
        {
            defender.Flinched = true;
        }

        if (move.StatChanges.Count > 0 && context.Random.Next(100) < move.StatChangeChance)
        {
            foreach (var statChange in move.StatChanges)
            {
                var target = statChange.TargetsSelf ? attacker : defender;
                if (target.IsFainted) continue;

                target.ChangeStage(statChange.Stat, statChange.Change);
                string direction = statChange.Change > 0 ? "상승했다" : "하락했다";
                await context.ShowMessage($"{target.Data.Name}의 {StatKor(statChange.Stat)}이(가) {direction}!");
            }
        }
    }

    private static string AilmentKor(string ailment) => ailment switch
    {
        "paralysis" => "마비",
        "poison" => "독",
        "burn" => "화상",
        "sleep" => "잠듦",
        "freeze" => "얼음",
        "confusion" => "혼란",
        _ => ailment
    };

    private static string StatKor(string stat) => stat switch
    {
        "attack" => "공격",
        "defense" => "방어",
        "special-attack" => "특공",
        "special-defense" => "특방",
        "speed" => "속도",
        _ => stat
    };
}