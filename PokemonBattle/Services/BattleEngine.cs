using PokemonBattle.Models;

namespace PokemonBattle.Services;

/// <summary>
/// Runs battle rules without depending on Blazor, rendering, or JavaScript.
/// It returns an ordered event stream for the page to display.
/// </summary>
public sealed class BattleEngine
{
    private readonly Random rng;
    private readonly IReadOnlyList<IBattleEffectHandler> effectHandlers;

    public BattleEngine(IEnumerable<IBattleEffectHandler> handlers)
        : this(new Random(), handlers)
    {
    }

    public BattleEngine(Random random, IEnumerable<IBattleEffectHandler> handlers)
    {
        rng = random;
        effectHandlers = handlers.OrderBy(handler => handler.Order).ToArray();
    }

    public int EffectiveSpeed(Pokemon pokemon, Pokemon? opponent = null)
    {
        double speed = pokemon.EffectiveSpdAgainst(opponent);
        if (pokemon.HeldItem == "구애스카프") speed *= 1.5;
        return (int)speed;
    }

    public bool CanSwitch(Pokemon active, Pokemon opponent)
    {
        if (active.Ingrained
            || active.BindingTurnsRemaining > 0
            || active.RampageMoveKey != null) return false;
        bool isGhostType = active.HasType(PokemonType.Ghost);
        if (opponent.SelectedAbility == "그림자밟기"
            && !isGhostType
            && active.SelectedAbility != "그림자밟기") return false;
        if (opponent.SelectedAbility == "개미지옥"
            && !active.HasType(PokemonType.Flying)
            && !isGhostType
            && active.SelectedAbility != "부유") return false;
        if (opponent.SelectedAbility == "자력"
            && active.IsSteelType
            && active.SelectedAbility != "자력") return false;
        return true;
    }

    /// <summary>
    /// Determines whether the active Pokémon can leave a wild battle.
    /// This is a permission check for the escape action; a regular wild
    /// encounter is allowed when no trapping condition applies, while Run
    /// Away explicitly bypasses those conditions.
    /// </summary>
    public bool CanEscape(Pokemon active, Pokemon opponent, bool isWildBattle = true)
    {
        if (!isWildBattle || active.IsFainted) return false;
        if (active.SelectedAbility == "도주") return true;
        return CanSwitch(active, opponent);
    }

    public double PreviewMultiplier(Move move, Pokemon target, Pokemon? attacker = null)
    {
        PokemonType attackType = attacker?.ResolveMoveType(move, target) ?? move.Type;
        double multiplier = TypeChart.GetMultiplier(attackType, target.CurrentType1);
        if (target.CurrentType2 != null) multiplier *= TypeChart.GetMultiplier(attackType, target.CurrentType2.Value);
        if (target.IsImmuneToMoveType(attackType)) multiplier = 0;
        if (target.SelectedAbility == "불가사의부적" && multiplier > 0 && multiplier < 2.0) multiplier = 0;
        return multiplier;
    }

    public IReadOnlyList<string> InitializeWeather(Pokemon hero, Pokemon enemy)
    {
        BattleWeather.Reset();
        BattleField.Reset();
        var messages = new List<string>();
        var entrants = new[] { (Pokemon: hero, Opponent: enemy), (Pokemon: enemy, Opponent: hero) }
            .OrderByDescending(entry => EffectiveSpeed(entry.Pokemon, entry.Opponent));
        foreach (var entry in entrants)
        {
            messages.AddRange(ActivateSwitchIn(entry.Pokemon, entry.Opponent));
        }
        return messages;
    }

