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
        if (MoveRuleMetadata.IsRampageMove(context.MoveKey)
            && context.TotalDamage > 0
            && !context.Attacker.IsFainted)
        {
            bool ended = false;
            if (context.Attacker.RampageMoveKey == null)
            {
                context.Attacker.StartRampage(context.MoveKey, context.Random.Next(2, 4));
            }
            else
            {
                ended = context.Attacker.AdvanceRampageTurn();
            }

            if (ended)
            {
                context.Attacker.ClearRampage();
                if (!context.Attacker.IsConfused && !context.Attacker.IsImmuneToConfusion())
                {
                    context.Attacker.ApplyConfusion(context.Random);
                    await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 난동이 끝나 혼란에 빠졌다!");
                }
            }
        }

        if (context.Move.DrainPercent > 0 && !context.Attacker.IsFainted)
        {
            int heal = Math.Max(1, context.TotalDamage * context.Move.DrainPercent / 100);
            context.Attacker.CurrentHp = Math.Min(context.Attacker.MaxHp, context.Attacker.CurrentHp + heal);
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) HP를 흡수했다!");
        }
        else if (context.Move.DrainPercent < 0 && !context.Attacker.IsFainted
            && context.Attacker.SelectedAbility != "돌머리")
        {
            int recoilDamage = Math.Max(1, context.TotalDamage * Math.Abs(context.Move.DrainPercent) / 100);
            context.Attacker.CurrentHp = Math.Max(0, context.Attacker.CurrentHp - recoilDamage);
            if (context.Attacker.CurrentHp == 0) context.Attacker.MarkFainted();
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 반동으로 데미지를 입었다!");
        }

        if (!context.Defender.IsFainted && context.MoveKey == "incinerate"
            && Pokemon.IsBerry(context.Defender.HeldItem))
        {
            string berry = context.Defender.HeldItem;
            context.Defender.HeldItem = "없음";
            await context.ShowMessage($"{context.Defender.Data.Name}의 {berry}이(가) 불태워졌다!");
        }

        if (!context.Defender.IsFainted && context.MoveKey == "knock-off"
            && context.Defender.HeldItem != "없음")
        {
            string item = context.Defender.HeldItem;
            context.Defender.HeldItem = "없음";
            await context.ShowMessage($"{context.Defender.Data.Name}의 {item}이(가) 떨어졌다!");
        }

        if (context.MoveKey is "self-destruct" or "explosion" or "misty-explosion"
            && !context.Attacker.IsFainted)
        {
            context.Attacker.MarkFainted();
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 자신을 희생했다!");
        }

        if (context.MoveKey == "memento" && !context.Attacker.IsFainted)
        {
            context.Attacker.MarkFainted();
            await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 추억의선물로 쓰러졌다!");
        }

        if (context.MoveKey is "giga-impact" or "hyper-beam" or "rock-wrecker"
            or "roar-of-time" or "blast-burn" or "frenzy-plant" or "hydro-cannon"
            or "meteor-assault")
        {
            context.Attacker.SetMustRecharge();
        }

        if (context.MoveKey is "bug-bite" or "pluck"
            && context.TotalDamage > 0
            && !context.Attacker.IsBerryEatingBlockedBy(context.Defender)
            && context.Defender.TryTakeHeldBerry(out string? berryName))
        {
            context.Attacker.ApplyBerryEffect(berryName!);
            await context.ShowMessage(
                $"{context.Attacker.Data.Name}은(는) {context.Defender.Data.Name}의 {berryName}을(를) 빼앗아 먹었다!");
            if (context.Attacker.SelectedAbility == "볼주머니")
            {
                int before = context.Attacker.CurrentHp;
                int heal = Math.Max(1, context.Attacker.MaxHp / 8);
                context.Attacker.CurrentHp = Math.Min(context.Attacker.MaxHp, context.Attacker.CurrentHp + heal);
                if (context.Attacker.CurrentHp > before)
                {
                    await context.ShowMessage($"{context.Attacker.Data.Name}은(는) 볼주머니로 HP를 회복했다!");
                }
            }
        }
        else if (context.MoveKey is "bug-bite" or "pluck"
            && context.TotalDamage > 0
            && context.Attacker.IsBerryEatingBlockedBy(context.Defender))
        {
            await context.ShowMessage(
                $"{context.Attacker.Data.Name}은(는) 상대의 긴장감 때문에 나무열매를 먹을 수 없다!");
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

        await ApplyMoveSpecificEffectAsync(context);

        string? weather = MoveRuleMetadata.WeatherForMove(context.MoveKey);
        if (weather != null)
        {
            BattleWeather.Set(weather, turns: 5);
            await context.ShowMessage($"{attacker.Data.Name}의 기술로 날씨가 {weather}(으)로 바뀌었다!");
        }

        string? field = MoveRuleMetadata.FieldForMove(context.MoveKey);
        if (field != null)
        {
            BattleField.Set(field, turns: 5);
            await context.ShowMessage($"{attacker.Data.Name}의 기술로 필드가 {field}(으)로 바뀌었다!");
        }

        if (move.IsStatus && move.HealingPercent > 0 && context.MoveKey != "swallow")
        {
            int heal = MoveRuleMetadata.RecoveryAmount(context.MoveKey, move, attacker.MaxHp);
            attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
            await context.ShowMessage($"{attacker.Data.Name}은(는) HP를 회복했다!");
        }

        if (!suppressSecondaryEffects && IsSupportedAilment(move.AilmentName) && !defender.IsFainted
            && context.Random.Next(100) < Math.Min(100, move.AilmentChance * chanceMultiplier))
        {
            if (move.AilmentName == "confusion")
            {
                if (!defender.IsConfused && !defender.IsImmuneToConfusion())
                {
                    defender.ApplyConfusion(context.Random);
                    await context.ShowMessage($"{defender.Data.Name}은(는) 혼란에 빠졌다!");
                }
            }
            else if (defender.Status == StatusCondition.None && !defender.IsImmuneToAilment(move.AilmentName))
            {
                string ailment = context.MoveKey == "toxic" ? "toxic" : move.AilmentName;
                defender.ApplyAilment(ailment, context.Random);
                await context.ShowMessage($"{defender.Data.Name}은(는) {AilmentKor(ailment)} 상태가 되었다!");

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
            && context.MoveKey != "stockpile"
            && context.Random.Next(100) < Math.Min(100, move.StatChangeChance * chanceMultiplier))
        {
            foreach (var statChange in move.StatChanges)
            {
                bool targetsSelf = statChange.TargetsSelf || IsSelfStatChange(context.MoveKey, statChange.Stat);
                var target = targetsSelf ? attacker : defender;
                if (target.IsFainted) continue;

                int before = target.StatStages[statChange.Stat];
                target.ChangeStage(statChange.Stat, statChange.Change, causedByOpponent: !targetsSelf);
                int after = target.StatStages[statChange.Stat];
                if (before == after) continue;

                string direction = after > before ? "상승했다" : "하락했다";
                await context.ShowMessage($"{target.Data.Name}의 {StatKor(statChange.Stat)}이(가) {direction}!");
                if (!targetsSelf && after < before)
                {
                    string? reaction = target.TriggerStatDropAbility();
                    if (reaction != null) await context.ShowMessage(reaction);
                }
            }
        }
    }

    public async Task EndOfTurnAsync(BattleEndOfTurnContext context)
    {
        var pokemon = context.Pokemon;
        if (pokemon.IsFainted) return;

        if (pokemon.LeechSeeded)
        {
            int damage = Math.Max(1, pokemon.MaxHp / 8);
            pokemon.CurrentHp = Math.Max(0, pokemon.CurrentHp - damage);
            if (pokemon.CurrentHp == 0) pokemon.MarkFainted();
            await context.ShowMessage($"{pokemon.Data.Name}은(는) 씨뿌리기로 HP를 빼앗겼다!", 900);

            var source = pokemon.LeechSeedSource;
            if (source != null && !source.IsFainted)
            {
                int heal = Math.Min(damage, source.MaxHp - source.CurrentHp);
                source.CurrentHp += heal;
                if (heal > 0) await context.ShowMessage($"{source.Data.Name}은(는) 씨뿌리기로 HP를 회복했다!", 900);
            }
        }

        if (!pokemon.IsFainted && pokemon.BindingTurnsRemaining > 0)
        {
            int damage = Math.Max(1, pokemon.MaxHp / 8);
            pokemon.CurrentHp = Math.Max(0, pokemon.CurrentHp - damage);
            if (pokemon.CurrentHp == 0) pokemon.MarkFainted();
            await context.ShowMessage($"{pokemon.Data.Name}은(는) 조이기 기술의 지속 데미지를 입었다!", 900);
        }

        if (!pokemon.IsFainted && pokemon.Ingrained)
        {
            int before = pokemon.CurrentHp;
            pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + Math.Max(1, pokemon.MaxHp / 16));
            if (pokemon.CurrentHp > before)
                await context.ShowMessage($"{pokemon.Data.Name}은(는) 뿌리박기로 HP를 회복했다!", 900);
        }

        if (!pokemon.IsFainted && pokemon.YawnTurnsRemaining == 1
            && pokemon.Status == StatusCondition.None && !pokemon.UproarActive)
        {
            pokemon.ApplyAilment("sleep");
            if (pokemon.Status == StatusCondition.Sleep)
                await context.ShowMessage($"{pokemon.Data.Name}은(는) 하품 때문에 잠들었다!", 900);
        }

        if (!pokemon.IsFainted && pokemon.PerishTurnsRemaining > 0)
        {
            if (pokemon.PerishTurnsRemaining == 1)
            {
                pokemon.MarkFainted();
                await context.ShowMessage($"{pokemon.Data.Name}은(는) 멸망의노래로 쓰러졌다!", 900);
            }
            else
            {
                pokemon.SetPerish(pokemon.PerishTurnsRemaining - 1);
                await context.ShowMessage(
                    $"{pokemon.Data.Name}의 멸망의노래 카운트가 {pokemon.PerishTurnsRemaining}이 되었다!", 900);
            }
        }

        if (!pokemon.IsFainted && pokemon.NightmareActive && pokemon.Status == StatusCondition.Sleep)
        {
            int damage = Math.Max(1, pokemon.MaxHp / 4);
            pokemon.CurrentHp = Math.Max(0, pokemon.CurrentHp - damage);
            if (pokemon.CurrentHp == 0) pokemon.MarkFainted();
            await context.ShowMessage($"{pokemon.Data.Name}은(는) 악몽으로 고통받았다!", 900);
        }
    }

    private static async Task ApplyMoveSpecificEffectAsync(BattleEffectContext context)
    {
        var move = context.Move;
        var attacker = context.Attacker;
        var defender = context.Defender;
        string key = context.MoveKey;

        if (defender.IsFainted && key is not ("self-destruct" or "explosion" or "misty-explosion" or "memento"))
            return;

        switch (MoveRuleMetadata.GetRule(key, move).Kind)
        {
            case MoveRuleKind.LeechSeed:
                if (defender.HasType(PokemonType.Grass))
                {
                    await context.ShowMessage($"{defender.Data.Name}은(는) 씨뿌리기를 피했다!");
                }
                else
                {
                    defender.MarkLeechSeeded(attacker);
                    await context.ShowMessage($"{defender.Data.Name}에게 씨가 뿌려졌다!");
                }
                break;

            case MoveRuleKind.Binding:
                defender.SetBinding(key, 4);
                await context.ShowMessage($"{defender.Data.Name}은(는) {move.Name}에 휘감겼다!");
                break;

            case MoveRuleKind.Yawn:
                if (defender.IsImmuneToAilment("sleep"))
                    await context.ShowMessage($"{defender.Data.Name}은(는) 잠들지 않는다!");
                else
                {
                    defender.SetYawn();
                    await context.ShowMessage($"{defender.Data.Name}은(는) 하품을 했다. 다음 턴 잠들 것 같다!");
                }
                break;

            case MoveRuleKind.PerishSong:
                attacker.SetPerish(3);
                defender.SetPerish(3);
                await context.ShowMessage("멸망의노래를 들은 포켓몬은 3턴 뒤 쓰러진다!");
                break;

            case MoveRuleKind.Disable:
                if (defender.IsImmuneToMentalEffect("disable"))
                {
                    await context.ShowMessage($"{defender.Data.Name}은(는) 아로마베일로 기술 봉인을 막았다!");
                }
                else if (defender.LastMoveKey != null)
                {
                    defender.DisableMove(defender.LastMoveKey);
                    await context.ShowMessage($"{defender.Data.Name}의 {defender.LastMoveKey}이(가) 봉인되었다!");
                }
                break;

            case MoveRuleKind.MoveRestriction:
                await ApplyRestrictionAsync(context);
                break;

            case MoveRuleKind.HazardRemoval:
                attacker.ClearLeechSeed();
                attacker.ClearBinding();
                await context.ShowMessage($"{attacker.Data.Name} 주변의 지속 효과가 사라졌다!");
                break;
        }

        if (key == "stockpile")
        {
            if (attacker.TryStockpile())
            {
                attacker.ChangeStage("defense", 1);
                attacker.ChangeStage("special-defense", 1);
                await context.ShowMessage($"{attacker.Data.Name}은(는) 힘을 비축했다! ({attacker.StockpileCount}/3)");
            }
            else
                await context.ShowMessage($"{attacker.Data.Name}은(는) 더 이상 힘을 비축할 수 없다!");
        }

        if (key == "swallow")
        {
            int stockpile = attacker.ConsumeStockpile();
            if (stockpile == 0)
            {
                await context.ShowMessage($"{attacker.Data.Name}은(는) 비축한 힘이 없어 꿀꺽할 수 없다!");
            }
            else
            {
                int heal = attacker.MaxHp * stockpile / 4;
                attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
                await context.ShowMessage($"{attacker.Data.Name}은(는) 비축한 힘으로 HP를 회복했다!");
            }
        }

        if (key == "rage")
        {
            attacker.SetRage();
            await context.ShowMessage($"{attacker.Data.Name}은(는) 분노하기 시작했다!");
        }

        if (key == "ingrain")
        {
            attacker.SetIngrained();
            await context.ShowMessage($"{attacker.Data.Name}은(는) 뿌리를 내려 움직이지 않게 되었다!");
        }

        if (key == "charge")
        {
            attacker.SetChargeBoost();
            await context.ShowMessage($"{attacker.Data.Name}은(는) 전기를 모아 다음 전기 기술을 강화했다!");
        }

        if (key is "foresight" or "odor-sleuth" or "miracle-eye")
        {
            defender.RevealTypeImmunity();
            await context.ShowMessage($"{defender.Data.Name}의 타입 상성이 간파되었다!");
        }

        if (key == "clear-smog")
        {
            defender.ClearStatStages();
            await context.ShowMessage($"{defender.Data.Name}의 능력 변화가 원래대로 돌아왔다!");
        }

        if (key == "haze")
        {
            attacker.ClearStatStages();
            defender.ClearStatStages();
            await context.ShowMessage("모든 포켓몬의 능력 변화가 사라졌다!");
        }

        if (key == "strength-sap" && !attacker.IsFainted)
        {
            int heal = Math.Max(1, defender.EffectiveAtk);
            attacker.CurrentHp = Math.Min(attacker.MaxHp, attacker.CurrentHp + heal);
            await context.ShowMessage($"{attacker.Data.Name}은(는) 상대의 공격만큼 HP를 회복했다!");
        }

        if (key == "uproar")
        {
            attacker.SetUproar();
            defender.SetUproar();
        }

        if (key == "nightmare" && defender.Status == StatusCondition.Sleep)
        {
            defender.SetNightmare();
            await context.ShowMessage($"{defender.Data.Name}은(는) 악몽에 시달리기 시작했다!");
        }

        if (key is "brick-break" && !defender.IsFainted)
            await context.ShowMessage($"{defender.Data.Name}의 장벽이 부서졌다!");

        bool switchesAttacker = key is "u-turn" or "volt-switch" or "parting-shot"
            or "baton-pass" or "teleport";
        if (MoveRuleMetadata.IsForcedSwitchMove(key) && !attacker.IsFainted
            && (switchesAttacker || defender.CanBeForcedSwitched))
        {
            context.RequestSwitch = true;
            context.SwitchPokemon = switchesAttacker ? attacker : defender;
            context.SwitchReason = switchesAttacker ? "교대 기술" : "강제 교체";
        }
        else if (MoveRuleMetadata.IsForcedSwitchMove(key) && !attacker.IsFainted
            && !switchesAttacker && !defender.CanBeForcedSwitched)
        {
            await context.ShowMessage($"{defender.Data.Name}은(는) 흡반으로 강제 교체를 막았다!");
        }
    }

    private static async Task ApplyRestrictionAsync(BattleEffectContext context)
    {
        var attacker = context.Attacker;
        var defender = context.Defender;
        switch (context.MoveKey)
        {
            case "taunt":
                if (defender.IsImmuneToMentalEffect("taunt"))
                    await context.ShowMessage($"{defender.Data.Name}은(는) 아로마베일로 도발을 막았다!");
                else
                {
                    defender.SetTaunt(3);
                    await context.ShowMessage($"{defender.Data.Name}은(는) 도발에 걸렸다!");
                }
                break;
            case "torment":
                if (defender.IsImmuneToMentalEffect("torment"))
                    await context.ShowMessage($"{defender.Data.Name}은(는) 아로마베일로 괴롭힘을 막았다!");
                else
                {
                    defender.SetTorment(5);
                    await context.ShowMessage($"{defender.Data.Name}은(는) 괴롭힘을 당해 같은 기술을 연속으로 쓸 수 없다!");
                }
                break;
            case "throat-chop":
                defender.SetThroatChop(2);
                await context.ShowMessage($"{defender.Data.Name}은(는) 소리 기술을 쓸 수 없게 되었다!");
                break;
            case "embargo":
                defender.SetEmbargo(5);
                await context.ShowMessage($"{defender.Data.Name}은(는) 도구를 사용할 수 없게 되었다!");
                break;
            case "heal-block":
                if (defender.IsImmuneToMentalEffect("heal-block"))
                    await context.ShowMessage($"{defender.Data.Name}은(는) 아로마베일로 회복 봉인을 막았다!");
                else
                {
                    defender.SetHealBlock(5);
                    await context.ShowMessage($"{defender.Data.Name}은(는) 회복 기술을 쓸 수 없게 되었다!");
                }
                break;
            case "imprison":
                defender.AddImprisonedMoves(attacker.CurrentPP.Keys);
                await context.ShowMessage($"{defender.Data.Name}은(는) 상대가 알고 있는 기술을 쓸 수 없게 되었다!");
                break;
            case "encore" when defender.LastMoveKey != null:
                if (defender.IsImmuneToMentalEffect("encore"))
                    await context.ShowMessage($"{defender.Data.Name}은(는) 아로마베일로 앙코르를 막았다!");
                else
                {
                    defender.SetEncore(defender.LastMoveKey, 3);
                    await context.ShowMessage($"{defender.Data.Name}은(는) {defender.LastMoveKey}를 계속 사용하게 되었다!");
                }
                break;
            case "attract":
                defender.SetInfatuated();
                await context.ShowMessage($"{defender.Data.Name}은(는) 헤롱헤롱 상태가 되었다!");
                break;
        }
    }

    private static bool IsSupportedAilment(string ailment) =>
        ailment is "paralysis" or "poison" or "toxic" or "burn" or "sleep" or "freeze" or "confusion";

    private static bool IsSelfStatChange(string moveKey, string stat) =>
        moveKey switch
        {
            "metal-claw" when stat == "attack" => true,
            "steel-wing" when stat == "defense" => true,
            "charge-beam" when stat == "special-attack" => true,
            "fiery-dance" when stat == "special-attack" => true,
            "diamond-storm" when stat == "defense" => true,
            "silver-wind" or "ancient-power" => true,
            "v-create" or "dragon-ascent" or "leaf-storm" or "psycho-boost" => false,
            _ => false
        };

    private static string AilmentKor(string ailment) => ailment switch
    {
        "paralysis" => "마비",
        "poison" => "독",
        "burn" => "화상",
        "sleep" => "잠듦",
        "freeze" => "얼음",
        "confusion" => "혼란",
        "toxic" => "맹독",
        _ => ailment
    };

    private static string StatKor(string stat) => stat switch
    {
        "attack" => "공격",
        "defense" => "방어",
        "special-attack" => "특공",
        "special-defense" => "특방",
        "speed" => "속도",
        "accuracy" => "명중률",
        "evasion" => "회피율",
        _ => stat
    };
}