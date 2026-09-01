using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class RunMetaEffectHandler : IBattleEffectHandler
{
    public int Order => 275;

    public void ModifyPower(BattlePowerContext context)
    {
        var meta = context.RunMeta;
        if (meta == null) return;

        foreach (var legacyId in meta.LegacyIds)
        {
            var legacy = RunMetaCatalog.Legacy(legacyId);
            if (legacy == null) continue;

            switch (legacy.Effect)
            {
                case RunLegacyEffect.FirstStrikePower
                    when context.AttackerIsHero && context.AttackerMovedFirst:
                    context.Power *= 1.25;
                    break;
                case RunLegacyEffect.AfflictedTargetPower
                    when context.AttackerIsHero
                        && (context.Defender.Status != StatusCondition.None
                        || context.Defender.IsConfused):
                    context.Power *= 1.25;
                    break;
                case RunLegacyEffect.HighHpDefense
                    when !context.AttackerIsHero
                        && context.Defender.CurrentHp >= context.Defender.MaxHp * 0.75:
                    context.Power *= 0.75;
                    break;
                case RunLegacyEffect.LowHpOffense
                    when context.AttackerIsHero
                        && context.Attacker.CurrentHp <= context.Attacker.MaxHp * 0.25:
                    context.Power *= 1.2;
                    break;
                case RunLegacyEffect.WeatherPower
                    when context.AttackerIsHero
                        && BattleWeather.Current != BattleWeather.Clear:
                    context.Power *= 1.1;
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
        if (meta == null || context.Pokemon.IsFainted) return;
        if (!meta.LegacyIds.Contains("last-breath")) return;

        int before = context.Pokemon.CurrentHp;
        context.Pokemon.CurrentHp = Math.Min(
            context.Pokemon.MaxHp,
            context.Pokemon.CurrentHp + Math.Max(1, context.Pokemon.MaxHp / 12));
        if (context.Pokemon.CurrentHp > before)
        {
            await context.ShowMessage(
                $"{context.Pokemon.Data.Name}은(는) 마지막 불씨로 HP를 회복했다!", 900);
        }
    }
}