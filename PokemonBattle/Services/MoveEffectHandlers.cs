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
        bool suppressSecondaryEffects = attacker.SelectedAbility == "우격다짐"
            || (!move.IsStatus && defender.SelectedAbility == "인분");
        int chanceMultiplier = attacker.SelectedAbility == "하늘의은총" ? 2 : 1;

        if (move.IsStatus && move.HealingPercent > 0)
        {
            int heal = attacker.MaxHp * move.HealingPercent / 100;
            attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
            await context.ShowMessage($"{attacker.Data.Name}은(는) HP를 회복했다!");
        }

        if (!suppressSecondaryEffects && move.AilmentName != "none" && !defender.IsFainted
            && context.Random.Next(100) < Math.Min(100, move.AilmentChance * chanceMultiplier))
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

                if (defender.SelectedAbility == "싱크로"
                    && move.AilmentName is "paralysis" or "poison" or "burn"
                    && attacker.Status == StatusCondition.None
                    && !attacker.IsImmuneToAilment(move.AilmentName))
                {
                    attacker.ApplyAilment(move.AilmentName);
                    await context.ShowMessage($"{defender.Data.Name}의 싱크로가 {attacker.Data.Name}에게 상태 이상을 옮겼다!");
                }
            }
        }

        int flinchChance = move.FlinchChance;
        if (!move.IsStatus && attacker.SelectedAbility == "악취") flinchChance = Math.Max(flinchChance, 10);
        if (!suppressSecondaryEffects && flinchChance > 0 && !defender.IsFainted
            && defender.SelectedAbility != "정신력"
            && context.Random.Next(100) < Math.Min(100, flinchChance * chanceMultiplier))
        {
            defender.Flinched = true;
        }

        if (!suppressSecondaryEffects && move.StatChanges.Count > 0
            && context.Random.Next(100) < Math.Min(100, move.StatChangeChance * chanceMultiplier))
        {
            foreach (var statChange in move.StatChanges)
            {
                var target = statChange.TargetsSelf ? attacker : defender;
                if (target.IsFainted) continue;

                int before = target.StatStages[statChange.Stat];
                target.ChangeStage(statChange.Stat, statChange.Change, causedByOpponent: !statChange.TargetsSelf);
                int after = target.StatStages[statChange.Stat];
                if (before == after) continue;

                string direction = after > before ? "상승했다" : "하락했다";
                await context.ShowMessage($"{target.Data.Name}의 {StatKor(statChange.Stat)}이(가) {direction}!");
                if (!statChange.TargetsSelf && after < before)
                {
                    string? reaction = target.TriggerStatDropAbility();
                    if (reaction != null) await context.ShowMessage(reaction);
                }
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