using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed class DamageModifierEffectHandler : IBattleEffectHandler
{
    public int Order => 300;

    public void ModifyPower(BattlePowerContext context)
    {
        var attacker = context.Attacker;
        var move = context.Move;
        var attackType = context.AttackType;

        if (TypeBoostItem(attacker.HeldItem) == attackType) context.Power *= 1.2;
        if (attacker.HeldItem == "구애머리띠" && !move.IsSpecial) context.Power *= 1.5;
        if (attacker.HeldItem == "힘의머리띠" && !move.IsSpecial) context.Power *= 1.1;
        if (attacker.HeldItem == "구애안경" && move.IsSpecial) context.Power *= 1.5;
        if (attacker.HeldItem == "생명의구슬") context.Power *= 1.3;
        if (attacker.SelectedAbility == "타오르는불꽃" && attacker.FlashFireActive && attackType == PokemonType.Fire)
        {
            context.Power *= 1.5;
        }
        if (attacker.SelectedAbility == "테크니션" && move.Power <= 60) context.Power *= 1.5;
        if (attacker.SelectedAbility == "우격다짐"
            && (move.AilmentChance > 0 || move.FlinchChance > 0 || move.StatChanges.Count > 0)) context.Power *= 1.3;
        if (attacker.SelectedAbility is "프리즈스킨" or "페어리스킨" && move.Type == PokemonType.Normal) context.Power *= 1.2;
        if (attacker.SelectedAbility == "적응력"
            && (attackType == attacker.Data.Type1 || attacker.Data.Type2 == attackType))
        {
            // STAB is applied by BattleEngine before handlers; replace 1.5x with 2x.
            context.Power *= 2.0 / 1.5;
        }
        // Huge Power/Pure Power are already reflected in Pokemon.EffectiveAtk.
        if (attacker.SelectedAbility == "색안경"
            && BattleTypeMultiplier(attackType, context.Defender) is > 0 and < 1)
        {
            context.Power *= 2.0;
        }
        if ((attacker.SelectedAbility is "심록" or "맹화" or "급류" or "벌레의알림")
            && attacker.CurrentHp <= attacker.MaxHp / 3
            && ((attacker.SelectedAbility == "심록" && attackType == PokemonType.Grass)
                || (attacker.SelectedAbility == "맹화" && attackType == PokemonType.Fire)
                || (attacker.SelectedAbility == "급류" && attackType == PokemonType.Water)
                || (attacker.SelectedAbility == "벌레의알림" && attackType == PokemonType.Bug)))
        {
            context.Power *= 1.5;
        }
        if (attacker.SelectedAbility == "독폭주" && attacker.Status == StatusCondition.Poison && !move.IsSpecial) context.Power *= 1.5;
        if (attacker.SelectedAbility == "이판사판" && move.DrainPercent < 0) context.Power *= 1.2;
        if (attacker.SelectedAbility == "철주먹" && PunchMoves.Contains(move.Name)) context.Power *= 1.2;
        if (attacker.SelectedAbility == "옹골찬턱" && BiteMoves.Contains(move.Name)) context.Power *= 1.5;
        if (attacker.SelectedAbility == "단단한발톱" && context.MakesContact) context.Power *= 1.3;
        if (attacker.SelectedAbility == "메가런처" && PulseMoves.Contains(move.Name)) context.Power *= 1.5;
        if (attacker.SelectedAbility == "모래의힘" && BattleWeather.Current == "모래바람"
            && (attackType is PokemonType.Rock or PokemonType.Ground or PokemonType.Steel)) context.Power *= 1.3;

        if (BattleWeather.Current == "쾌청")
        {
            if (attackType == PokemonType.Fire) context.Power *= 1.5;
            if (attackType == PokemonType.Water) context.Power *= 0.5;
        }
        else if (BattleWeather.Current == "비")
        {
            if (attackType == PokemonType.Water) context.Power *= 1.5;
            if (attackType == PokemonType.Fire) context.Power *= 0.5;
        }
    }

    public Task AfterDamageResultAsync(BattleEffectContext context)
    {
        var attacker = context.Attacker;
        if (attacker.HeldItem != "생명의구슬" || attacker.SelectedAbility == "매직가드"
            || attacker.IsFainted || context.TotalDamage <= 0) return Task.CompletedTask;

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

    private static readonly HashSet<string> PunchMoves = new()
    {
        "불꽃펀치", "냉동펀치", "번개펀치", "메가톤펀치", "마하펀치", "폭발펀치",
        "그로우펀치", "암해머", "코멧펀치", "더블펀처", "드레인펀치", "진공펀치",
        "섀도펀치", "불릿펀치", "배대뒤치기"
    };

    private static readonly HashSet<string> BiteMoves = new()
    {
        "물기", "깨물어부수기", "불꽃엄니", "얼음엄니", "번개엄니", "독엄니",
        "필살앞니", "하이퍼팽", "물고버티기"
    };

    private static readonly HashSet<string> PulseMoves = new()
    {
        "파동탄", "물의파동", "악의파동", "용의파동", "파동탄", "오라휠"
    };

    private static double BattleTypeMultiplier(PokemonType attackType, Pokemon defender)
    {
        double multiplier = TypeChart.GetMultiplier(attackType, defender.Data.Type1);
        if (defender.Data.Type2 != null) multiplier *= TypeChart.GetMultiplier(attackType, defender.Data.Type2.Value);
        return multiplier;
    }
}

public sealed class AbilityLifecycleEffectHandler : IBattleEffectHandler
{
    public int Order => 250;

    public async Task AfterHitAsync(BattleEffectContext context)
    {
        if (context.LastHitDamage <= 0) return;

        if (!context.Defender.IsFainted && context.AttackType == PokemonType.Dark
            && context.Defender.SelectedAbility == "정의의마음")
        {
            context.Defender.ChangeStage("attack", 1);
            await context.ShowMessage($"{context.Defender.Data.Name}의 정의의마음으로 공격이 올랐다!");
        }

        if (!context.Defender.IsFainted && !context.Move.IsSpecial
            && context.Defender.SelectedAbility == "깨어진갑옷")
        {
            context.Defender.ChangeStage("defense", -1);
            context.Defender.ChangeStage("speed", 2);
            await context.ShowMessage($"{context.Defender.Data.Name}의 깨어진갑옷으로 방어가 떨어지고 속도가 크게 올랐다!");
        }

        if (context.MoveKey is not ("bug-bite" or "pluck"))
        {
            await ConsumeBerryAsync(
                context.Defender,
                message => context.ShowMessage(message));
        }
    }

    public async Task AfterDamageResultAsync(BattleEffectContext context)
    {
        if (context.TotalDamage <= 0) return;

        if (context.Defender.IsFainted && !context.Attacker.IsFainted
            && context.Attacker.SelectedAbility == "자기과신")
        {
            context.Attacker.ChangeStage("attack", 1);
            await context.ShowMessage($"{context.Attacker.Data.Name}의 자기과신으로 공격이 올랐다!");
        }

        if (!context.Defender.IsFainted && context.Attacker.HeldItem == "없음"
            && context.Defender.HeldItem != "없음" && context.Attacker.SelectedAbility == "매지션")
        {
            context.Attacker.HeldItem = context.Defender.HeldItem;
            context.Defender.HeldItem = "없음";
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 매지션으로 상대의 도구를 빼앗았다!");
        }
    }

    public async Task AfterMoveAsync(BattleEffectContext context)
    {
        await ConsumeBerryAsync(
            context.Attacker,
            message => context.ShowMessage(message));
        await ConsumeBerryAsync(
            context.Defender,
            message => context.ShowMessage(message));
    }

    public async Task EndOfTurnAsync(BattleEndOfTurnContext context)
    {
        var pokemon = context.Pokemon;
        if (pokemon.IsFainted) return;

        await ConsumeBerryAsync(
            pokemon,
            message => context.ShowMessage(message, 900));

        if (pokemon.UpdateFormAtEndOfTurn())
        {
            await context.ShowMessage($"{pokemon.Data.Name}의 달마모드로 모습이 변했다!", 900);
        }

        if (pokemon.SelectedAbility == "가속")
        {
            pokemon.ChangeStage("speed", 1);
            await context.ShowMessage($"{pokemon.Data.Name}의 가속으로 속도가 올랐다!", 900);
        }

        if (pokemon.SelectedAbility == "촉촉바디" && BattleWeather.Current == "비"
            && pokemon.Status != StatusCondition.None)
        {
            pokemon.ClearPrimaryStatus();
            await context.ShowMessage($"{pokemon.Data.Name}의 촉촉바디로 상태 이상이 회복되었다!", 900);
        }
        else if (pokemon.SelectedAbility == "탈피" && pokemon.Status != StatusCondition.None
            && Random.Shared.Next(100) < 30)
        {
            pokemon.ClearPrimaryStatus();
            await context.ShowMessage($"{pokemon.Data.Name}은(는) 탈피로 상태 이상을 회복했다!", 900);
        }

        int heal = 0;
        if (pokemon.SelectedAbility == "젖은접시" && BattleWeather.Current == "비") heal = pokemon.MaxHp / 16;
        if (pokemon.SelectedAbility == "건조피부" && BattleWeather.Current == "비") heal = pokemon.MaxHp / 8;
        if (pokemon.SelectedAbility == "아이스바디" && BattleWeather.Current == "싸라기눈") heal = pokemon.MaxHp / 16;
        if (heal > 0)
        {
            int before = pokemon.CurrentHp;
            pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + Math.Max(1, heal));
            if (pokemon.CurrentHp > before)
            {
                await context.ShowMessage($"{pokemon.Data.Name}은(는) {pokemon.SelectedAbility}으로 HP를 회복했다!", 900);
            }
        }

        bool takesAbilityDamage = (pokemon.SelectedAbility == "선파워" && BattleWeather.Current == "쾌청")
            || (pokemon.SelectedAbility == "건조피부" && BattleWeather.Current == "쾌청");
        if (takesAbilityDamage && pokemon.SelectedAbility != "매직가드")
        {
            await DamageAsync(context, Math.Max(1, pokemon.MaxHp / 8),
                $"{pokemon.Data.Name}은(는) {pokemon.SelectedAbility}으로 HP가 줄었다!");
        }

        if (pokemon.IsFainted || pokemon.SelectedAbility is "매직가드" or "방진") return;
        bool sandDamage = BattleWeather.Current == "모래바람"
            && pokemon.Data.Type1 is not (PokemonType.Rock or PokemonType.Ground or PokemonType.Steel)
            && pokemon.Data.Type2 is not (PokemonType.Rock or PokemonType.Ground or PokemonType.Steel);
        bool hailDamage = BattleWeather.Current == "싸라기눈"
            && pokemon.Data.Type1 != PokemonType.Ice && pokemon.Data.Type2 != PokemonType.Ice;
        if (sandDamage || hailDamage)
        {
            await DamageAsync(context, Math.Max(1, pokemon.MaxHp / 16),
                $"{pokemon.Data.Name}은(는) {BattleWeather.Current}에 데미지를 입었다!");
        }
    }

    private static async Task DamageAsync(BattleEndOfTurnContext context, int damage, string message)
    {
        context.Pokemon.CurrentHp = Math.Max(0, context.Pokemon.CurrentHp - damage);
        if (context.Pokemon.CurrentHp == 0) context.Pokemon.IsFainted = true;
        await context.ShowMessage(message, 900);
    }

    private static async Task ConsumeBerryAsync(Pokemon pokemon, Func<string, Task> showMessage)
    {
        if (pokemon.IsFainted || !pokemon.TryConsumeBerry(out string? berryMessage)) return;

        await showMessage(berryMessage!);
        if (pokemon.SelectedAbility != "볼주머니") return;

        int before = pokemon.CurrentHp;
        int pouchHeal = Math.Max(1, pokemon.MaxHp / 8);
        pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + pouchHeal);
        if (pokemon.CurrentHp > before)
        {
            await showMessage($"{pokemon.Data.Name}은(는) 볼주머니로 HP를 회복했다!");
        }
    }
}