    public IReadOnlyList<string> ActivateSwitchIn(Pokemon entrant, Pokemon opponent)
    {
        var messages = new List<string>();
        entrant.ResetFieldCounter();

        string originalName = entrant.Data.Name;
        if (entrant.TryTransformInto(opponent))
        {
            messages.Add($"{originalName}의 괴짜로 {entrant.Data.Name}으로 변신했다!");
        }

        string? weather = entrant.SelectedAbility switch
        {
            "가뭄" => "쾌청",
            "잔비" => "비",
            "모래날림" => "모래바람",
            "눈퍼뜨리기" => "싸라기눈",
            _ => null
        };
        if (weather != null)
        {
            BattleWeather.Set(weather);
            messages.Add($"{entrant.Data.Name}의 {entrant.SelectedAbility}! 날씨가 {weather}(으)로 바뀌었다!");
        }

        if (entrant.SelectedAbility == "위협")
        {
            int before = opponent.StatStages["attack"];
            opponent.ChangeStage("attack", -1, causedByOpponent: true);
            if (opponent.StatStages["attack"] < before)
            {
                messages.Add($"{entrant.Data.Name}의 위협으로 {opponent.Data.Name}의 공격이 떨어졌다!");
                string? reaction = opponent.TriggerStatDropAbility();
                if (reaction != null) messages.Add(reaction);
            }
            else
            {
                messages.Add($"{opponent.Data.Name}은(는) 위협의 영향을 받지 않았다!");
            }
        }

        if (entrant.SelectedAbility == "다운로드")
        {
            string stat = opponent.EffectiveDef <= opponent.EffectiveSpDef ? "attack" : "special-attack";
            entrant.ChangeStage(stat, 1);
            messages.Add($"{entrant.Data.Name}의 다운로드로 {(stat == "attack" ? "공격" : "특공")}이 올랐다!");
        }

        if (entrant.SelectedAbility == "슬로스타트")
        {
            messages.Add($"{entrant.Data.Name}은(는) 슬로스타트로 힘을 내지 못하고 있다!");
        }

        return messages;
    }

