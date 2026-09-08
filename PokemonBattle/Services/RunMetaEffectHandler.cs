using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class RunMetaEffectHandler : IBattleEffectHandler
{
    public int Order => 275;

    public void ModifyPower(BattlePowerContext context)
    {
        var meta = context.RunMeta;
        if (meta == null) return;

        // 같은 유산은 종류별로 묶어 현재 중첩 단계의 배율 하나만 적용한다.
        foreach (var legacyGroup in meta.LegacyIds.GroupBy(id => id, StringComparer.Ordinal))
        {
            var legacy = RunMetaCatalog.Legacy(legacyGroup.Key);
            if (legacy == null) continue;
            int stackStage = Math.Min(
                legacyGroup.Count(),
                legacy.StackMultipliers.Count);
            if (stackStage == 0) continue;
            double stackMultiplier = legacy.StackMultipliers[stackStage - 1];

            switch (legacy.Effect)
            {
                case RunLegacyEffect.FirstStrikePower
                    when context.AttackerIsHero && context.AttackerMovedFirst:
                    context.Power *= stackMultiplier;
                    break;
                case RunLegacyEffect.AfflictedTargetPower
                    when context.AttackerIsHero
                        && (context.Defender.Status != StatusCondition.None
                        || context.Defender.IsConfused):
                    context.Power *= stackMultiplier;
                    break;
                case RunLegacyEffect.HighHpDefense
                    when !context.AttackerIsHero
                        && context.Defender.CurrentHp >= context.Defender.MaxHp * 0.75:
                    context.Power *= stackMultiplier;
                    break;
                case RunLegacyEffect.LowHpOffense
                    when context.AttackerIsHero
                        && context.Attacker.CurrentHp <= context.Attacker.MaxHp * 0.25:
                    context.Power *= stackMultiplier;
                    break;
                case RunLegacyEffect.WeatherPower
                    when context.AttackerIsHero
                        && BattleWeather.Current != BattleWeather.Clear:
                    context.Power *= stackMultiplier;
                    break;
            }
        }

        if (!context.AttackerIsHero
            && meta.RiskCovenantAccepted
            && meta.RiskCovenantId == "dark-pact"
            && !string.IsNullOrWhiteSpace(context.Attacker.SelectedAbility)
            && !context.Attacker.IsAbilitySuppressedBy(context.Defender))
        {
            context.Power *= 1.15;
        }
    }

    public async Task EndOfTurnAsync(BattleEndOfTurnContext context)
    {
        var meta = context.RunMeta;
        if (meta == null || context.Pokemon.IsFainted || !context.IsHero) return;
        if (!meta.LegacyIds.Contains("last-breath")) return;

        int before = context.Pokemon.CurrentHp;
        context.Pokemon.CurrentHp = Math.Min(
            context.Pokemon.MaxHp,
            context.Pokemon.CurrentHp + Math.Max(1, context.Pokemon.MaxHp / 16));
        if (context.Pokemon.CurrentHp > before)
        {
            await context.ShowMessage(
                $"{context.Pokemon.Data.Name}은(는) 마지막 불씨로 HP를 회복했다!", 900);
        }
    }
}