public sealed class ContactReactionEffectHandler : IBattleEffectHandler
{
    public int Order => 200;

    public async Task AfterHitAsync(BattleEffectContext context)
    {
        if (!context.MakesContact || context.LastHitDamage <= 0 || context.Attacker.IsFainted) return;

        int? reflectedDamage = context.Defender.TryReflectDamage(context.MakesContact);
        if (reflectedDamage != null)
        {
            context.Attacker.CurrentHp = Math.Max(0, context.Attacker.CurrentHp - reflectedDamage.Value);
            if (context.Attacker.CurrentHp == 0) context.Attacker.IsFainted = true;
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) {context.Defender.SelectedAbility}에 상처를 입었다!");
        }

        if (context.Defender.SelectedAbility == "미라" && context.Attacker.SelectedAbility != "미라")
        {
            context.Attacker.SelectedAbility = "미라";
            await context.ShowMessage($"{context.Attacker.Data.Name}의 특성이 미라로 변했다!");
        }

        if (context.Defender.IsFainted && context.Defender.SelectedAbility == "유폭"
            && !context.Attacker.IsFainted)
        {
            int damage = Math.Max(1, context.Attacker.MaxHp / 4);
            context.Attacker.CurrentHp = Math.Max(0, context.Attacker.CurrentHp - damage);
            if (context.Attacker.CurrentHp == 0) context.Attacker.IsFainted = true;
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 유폭으로 데미지를 입었다!");
        }