    public void PrepareSwitchOut(Pokemon pokemon)
    {
        if (pokemon.SelectedAbility == "재생력" && !pokemon.IsFainted)
        {
            int heal = pokemon.MaxHp / 3;
            pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + heal);
        }
        if (pokemon.SelectedAbility == "자연회복")
        {
            pokemon.ClearPrimaryStatus();
        }
        pokemon.ResetOnSwitchOut();
    }

    public BattleTurnPlan PlanTurn(
        Pokemon hero,
        string? heroMoveKey,
        Pokemon enemy,
        IReadOnlyCollection<string> enemyMoveKeys)
    {
        string? enemyMoveKey = PickEnemyMove(enemy, enemyMoveKeys, hero);
        var heroMove = heroMoveKey == null ? null : MoveDatabase.All[heroMoveKey];
        var enemyMove = enemyMoveKey == null ? null : MoveDatabase.All[enemyMoveKey];
        int heroPriority = MovePriority(hero, heroMove);
        int enemyPriority = MovePriority(enemy, enemyMove);
        bool heroFirst = heroPriority != enemyPriority
            ? heroPriority > enemyPriority
            : EffectiveSpeed(hero, enemy) >= EffectiveSpeed(enemy, hero);
        return new BattleTurnPlan(enemyMoveKey, heroFirst);
    }

    public string? PickEnemyMove(Pokemon enemy, IReadOnlyCollection<string> moveKeys, Pokemon hero)
    {
        var usable = moveKeys.Where(enemy.CanUseMove).ToArray();
        if (usable.Length == 0) return null;

        double bestScore = -1;
        var bestKeys = new List<string>();
        foreach (var key in usable)
        {
            var move = MoveDatabase.All[key];
            double score;
            if (move.IsStatus)
            {
                score = 30 + move.StatChanges.Count * 15 + (move.AilmentName != "none" ? 20 : 0);
            }
            else
            {
                PokemonType attackType = MoveRuleMetadata.ResolveMoveType(key, move, enemy, hero);
                double averageHits = (move.MinHits + move.MaxHits) / 2.0;
                bool stab = enemy.HasType(attackType);
                score = MoveRuleMetadata.EffectivePower(key, move, enemy, hero) * averageHits
                    * (stab ? 1.5 : 1.0)
                    * PreviewMultiplier(move, hero, enemy)
                    * ((move.AlwaysHits
                        ? 100
                        : MoveRuleMetadata.EffectiveAccuracy(key, move, enemy, hero)) / 100.0);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestKeys.Clear();
                bestKeys.Add(key);
            }
            else if (Math.Abs(score - bestScore) < 0.01)
            {
                bestKeys.Add(key);
            }
        }

        return bestKeys[rng.Next(bestKeys.Count)];
    }

    public async Task ApplyEndOfTurnEffectsAsync(
        IEnumerable<Pokemon> activePokemon,
        Func<BattleEvent, Task> emit)
    {
        var active = activePokemon.ToArray();
        foreach (var pokemon in active)
        {
            if (pokemon.IsFainted) continue;

            var statusMessage = pokemon.ApplyEndOfTurnStatusDamage();
            if (statusMessage != null) await emit(BattleEvent.TurnEnd(statusMessage, 1100));

            var opponent = active.FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, pokemon) && !candidate.IsFainted);
            var context = new BattleEndOfTurnContext(pokemon, emit, opponent, rng);
            foreach (var handler in effectHandlers) await handler.EndOfTurnAsync(context);
            pokemon.AdvanceTurn();
        }

        if (BattleWeather.AdvanceTurn())
        {
            await emit(BattleEvent.TurnEnd("날씨의 효과가 사라졌다!", 900));
        }
        if (BattleField.AdvanceTurn())
        {
            await emit(BattleEvent.TurnEnd("필드의 효과가 사라졌다!", 900));
        }
    }

    public async Task ApplyEndOfBattleEffectsAsync(
        IEnumerable<Pokemon> pokemon,
        Func<BattleEvent, Task> emit)
    {
        foreach (var participant in pokemon)
        {
            if (participant.IsFainted) continue;

            var context = new BattleEndOfBattleContext(participant, rng, emit);
            foreach (var handler in effectHandlers) await handler.AfterBattleAsync(context);
        }
    }

    public async Task<BattleTurnResult> TakeTurnAsync(
        Pokemon attacker,
        Pokemon defender,
        string? moveKey,
        bool attackerIsHero,
        Func<BattleEvent, Task> emit)
    {
        var result = new BattleTurnResult();

        if (attacker.MustRecharge)
        {
            attacker.ClearRecharge();
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 재충전 중이라 움직일 수 없다!"));
            return result;
        }

        if (attacker.ShouldSkipTurn)
        {
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 게으름을 피우고 있다!"));
            return result;
        }

        if (attacker.Flinched)
        {
            attacker.Flinched = false;
            if (attacker.SelectedAbility == "불굴의마음")
            {
                attacker.ChangeStage("speed", 1);
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 불굴의마음으로 속도가 올랐다!"));
            }
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 움찔해서 움직일 수 없었다!"));
            return result;
        }

        var (canAct, statusMessage) = attacker.CheckActionPrevention(rng);
        if (statusMessage != null) await emit(BattleEvent.MessageLine(statusMessage));
        if (!canAct)
        {
            if (attacker.IsFainted) result.FaintedPokemon = attacker;
            return result;
        }

        string? pendingDelayedKey = attacker.ConsumePendingDelayedAttack(out Pokemon? delayedTarget);
        if (pendingDelayedKey != null && delayedTarget != null && !delayedTarget.IsFainted)
        {
            var delayedMove = MoveDatabase.All[pendingDelayedKey];
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}의 {delayedMove.Name}의 시한 공격이 떨어졌다!"));
            await ExecuteMoveAsync(attacker, delayedTarget, delayedMove, pendingDelayedKey,
                attackerIsHero, emit, result, isContinuation: true);
        }

        string? executingMoveKey = attacker.RampageMoveKey ?? attacker.ConsumePendingMove();
        bool isContinuation = executingMoveKey != null;
        if (executingMoveKey == null) executingMoveKey = moveKey;

        if (executingMoveKey == null)
        {
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 사용할 수 있는 기술이 없어 몸부림쳤다!"));
            int damage = Math.Max(1, (int)(((2.0 * attacker.Level / 5 + 2) * 50 * attacker.EffectiveAtk
                / Math.Max(defender.EffectiveDef, 1)) / 50) + 2);
            defender.CurrentHp = Math.Max(0, defender.CurrentHp - damage);
            defender.LastMultiplier = 1.0;
            if (defender.CurrentHp == 0) defender.MarkFainted();
            await emit(BattleEvent.Effect(
                TypeColors.GetEffectKind(PokemonType.Normal, false),
                attackerIsHero,
                PokemonType.Normal,
                "몸부림"));
        }
        else
        {
            if (!MoveDatabase.All.TryGetValue(executingMoveKey, out var move)
                || (!isContinuation && !attacker.TryUseMove(executingMoveKey)))
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 그 기술을 사용할 수 없다!"));
                return result;
            }

            attacker.MarkMoveUsed(executingMoveKey);
            var announceType = MoveRuleMetadata.ResolveMoveType(
                executingMoveKey, move, attacker, defender);
            if (attacker.TryChangeTypeForMove(announceType))
            {
                await emit(BattleEvent.MessageLine(
                    $"{attacker.Data.Name}의 변환자재로 {announceType}타입으로 변했다!"));
            }
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Announce,
                attacker,
                defender,
                attackerIsHero,
                move,
                executingMoveKey,
                announceType,
                TypeColors.GetEffectKind(announceType, move.IsStatus),
                $"{attacker.Data.Name}의 {move.Name}!"));

            var rule = MoveRuleMetadata.GetRule(executingMoveKey, move);
            if (!MoveRuleMetadata.IsProtectionMove(executingMoveKey))
                attacker.ResetProtectionStreak();
            if (executingMoveKey == "focus-punch" && attacker.WasDamagedThisTurn)
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 공격받아 집중이 끊겼다!"));
                return result;
            }
            if (!isContinuation && rule.Kind == MoveRuleKind.DelayedDamage)
            {
                attacker.SetPendingDelayedAttack(executingMoveKey, defender, rule.Duration);
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Windup, attacker, defender, attackerIsHero, move,
                    executingMoveKey, announceType,
                    TypeColors.GetEffectKind(announceType, move.IsStatus),
                    target: "opponent",
                    presentationKey: MovePresentationCatalog.Resolve(executingMoveKey, move)));
                await emit(BattleEvent.MessageLine(
                    $"{attacker.Data.Name}은(는) 미래를 예지했다. {rule.Duration}턴 뒤 공격한다!"));
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Recovery, attacker, defender, attackerIsHero, move,
                    executingMoveKey, announceType,
                    TypeColors.GetEffectKind(announceType, move.IsStatus),
                    target: "opponent",
                    presentationKey: MovePresentationCatalog.Resolve(executingMoveKey, move)));
                return result;
            }
            if (!isContinuation && rule.Kind == MoveRuleKind.Charge)
            {
                attacker.SetPendingMove(executingMoveKey,
                    executingMoveKey is "bounce" or "dive" or "fly" or "sky-drop"
                    or "phantom-force" or "shadow-force");
                if (executingMoveKey == "skull-bash")
                {
                    attacker.ChangeStage("defense", 1);
                    await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 머리를 움츠려 방어를 올렸다!"));
                }
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Windup, attacker, defender, attackerIsHero, move,
                    executingMoveKey, announceType,
                    TypeColors.GetEffectKind(announceType, move.IsStatus),
                    target: "opponent",
                    presentationKey: MovePresentationCatalog.Resolve(executingMoveKey, move)));
                await emit(BattleEvent.MessageLine(
                    $"{attacker.Data.Name}은(는) {move.Name}을(를) 준비했다!"));
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Recovery, attacker, defender, attackerIsHero, move,
                    executingMoveKey, announceType,
                    TypeColors.GetEffectKind(announceType, move.IsStatus),
                    target: "opponent",
                    presentationKey: MovePresentationCatalog.Resolve(executingMoveKey, move)));
                return result;
            }
            await ExecuteMoveAsync(attacker, defender, move, executingMoveKey, attackerIsHero, emit, result, isContinuation);
            await AdvanceRampageAfterAttemptAsync(attacker, executingMoveKey, emit);
        }

        if (attacker.IsFainted && defender.IsFainted)
        {
            result.FaintedPokemon = attacker;
            result.OtherFaintedPokemon = defender;
        }
        else if (attacker.IsFainted) result.FaintedPokemon = attacker;
        else if (defender.IsFainted) result.FaintedPokemon = defender;

        if (result.FaintedPokemon != null)
        {
            await emit(BattleEvent.ActorStep(
                BattleEventPhase.Faint,
                result.FaintedPokemon,
                ReferenceEquals(result.FaintedPokemon, attacker) ? attackerIsHero : !attackerIsHero));
        }

        if (result.OtherFaintedPokemon != null)
        {
            await emit(BattleEvent.ActorStep(
                BattleEventPhase.Faint,
                result.OtherFaintedPokemon,
                ReferenceEquals(result.OtherFaintedPokemon, attacker) ? attackerIsHero : !attackerIsHero));
        }
        return result;
    }

    private async Task AdvanceRampageAfterAttemptAsync(
        Pokemon attacker,
        string moveKey,
        Func<BattleEvent, Task> emit)
    {
        if (!MoveRuleMetadata.IsRampageMove(moveKey) || attacker.IsFainted)
        {
            return;
        }

        bool ended;
        if (attacker.RampageMoveKey == null)
        {
            attacker.StartRampage(moveKey, rng.Next(2, 4));
            ended = false;
        }
        else
        {
            ended = attacker.AdvanceRampageTurn();
        }

        if (!ended)
        {
            return;
        }

        attacker.ClearRampage();
        if (!attacker.IsConfused && !attacker.IsImmuneToConfusion())
        {
            attacker.ApplyConfusion(rng);
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}은(는) 난동이 끝나 혼란에 빠졌다!"));
        }
    }

    private async Task ExecuteMoveAsync(
        Pokemon attacker,
        Pokemon defender,
        Move move,
        string moveKey,
        bool attackerIsHero,
        Func<BattleEvent, Task> emit,
        BattleTurnResult result,
        bool isContinuation = false)
    {
        if (attacker.UpdateFormForMove(moveKey, move.IsStatus))
        {
            string form = attacker.IsAlternateForm ? "공격모드" : "방어모드";
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}의 배틀스위치로 {form}로 모습이 변했다!"));
        }

        PokemonType attackType = MoveRuleMetadata.ResolveMoveType(
            moveKey, move, attacker, defender);
        bool makesContact = MoveRuleMetadata.MakesContact(moveKey, move);
        string effectKind = TypeColors.GetEffectKind(attackType, move.IsStatus);
        string presentationKey = MovePresentationCatalog.Resolve(moveKey, move);
        await emit(BattleEvent.MoveStep(
            BattleEventPhase.Windup,
            attacker,
            defender,
            attackerIsHero,
            move,
            moveKey,
            attackType,
            effectKind,
            target: TargetsOpponent(move) ? "opponent" : "self",
            presentationKey: presentationKey));

        double effectiveAccuracy = MoveRuleMetadata.EffectiveAccuracy(
            moveKey, move, attacker, defender);
        if (BattleField.Current == BattleField.Psychic && move.Priority > 0 && TargetsOpponent(move))
        {
            await emit(BattleEvent.MessageLine(
                $"{defender.Data.Name} 주변의 사이코필드가 우선도 기술을 막았다!"));
            return;
        }
        bool hit = move.AlwaysHits || attacker.SelectedAbility == "노가드" || defender.SelectedAbility == "노가드"
            || rng.Next(100) < Math.Min(100, (int)effectiveAccuracy);
        if (!hit)
        {
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 빗나갔다!"));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "miss", target: "opponent", presentationKey: presentationKey));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Recovery, attacker, defender, attackerIsHero, move, moveKey,
                attackType, effectKind, target: "opponent", presentationKey: presentationKey));
            return;
        }

        var context = new BattleEffectContext(
            attacker, defender, move, attackerIsHero, rng, emit, moveKey, attackType, makesContact);

        if (defender.IsSemiInvulnerable && !IsSemiInvulnerableBypass(moveKey))
        {
            await emit(BattleEvent.MessageLine($"{defender.Data.Name}은(는) 모습을 감춰 공격을 피했다!"));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "miss", target: "opponent", presentationKey: presentationKey));
            return;
        }

        if (MoveRuleMetadata.IsProtectionMove(moveKey) && moveKey != "kings-shield")
        {
            if (attacker.TryActivateProtection(moveKey, rng))
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) {move.Name}으로 몸을 지켰다!"));
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                    attackType, "shield", target: "self", presentationKey: presentationKey));
            }
            else
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 {move.Name}은(는) 실패했다!"));
            }
            return;
        }

        if (MoveRuleMetadata.ChangesToShieldForm(moveKey))
        {
            if (!attacker.TryActivateProtection(moveKey, rng)) return;
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) {move.Name}으로 몸을 지켰다!"));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "shield", target: "self", presentationKey: presentationKey));
            return;
        }

        if (defender.IsProtected
            && TargetsOpponent(move)
            && !MoveRuleMetadata.BypassesProtection(moveKey))
        {
            string protectionName = defender.ActiveProtectionMoveKey != null
                && MoveDatabase.All.TryGetValue(defender.ActiveProtectionMoveKey, out var protectionMove)
                ? protectionMove.Name
                : "방어 기술";
            await emit(BattleEvent.MessageLine($"{defender.Data.Name}은(는) {protectionName}(으)로 기술을 막았다!"));
            if (makesContact)
            {
                switch (MoveRuleMetadata.GetProtectionEffect(defender.ActiveProtectionMoveKey ?? "protect"))
                {
                    case ProtectionEffect.KingsShield:
                    {
                        int before = attacker.StatStages["attack"];
                        attacker.ChangeStage("attack", -2, causedByOpponent: true);
                        if (attacker.StatStages["attack"] < before)
                        {
                            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 크게 떨어졌다!"));
                            string? reaction = attacker.TriggerStatDropAbility();
                            if (reaction != null) await emit(BattleEvent.MessageLine(reaction));
                        }
                        break;
                    }
                    case ProtectionEffect.Obstruct:
                    {
                        int before = attacker.StatStages["defense"];
                        attacker.ChangeStage("defense", -2, causedByOpponent: true);
                        if (attacker.StatStages["defense"] < before)
                            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 방어가 크게 떨어졌다!"));
                        break;
                    }
                    case ProtectionEffect.SpikyShield:
                    {
                        int damage = Math.Max(1, attacker.MaxHp / 8);
                        attacker.CurrentHp = Math.Max(0, attacker.CurrentHp - damage);
                        if (attacker.CurrentHp == 0) attacker.MarkFainted();
                        await emit(BattleEvent.MessageLine(
                            $"{attacker.Data.Name}은(는) 가시방벽에 찔려 데미지를 입었다!"));
                        break;
                    }
                    case ProtectionEffect.BanefulBunker:
                        if (attacker.Status == StatusCondition.None)
                        {
                            attacker.ApplyAilment("poison", rng, defender);
                            if (attacker.Status == StatusCondition.Poison)
                                await emit(BattleEvent.MessageLine(
                                    $"{attacker.Data.Name}은(는) 독가시방벽 때문에 독 상태가 되었다!"));
                        }
                        break;
                }
            }
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "shield", target: "opponent", presentationKey: presentationKey));
            return;
        }

        if (IsBlockedByAbility(defender, move))
        {
            await emit(BattleEvent.MessageLine($"{defender.Data.Name}은(는) {defender.SelectedAbility}으로 기술을 막았다!"));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "immune", target: "opponent", presentationKey: presentationKey,
                statusResult: "immune"));
            return;
        }

        bool revealedImmunity = defender.TypeImmunityRevealed
            && attackType is PokemonType.Normal or PokemonType.Fighting or PokemonType.Psychic;
        bool bypassesGroundImmunity = moveKey == "thousand-arrows"
            && attackType == PokemonType.Ground;
        if (TargetsOpponent(move) && defender.IsImmuneToMoveType(attackType)
            && !revealedImmunity && !bypassesGroundImmunity)
        {
            var (absorbed, absorbMessage) = defender.TryAbsorb(attackType);
            context.WasAbsorbed = absorbed;
            if (absorbMessage != null)
            {
                await emit(BattleEvent.MessageLine(absorbMessage));
            }
            else
            {
                await emit(BattleEvent.MessageLine($"{defender.Data.Name}은(는) {defender.SelectedAbility}으로 기술을 무효화했다!"));
            }
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "immune", target: "opponent", presentationKey: presentationKey,
                statusResult: "immune"));
            return;
        }

        if (!move.IsStatus && move.Power > 0)
        {
            int attackStat = GetAttackStat(attacker, defender, moveKey, move);
            int defenseStat = GetDefenseStat(attacker, defender, moveKey, move);
            double power = MoveRuleMetadata.EffectivePower(moveKey, move, attacker, defender);
            bool stab = attacker.HasType(attackType);
            if (stab) power *= 1.5;
            if (attacker.ChargeBoostActive && attackType == PokemonType.Electric)
            {
                power *= 2;
                attacker.ClearChargeBoost();
            }

            var powerContext = new BattlePowerContext(
                attacker, defender, move, attackType, makesContact, power, moveKey);
            foreach (var handler in effectHandlers) handler.ModifyPower(powerContext);
            power = powerContext.Power;

            int hitCount = RollHitCount(attacker, move);
            for (int i = 0; i < hitCount; i++)
            {
                if (defender.IsFainted) break;

                bool isCritical = RollCriticalHit(attacker, defender, moveKey);
                if (isCritical)
                {
                    await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 급소에 맞았다!"));
                }
                int hpBefore = defender.CurrentHp;
                int scaledPower = (int)(((2.0 * attacker.Level / 5 + 2)
                    * power
                    * ((double)attackStat / Math.Max(defenseStat, 1))) / 50) + 2;
                if (isCritical && attacker.SelectedAbility == "스나이퍼") scaledPower = (int)(scaledPower * 1.5);
                defender.TakeDamage(
                    scaledPower,
                    attackType,
                    move.IsSpecial,
                    isCritical,
                    MoveRuleMetadata.SecondaryAttackType(moveKey),
                    moveKey == "freeze-dry"
                        && defender.HasType(PokemonType.Water) ? 2.0 : 1.0,
                    ignoresGroundImmunity: bypassesGroundImmunity);
                context.LastHitDamage = hpBefore - defender.CurrentHp;
                context.TotalDamage += context.LastHitDamage;
                context.ActualHits++;
                string? criticalReaction = defender.TriggerCriticalHitAbility();
                if (criticalReaction != null) await emit(BattleEvent.MessageLine(criticalReaction));
                await emit(BattleEvent.MoveStep(
                    BattleEventPhase.Impact,
                    attacker,
                    defender,
                    attackerIsHero,
                    move,
                    moveKey,
                    attackType,
                    effectKind,
                    target: "opponent",
                    presentationKey: presentationKey,
                    hitIndex: i + 1,
                    hitCount: hitCount,
                    damage: context.LastHitDamage,
                    hpBefore: hpBefore,
                    hpAfter: defender.CurrentHp,
                    isCritical: isCritical,
                    effectiveness: defender.LastMultiplier,
                    statusResult: defender.Status.ToString()));
                if (context.LastHitDamage > 0)
                {
                    await emit(BattleEvent.MessageLine(
                        $"{defender.Data.Name}에게 {context.LastHitDamage} 데미지를 입혔다!", 650));
                }
                foreach (var handler in effectHandlers) await handler.AfterHitAsync(context);
                if (attacker.IsFainted) break;
            }

            foreach (var handler in effectHandlers) await handler.AfterDamageAsync(context);

            if (move.MaxHits > 1) await emit(BattleEvent.MessageLine($"{context.ActualHits}번 맞았다!", 1000));
            var effectivenessLine = EffectivenessLine(defender.LastMultiplier);
            if (effectivenessLine != null) await emit(BattleEvent.MessageLine(effectivenessLine));
            if (defender.SurvivedByEndure) await emit(BattleEvent.MessageLine($"{defender.Data.Name}은(는) 버텨냈다!"));
            foreach (var handler in effectHandlers) await handler.AfterDamageResultAsync(context);
        }
        else if (move.IsStatus)
        {
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact,
                attacker,
                defender,
                attackerIsHero,
                move,
                moveKey,
                attackType,
                effectKind,
                target: TargetsOpponent(move) ? "opponent" : "self",
                presentationKey: presentationKey,
                hpBefore: defender.CurrentHp,
                hpAfter: defender.CurrentHp,
                statusResult: defender.Status.ToString()));
        }

        foreach (var handler in effectHandlers) await handler.AfterMoveAsync(context);
        await emit(BattleEvent.MoveStep(
            BattleEventPhase.Recovery,
            attacker,
            defender,
            attackerIsHero,
            move,
            moveKey,
            attackType,
            effectKind,
            target: TargetsOpponent(move) ? "opponent" : "self",
            presentationKey: presentationKey,
            hpAfter: defender.CurrentHp,
            statusResult: defender.Status.ToString()));
        if (context.RequestSwitch)
        {
            result.ForcedSwitchPokemon = context.SwitchPokemon;
            result.ForcedSwitchReason = context.SwitchReason;
        }
    }

    private static int GetAttackStat(Pokemon attacker, Pokemon defender, string moveKey, Move move)
    {
        if (moveKey == "body-press") return attacker.EffectiveDef;
        if (moveKey == "foul-play") return defender.EffectiveAtkAgainst(attacker);
        return move.IsSpecial
            ? attacker.EffectiveSpAtkAgainst(defender)
            : attacker.EffectiveAtkAgainst(defender);
    }

    private static int GetDefenseStat(Pokemon attacker, Pokemon defender, string moveKey, Move move)
    {
        if (moveKey is "secret-sword" or "psystrike" or "psyshock") return defender.EffectiveDef;
        return move.IsSpecial
            ? defender.EffectiveSpDefAgainst(attacker)
            : defender.EffectiveDef;
    }

    private int RollHitCount(Pokemon attacker, Move move)
    {
        if (move.MinHits == move.MaxHits) return move.MinHits;
        if (attacker.SelectedAbility == "스킬링크") return move.MaxHits;
        if (move.MinHits == 2 && move.MaxHits == 5)
        {
            int roll = rng.Next(100);
            if (roll < 35) return 2;
            if (roll < 70) return 3;
            if (roll < 85) return 4;
            return 5;
        }
        return rng.Next(move.MinHits, move.MaxHits + 1);
    }

    private int CriticalStage(Pokemon attacker, string moveKey)
    {
        if (MoveRuleMetadata.GuaranteesCriticalHit(moveKey)) return 3;

        int stage = 0;
        if (MoveRuleMetadata.HasHighCriticalRate(moveKey)) stage++;
        if (attacker.SelectedAbility == "대운") stage++;
        if (attacker.HeldItem == "대파") stage += 2;
        return Math.Min(stage, 3);
    }

    private bool RollCriticalHit(Pokemon attacker, Pokemon defender, string moveKey)
    {
        if (defender.IsCriticalImmune()) return false;

        int stage = CriticalStage(attacker, moveKey);
        int denominator = stage switch
        {
            3 => 1,
            2 => 2,
            1 => 8,
            _ => 24
        };
        return denominator == 1 || rng.Next(denominator) == 0;
    }

    private static int MovePriority(Pokemon pokemon, Move? move)
    {
        if (move == null) return 0;
        int priority = move.Priority;
        if (pokemon.SelectedAbility == "짓궂은마음" && move.IsStatus) priority++;
        if (pokemon.SelectedAbility == "질풍날개" && move.Type == PokemonType.Flying && pokemon.CurrentHp == pokemon.MaxHp) priority++;
        if (pokemon.SelectedAbility == "시간벌기") priority--;
        return priority;
    }

    private static bool IsBlockedByAbility(Pokemon defender, Move move)
    {
        if (defender.SelectedAbility == "방음"
            && (move.Name.Contains("소리") || move.Name.Contains("노래") || move.Name is "울음소리" or "하이퍼보이스")) return true;
        if (defender.SelectedAbility == "방진"
            && (move.Name.Contains("가루") || move.Name is "버섯포자" or "목화포자")) return true;
        if (defender.SelectedAbility == "방탄"
            && (move.Name.Contains("볼") || move.Name.Contains("탄") || move.Name.Contains("폭탄"))) return true;
        return false;
    }

    private static bool TargetsOpponent(Move move)
    {
        if (!move.IsStatus) return true;
        if (move.AilmentName != "none") return true;
        return move.StatChanges.Any(change => !change.TargetsSelf);
    }

    private static bool IsSemiInvulnerableBypass(string moveKey) =>
        moveKey is "gust" or "twister" or "thunder" or "hurricane" or "smack-down"
            or "thousand-arrows";

    private static string? EffectivenessLine(double multiplier)
    {
        if (multiplier >= 2.0) return "효과가 굉장했다!";
        if (multiplier > 0 && multiplier < 1.0) return "효과가 별로인 듯하다...";
        if (multiplier == 0) return "효과가 없는 것 같다...";
        return null;
    }
}