using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class DamageModifierEffectHandler : IBattleEffectHandler
{
    public int Order => 300;

    public void ModifyPower(BattlePowerContext context)
    {
        var attacker = context.Attacker;
        var move = context.Move;

        if (TypeBoostItem(attacker.HeldItem) == move.Type) context.Power *= 1.2;
        if (attacker.HeldItem == "구애머리띠" && !move.IsSpecial) context.Power *= 1.5;
        if (attacker.HeldItem == "힘의머리띠" && !move.IsSpecial) context.Power *= 1.1;
        if (attacker.HeldItem == "구애안경" && move.IsSpecial) context.Power *= 1.5;
        if (attacker.HeldItem == "생명의구슬") context.Power *= 1.3;
        if (attacker.SelectedAbility == "타오르는불꽃" && attacker.FlashFireActive && move.Type == PokemonType.Fire)
        {
            context.Power *= 1.5;
        }
        if (attacker.SelectedAbility == "테크니션" && move.Power <= 60) context.Power *= 1.5;

        if (BattleWeather.Current == "쾌청")
        {
            if (move.Type == PokemonType.Fire) context.Power *= 1.5;
            if (move.Type == PokemonType.Water) context.Power *= 0.5;
        }
        else if (BattleWeather.Current == "비")
        {
            if (move.Type == PokemonType.Water) context.Power *= 1.5;
            if (move.Type == PokemonType.Fire) context.Power *= 0.5;
        }
    }

    public Task AfterDamageResultAsync(BattleEffectContext context)
    {
        var attacker = context.Attacker;
        if (attacker.HeldItem != "생명의구슬" || attacker.IsFainted) return Task.CompletedTask;

        int recoil = Math.Max(1, attacker.MaxHp / 10);
        attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - recoil);
        if (attacker.CurrentHp == 0) attacker.IsFainted = true;
        return Task.CompletedTask;
    }

    public async Task EndOfTurnAsync(BattleEndOfTurnContext context)
    {
        var pokemon = context.Pokemon;
        if (pokemon.HeldItem != "먹다남은음식" || pokemon.IsFainted) return;

        int heal = Math.Max(1, pokemon.MaxHp / 16);
        int before = pokemon.CurrentHp;
        pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + heal);
        if (pokemon.CurrentHp > before)
        {
            await context.ShowMessage($"{pokemon.Data.Name}은(는) {pokemon.HeldItem} 효과로 HP를 회복했다!", 1100);
        }
    }

    private static PokemonType? TypeBoostItem(string item) => item switch
    {
        "검은안경" => PokemonType.Dark,
        "신비의물방울" => PokemonType.Water,
        "부드러운모래" => PokemonType.Ground,
        "용의이빨" => PokemonType.Dragon,
        "실크스카프" => PokemonType.Normal,
        "기적의씨" => PokemonType.Grass,
        "예리한부리" => PokemonType.Flying,
        "자석" => PokemonType.Electric,
        _ => null
    };
}

public sealed class ContactReactionEffectHandler : IBattleEffectHandler
{
    public int Order => 200;

    public async Task AfterDamageAsync(BattleEffectContext context)
    {
        if (context.Move.IsSpecial || context.Attacker.IsFainted) return;

        int? reflectedDamage = context.Defender.TryReflectDamage(true);
        if (reflectedDamage == null) return;

        context.Attacker.CurrentHp = Math.Max(0, context.Attacker.CurrentHp - reflectedDamage.Value);
        if (context.Attacker.CurrentHp == 0) context.Attacker.IsFainted = true;
        await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 철가시에 찔렸다!");
    }

    public async Task AfterMoveAsync(BattleEffectContext context)
    {
        var defender = context.Defender;
        if (context.Move.IsStatus || context.Move.Power <= 0 || defender.IsFainted) return;
        if (defender.SelectedAbility != "정전기" || context.Random.Next(100) >= 30) return;

        // Preserve the existing battle rule: Static only attempts its reaction
        // while the defender has no primary status condition.
        if (defender.Status != StatusCondition.None) return;
        context.Attacker.Status = StatusCondition.Paralysis;
        await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 정전기에 마비됐다!");
    }
}