        if (context.Defender.IsFainted || context.Attacker.IsFainted) return;

        if (context.Attacker.Status != StatusCondition.None) return;
        string? reaction = null;
        if (context.Defender.SelectedAbility == "정전기" && context.Random.Next(100) < 30)
        {
            context.Attacker.ApplyAilment("paralysis");
            if (context.Attacker.Status == StatusCondition.Paralysis) reaction = "정전기에 마비됐다";
        }
        else if (context.Defender.SelectedAbility == "독가시" && context.Random.Next(100) < 30)
        {
            context.Attacker.ApplyAilment("poison");
            if (context.Attacker.Status == StatusCondition.Poison) reaction = "독가시에 찔려 독 상태가 되었다";
        }
        else if (context.Defender.SelectedAbility == "불꽃몸" && context.Random.Next(100) < 30)
        {
            context.Attacker.ApplyAilment("burn");
            if (context.Attacker.Status == StatusCondition.Burn) reaction = "불꽃몸에 닿아 화상을 입었다";
        }
        else if (context.Defender.SelectedAbility == "포자" && context.Random.Next(100) < 30)
        {
            string ailment = context.Random.Next(3) switch { 0 => "poison", 1 => "paralysis", _ => "sleep" };
            context.Attacker.ApplyAilment(ailment);
            if (context.Attacker.Status != StatusCondition.None) reaction = $"포자 때문에 {AilmentKor(ailment)} 상태가 되었다";
        }
        else if (context.Defender.SelectedAbility == "독수" && context.Random.Next(100) < 30)
        {
            context.Attacker.ApplyAilment("poison");
            if (context.Attacker.Status == StatusCondition.Poison) reaction = "독수에 중독되었다";
        }
        if (reaction != null) await context.ShowMessage($"{context.Attacker.Data.Name}은(는) {reaction}!");
    }

    public async Task AfterMoveAsync(BattleEffectContext context)
    {
        var defender = context.Defender;
        if (context.Move.IsStatus || context.TotalDamage <= 0 || defender.IsFainted) return;
        if (defender.SelectedAbility == "저주받은바디" && context.Random.Next(100) < 30)
        {
            context.Attacker.DisableMove(context.MoveKey);
            await context.ShowMessage($"{context.Attacker.Data.Name}의 {context.Move.Name}이(가) 저주받은바디로 봉인되었다!");
        }
    }

    private static string AilmentKor(string ailment) => ailment switch
    {
        "paralysis" => "마비", "poison" => "독", "burn" => "화상", "sleep" => "잠듦", _ => ailment
    };
}