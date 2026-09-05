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
    private readonly BattleSideState heroSide = new();
    private readonly BattleSideState enemySide = new();
    private readonly HashSet<Pokemon> knownPokemon = new();
    private readonly Dictionary<Pokemon, bool> pokemonSides = new();
    private readonly BattleEnvironment environment;
    public RunMetaState? ActiveRunMeta { get; private set; }

    public BattleEngine(IEnumerable<IBattleEffectHandler> handlers)
        : this(new Random(), handlers)
    {
    }

    public BattleEngine(Random random, IEnumerable<IBattleEffectHandler> handlers)
    {
        rng = random;
        effectHandlers = handlers.OrderBy(handler => handler.Order).ToArray();
        environment = BattleEnvironmentContext.Active.Clone();
    }

    public string CurrentWeather
    {
        get
        {
            ActivateEnvironment();
            return BattleWeather.Current;
        }
    }

    public string CurrentField
    {
        get
        {
            ActivateEnvironment();
            return BattleField.Current;
        }
    }

    public int EffectiveSpeed(Pokemon pokemon, Pokemon? opponent = null)
    {
        ActivateEnvironment();
        double speed = pokemon.EffectiveSpdAgainst(opponent);
        if (pokemon.HeldItem == "구애스카프" && pokemon.HasActiveHeldItem(opponent)) speed *= 1.5;
        return (int)speed;
    }

    public void ConfigureRunMeta(RunMetaState? runMeta) =>
        ActiveRunMeta = runMeta;

    public bool CanSwitch(Pokemon active, Pokemon opponent)
    {
        ActivateEnvironment();
        if (active.Ingrained
            || active.BindingTurnsRemaining > 0
            || active.RampageMoveKey != null) return false;
        bool isGhostType = active.HasType(PokemonType.Ghost);
        if (opponent.HasActiveAbility("그림자밟기", active)
            && !isGhostType
            && active.SelectedAbility != "그림자밟기") return false;
        if (opponent.HasActiveAbility("개미지옥", active)
            && !active.HasType(PokemonType.Flying)
            && !isGhostType
            && active.SelectedAbility != "부유") return false;
        if (opponent.HasActiveAbility("자력", active)
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
        ActivateEnvironment();
        if (!isWildBattle || active.IsFainted) return false;
        if (active.HasActiveAbility("도주", opponent)) return true;
        return CanSwitch(active, opponent);
    }

    public double PreviewMultiplier(Move move, Pokemon target, Pokemon? attacker = null)
    {
        ActivateEnvironment();
        PokemonType attackType = attacker?.ResolveMoveType(move, target) ?? move.Type;
        double multiplier = TypeChart.GetMultiplier(attackType, target.CurrentType1);
        if (target.CurrentType2 != null) multiplier *= TypeChart.GetMultiplier(attackType, target.CurrentType2.Value);
        if (attacker?.HasActiveAbility("배짱", target) == true
            && attackType is PokemonType.Normal or PokemonType.Fighting
            && target.HasType(PokemonType.Ghost))
        {
            multiplier = target.CurrentType1 == PokemonType.Ghost
                ? 1.0
                : TypeChart.GetMultiplier(attackType, target.CurrentType1);
            if (target.CurrentType2 != null)
            {
                multiplier *= target.CurrentType2 == PokemonType.Ghost
                    ? 1.0
                    : TypeChart.GetMultiplier(attackType, target.CurrentType2.Value);
            }
        }
        if (target.IsImmuneToMoveType(attackType, attacker)) multiplier = 0;
        if (!target.IsAbilitySuppressedBy(attacker)
            && target.HasActiveAbility("불가사의부적", attacker) && multiplier > 0 && multiplier < 2.0) multiplier = 0;
        multiplier *= MoveRuleMetadata.AuraMultiplier(attackType, attacker, target);
        return multiplier;
    }

    public IReadOnlyList<string> InitializeWeather(
        Pokemon hero,
        Pokemon enemy,
        Pokemon? heroIllusionTarget = null,
        Pokemon? enemyIllusionTarget = null,
        string? initialWeather = null,
        string? initialField = null)
    {
        ActivateEnvironment();
        BattleWeather.Reset();
        BattleField.Reset();
        heroSide.Reset();
        enemySide.Reset();
        knownPokemon.Clear();
        pokemonSides.Clear();
        RegisterPokemon(hero, isHero: true);
        RegisterPokemon(enemy, isHero: false);
        var messages = new List<string>();
        if (initialWeather != null && initialWeather != BattleWeather.Clear)
        {
            BattleWeather.Set(initialWeather);
            messages.Add($"전장 각인으로 날씨가 {initialWeather}(으)로 고정되었다!");
        }
        if (initialField != null && initialField != BattleField.None)
        {
            BattleField.Set(initialField, turns: 5);
            messages.Add($"전장 각인으로 {initialField}이(가) 펼쳐졌다!");
        }
        var entrants = new[] { (Pokemon: hero, Opponent: enemy), (Pokemon: enemy, Opponent: hero) }
            .OrderByDescending(entry => EffectiveSpeed(entry.Pokemon, entry.Opponent));
        foreach (var entry in entrants)
        {
            var illusionTarget = ReferenceEquals(entry.Pokemon, hero)
                ? heroIllusionTarget
                : enemyIllusionTarget;
            messages.AddRange(ActivateSwitchIn(
                entry.Pokemon, entry.Opponent, illusionTarget,
                ReferenceEquals(entry.Pokemon, hero)));
        }
        if (initialWeather != null && BattleWeather.Current != initialWeather)
        {
            BattleWeather.Set(initialWeather);
            messages.Add($"전장 각인이 날씨를 {initialWeather}(으)로 되돌렸다!");
        }
        if (initialField != null && BattleField.Current != initialField)
        {
            BattleField.Set(initialField, turns: 5);
            messages.Add($"전장 각인이 {initialField}을(를) 유지했다!");
        }
        return messages;
    }

    public IReadOnlyList<string> ActivateSwitchIn(
        Pokemon entrant,
        Pokemon opponent,
        Pokemon? illusionTarget = null,
        bool isHeroSide = true)
    {
        ActivateEnvironment();
        var messages = new List<string>();
        RegisterPokemon(entrant, isHeroSide);
        RegisterPokemon(opponent, !isHeroSide);
        entrant.ResetFieldCounter();
        var side = isHeroSide ? heroSide : enemySide;
        messages.AddRange(ApplyEntryHazards(entrant, opponent, side));

        string originalName = entrant.Data.Name;
        if (entrant.TryTransformInto(opponent))
        {
            messages.Add($"{originalName}의 괴짜로 {entrant.Data.Name}으로 변신했다!");
        }
        else if (illusionTarget != null && entrant.TryActivateIllusion(illusionTarget))
        {
            messages.Add($"{originalName}은(는) 일루전으로 {entrant.Data.Name}으로 둔갑했다!");
        }

        if (entrant.HasActiveAbility("트레이스", opponent)
            && !opponent.IsFainted
            && !string.IsNullOrEmpty(opponent.SelectedAbility)
            && opponent.SelectedAbility != "트레이스")
        {
            entrant.SelectedAbility = opponent.SelectedAbility;
            messages.Add($"{entrant.Data.Name}의 트레이스로 상대의 {entrant.SelectedAbility}을(를) 복사했다!");
        }

        if (entrant.HasActiveAbility("통찰", opponent)
            && !opponent.IsFainted
            && opponent.HeldItem != "없음")
        {
            messages.Add($"{entrant.Data.Name}의 통찰로 상대가 {opponent.HeldItem}을(를) 지닌 것을 알아냈다!");
        }

        if (entrant.HasActiveAbility("예지몽", opponent))
        {
            var forewarnMove = opponent.CurrentPP.Keys
                .Where(MoveDatabase.All.ContainsKey)
                .Select(key => MoveDatabase.All[key])
                .OrderByDescending(move => move.Power)
                .FirstOrDefault();
            if (forewarnMove != null)
            {
                messages.Add($"{entrant.Data.Name}의 예지몽이 상대의 {forewarnMove.Name}을(를) 감지했다!");
            }
        }
        if (entrant.HasActiveAbility("위험예지", opponent))
        {
            bool danger = opponent.CurrentPP.Keys
                .Where(MoveDatabase.All.ContainsKey)
                .Select(key => MoveDatabase.All[key])
                .Any(move => !move.IsStatus && PreviewMultiplier(move, entrant, opponent) >= 2.0);
            if (danger)
            {
                messages.Add($"{entrant.Data.Name}의 위험예지로 위험한 기술을 감지했다!");
            }
        }

        string? weather = entrant.HasActiveAbility("가뭄", opponent) ? "쾌청"
            : entrant.HasActiveAbility("잔비", opponent) ? "비"
            : entrant.HasActiveAbility("모래날림", opponent) ? "모래바람"
            : entrant.HasActiveAbility("눈퍼뜨리기", opponent) ? "싸라기눈"
            : null;
        if (weather != null)
        {
            BattleWeather.Set(weather);
            messages.Add($"{entrant.Data.Name}의 {entrant.SelectedAbility}! 날씨가 {weather}(으)로 바뀌었다!");
        }
        if (entrant.UpdateWeatherForm(opponent))
        {
            messages.Add($"{entrant.Data.Name}의 기분파로 모습이 날씨에 맞게 변했다!");
        }

        if (entrant.HasActiveAbility("위협", opponent))
        {
            int before = opponent.StatStages["attack"];
            opponent.ChangeStage("attack", -1, causedByOpponent: true, opponent: entrant);
            if (opponent.StatStages["attack"] < before)
            {
                messages.Add($"{entrant.Data.Name}의 위협으로 {opponent.Data.Name}의 공격이 떨어졌다!");
                string? reaction = opponent.TriggerStatDropAbility(entrant);
                if (reaction != null) messages.Add(reaction);
            }
            else
            {
                messages.Add($"{opponent.Data.Name}은(는) 위협의 영향을 받지 않았다!");
            }
            if (opponent.HasActiveAbility("주눅", entrant))
            {
                opponent.ChangeStage("speed", 1);
                messages.Add($"{opponent.Data.Name}의 주눅으로 속도가 올라갔다!");
            }
        }

        if (entrant.HasActiveAbility("다운로드", opponent))
        {
            string stat = opponent.EffectiveDef <= opponent.EffectiveSpDef ? "attack" : "special-attack";
            entrant.ChangeStage(stat, 1);
            messages.Add($"{entrant.Data.Name}의 다운로드로 {(stat == "attack" ? "공격" : "특공")}이 올랐다!");
        }

        if (entrant.HasActiveAbility("슬로스타트", opponent))
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
        ActivateEnvironment();
        string? enemyMoveKey = PickEnemyMove(enemy, enemyMoveKeys, hero);
        var heroMove = heroMoveKey == null ? null : MoveDatabase.All[heroMoveKey];
        var enemyMove = enemyMoveKey == null ? null : MoveDatabase.All[enemyMoveKey];
        int heroPriority = MovePriority(hero, heroMove);
        int enemyPriority = MovePriority(enemy, enemyMove);
        bool heroFirst = heroPriority != enemyPriority
            ? heroPriority > enemyPriority
            : BattleField.TrickRoomActive
                ? EffectiveSpeed(hero, enemy) <= EffectiveSpeed(enemy, hero)
                : EffectiveSpeed(hero, enemy) >= EffectiveSpeed(enemy, hero);
        return new BattleTurnPlan(enemyMoveKey, heroFirst);
    }

    public string? PickEnemyMove(
        Pokemon enemy,
        IReadOnlyCollection<string> moveKeys,
        Pokemon hero,
        int aiGrade = 3)
    {
        ActivateEnvironment();
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
                bool ailmentBlocked = aiGrade >= 1
                    && move.AilmentName != "none"
                    && (hero.Status != StatusCondition.None
                        || hero.IsImmuneToAilment(move.AilmentName, enemy));
                int opponentStatChangeBonus = move.StatChanges
                    .Count(change => !change.TargetsSelf) * 15;
                int selfBoostBonus = move.StatChanges
                    .Count(change => change.TargetsSelf && change.Change > 0) * 15;
                bool selfBoostIsDiscouraged = aiGrade >= 2
                    && (enemy.CurrentHp < enemy.MaxHp / 2.0
                    || IsTypeDisadvantaged(enemy, hero));
                string? weather = MoveRuleMetadata.WeatherForMove(key);
                string? field = MoveRuleMetadata.FieldForMove(key);
                bool environmentAlreadyActive = aiGrade >= 2
                    && ((weather != null && weather == BattleWeather.Current)
                    || (field != null && field == BattleField.Current));
                // 이미 걸렸거나 면역인 상태 이상은 선택하지 않는다.
                score = ailmentBlocked || environmentAlreadyActive
                    ? 0
                    : 30 + opponentStatChangeBonus
                        + (selfBoostIsDiscouraged ? 0 : selfBoostBonus)
                        + (move.AilmentName != "none" ? 20 : 0);
            }
            else
            {
                PokemonType attackType = MoveRuleMetadata.ResolveMoveType(key, move, enemy, hero);
                double averageHits = (move.MinHits + move.MaxHits) / 2.0;
                bool stab = enemy.HasType(attackType);
                double effectivePower = MoveRuleMetadata.EffectivePower(key, move, enemy, hero)
                    * averageHits
                    * (stab ? 1.5 : 1.0);
                int attackStat = move.IsSpecial
                    ? enemy.EffectiveSpAtkAgainst(hero)
                    : enemy.EffectiveAtkAgainst(hero);
                int defenseStat = move.IsSpecial
                    ? hero.EffectiveSpDefAgainst(enemy)
                    : hero.EffectiveDefAgainst(enemy);
                double estimatedDamage = (((2.0 * enemy.Level / 5 + 2)
                    * effectivePower
                    * ((double)attackStat / Math.Max(defenseStat, 1))) / 50) + 2;
                estimatedDamage *= PreviewMultiplier(move, hero, enemy);
                double accuracy = move.AlwaysHits
                    ? 100
                    : MoveRuleMetadata.EffectiveAccuracy(key, move, enemy, hero);
                score = estimatedDamage * (accuracy / 100.0);
                // 한 번에 쓰러뜨릴 수 있으면 높은 명중률을 함께 우선한다.
                if (aiGrade >= 3 && estimatedDamage >= hero.CurrentHp * 1.15)
                    score += 1_000_000 + accuracy * 1_000;
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
        ActivateEnvironment();
        var active = activePokemon.ToArray();
        for (int index = 0; index < active.Length; index++)
        {
            var pokemon = active[index];
            RegisterPokemon(pokemon, index == 0);
            if (pokemon.IsFainted) continue;

            var statusMessage = pokemon.ApplyEndOfTurnStatusDamage();
            if (statusMessage != null) await emit(BattleEvent.TurnEnd(statusMessage, 1100));

            var opponent = active.FirstOrDefault(candidate =>
                !ReferenceEquals(candidate, pokemon) && !candidate.IsFainted);
            var context = new BattleEndOfTurnContext(
                pokemon, emit, opponent, rng, ActiveRunMeta, isHero: index == 0);
            foreach (var handler in effectHandlers) await handler.EndOfTurnAsync(context);
            pokemon.AdvanceTurn();
        }

        foreach (var pokemon in knownPokemon)
        {
            if (!active.Contains(pokemon))
                pokemon.AdvancePendingDelayedAttackTurn();
        }

        if (BattleWeather.AdvanceTurn())
        {
            await emit(BattleEvent.TurnEnd("날씨의 효과가 사라졌다!", 900));
        }
        if (BattleField.AdvanceTurn())
        {
            await emit(BattleEvent.TurnEnd("필드의 효과가 사라졌다!", 900));
        }

        // 시한 공격은 시전자의 행동과 무관하게 턴 종료에 처리한다.
        foreach (var attacker in knownPokemon.ToArray())
        {
            if (attacker.PendingDelayedAttackKey == null
                || attacker.PendingDelayedAttackTurns > 0) continue;

            string? delayedKey = attacker.ConsumePendingDelayedAttack(out Pokemon? recordedTarget);
            if (delayedKey == null) continue;

            Pokemon? target = recordedTarget is { IsFainted: false }
                ? recordedTarget
                : FindCurrentOpponent(attacker, active);
            if (target == null || target.IsFainted)
            {
                await emit(BattleEvent.MessageLine(
                    $"{attacker.Data.Name}의 {MoveDatabase.All[delayedKey].Name}의 시한 공격은 대상이 없어 사라졌다!"));
                continue;
            }

            bool attackerIsHero = pokemonSides.TryGetValue(attacker, out bool isHero) && isHero;
            var delayedResult = new BattleTurnResult();
            var delayedMove = MoveDatabase.All[delayedKey];
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}의 {delayedMove.Name}의 시한 공격이 떨어졌다!"));
            await ExecuteMoveAsync(
                attacker,
                target,
                delayedMove,
                delayedKey,
                attackerIsHero,
                emit,
                delayedResult,
                isContinuation: true);
            await EmitFaintEventsAsync(delayedResult, attacker, attackerIsHero, emit);
        }
    }

    public async Task ApplyEndOfBattleEffectsAsync(
        IEnumerable<Pokemon> pokemon,
        Func<BattleEvent, Task> emit)
    {
        ActivateEnvironment();
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
        Func<BattleEvent, Task> emit,
        bool? attackerMovedFirst = null)
    {
        ActivateEnvironment();
        RegisterPokemon(attacker, attackerIsHero);
        RegisterPokemon(defender, !attackerIsHero);
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

            if (!isContinuation
                && TargetsOpponent(move)
                && defender.HasActiveAbility("프레셔", attacker)
                && attacker.CurrentPP.TryGetValue(executingMoveKey, out int remainingPp))
            {
                attacker.CurrentPP[executingMoveKey] = Math.Max(0, remainingPp - 1);
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
            await ExecuteMoveAsync(
                attacker, defender, move, executingMoveKey, attackerIsHero, emit, result,
                isContinuation, attackerMovedFirst);
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

    private void RegisterPokemon(Pokemon pokemon, bool isHero)
    {
        knownPokemon.Add(pokemon);
        pokemonSides[pokemon] = isHero;
    }

    private Pokemon? FindCurrentOpponent(Pokemon attacker, IReadOnlyList<Pokemon> active)
    {
        bool attackerIsHero = pokemonSides.TryGetValue(attacker, out bool isHero) && isHero;
        return active.FirstOrDefault(candidate =>
            !ReferenceEquals(candidate, attacker)
            && !candidate.IsFainted
            && (!pokemonSides.TryGetValue(candidate, out bool candidateIsHero)
                || candidateIsHero != attackerIsHero));
    }

    private static async Task EmitFaintEventsAsync(
        BattleTurnResult result,
        Pokemon attacker,
        bool attackerIsHero,
        Func<BattleEvent, Task> emit)
    {
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
        bool isContinuation = false,
        bool? attackerMovedFirst = null,
        bool isReflected = false)
    {
        if (attacker.UpdateFormForMove(moveKey, move.IsStatus))
        {
            string form = attacker.IsAlternateForm ? "공격모드" : "방어모드";
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}의 배틀스위치로 {form}로 모습이 변했다!"));
        }

        PokemonType attackType = MoveRuleMetadata.ResolveMoveType(
            moveKey, move, attacker, defender);
        bool makesContact = MoveRuleMetadata.MakesContact(moveKey, move)
            && !(attacker.HasActiveHeldItem(defender) && attacker.HeldItem == "보호패드");
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
        if (moveKey is "self-destruct" or "explosion" or "misty-explosion"
            && (attacker.HasActiveAbility("습기", defender)
                || defender.HasActiveAbility("습기", attacker)))
        {
            await emit(BattleEvent.MessageLine(
                $"{attacker.Data.Name}은(는) 습기 때문에 폭발할 수 없다!"));
            return;
        }
        bool hit = move.AlwaysHits || attacker.HasActiveAbility("노가드", defender)
            || defender.HasActiveAbility("노가드", attacker)
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
            attacker, defender, move, attackerIsHero, rng, emit, moveKey, attackType, makesContact,
            attackerIsHero ? heroSide : enemySide,
            attackerIsHero ? enemySide : heroSide);

        foreach (var handler in effectHandlers) await handler.BeforeMoveAsync(context);
        if (context.MoveFailed)
        {
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact,
                attacker,
                defender,
                attackerIsHero,
                move,
                moveKey,
                attackType,
                "miss",
                target: "opponent",
                presentationKey: presentationKey,
                hpBefore: defender.CurrentHp,
                hpAfter: defender.CurrentHp,
                statusResult: defender.Status.ToString()));
            return;
        }

        if (!isReflected && move.IsStatus && TargetsOpponent(move)
            && defender.HasActiveAbility("매직미러", attacker))
        {
            await emit(BattleEvent.MessageLine(
                $"{defender.Data.Name}의 매직미러가 {move.Name}을(를) 되받아쳤다!"));
            await ExecuteMoveAsync(
                defender, attacker, move, moveKey, !attackerIsHero, emit, result,
                isContinuation, attackerMovedFirst, isReflected: true);
            return;
        }

        if (move.IsStatus && TargetsOpponent(move)
            && defender.HasSubstitute
            && MoveRuleMetadata.GetRule(moveKey, move).Kind != MoveRuleKind.HazardPlacement)
        {
            await emit(BattleEvent.MessageLine(
                $"{defender.Data.Name}의 대타가 {move.Name}을(를) 막았다!"));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "shield", target: "opponent", presentationKey: presentationKey,
                statusResult: "blocked"));
            return;
        }

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
                        attacker.ChangeStage(
                            "attack",
                            -2,
                            causedByOpponent: true,
                            opponent: defender);
                        if (attacker.StatStages["attack"] < before)
                        {
                            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 크게 떨어졌다!"));
                        string? reaction = attacker.TriggerStatDropAbility(defender);
                            if (reaction != null) await emit(BattleEvent.MessageLine(reaction));
                        }
                        break;
                    }
                    case ProtectionEffect.Obstruct:
                    {
                        int before = attacker.StatStages["defense"];
                        attacker.ChangeStage(
                            "defense",
                            -2,
                            causedByOpponent: true,
                            opponent: defender);
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
                            string? immunityMessage = attacker.GetAilmentImmunityMessage("poison", defender);
                            if (immunityMessage != null)
                            {
                                await emit(BattleEvent.MessageLine(immunityMessage));
                            }
                            else
                            {
                                attacker.ApplyAilment("poison", rng, defender);
                            }
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

        if (IsBlockedByAbility(defender, move, attacker))
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
        if (TargetsOpponent(move) && defender.IsImmuneToWindMove(moveKey, attacker))
        {
            var (absorbed, absorbMessage) = defender.TryAbsorb(attackType, moveKey);
            context.WasAbsorbed = absorbed;
            if (absorbMessage != null) await emit(BattleEvent.MessageLine(absorbMessage));
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, "immune", target: "opponent", presentationKey: presentationKey,
                statusResult: "immune"));
            return;
        }
        if (TargetsOpponent(move) && defender.IsImmuneToMoveType(attackType, attacker)
            && !revealedImmunity && !bypassesGroundImmunity)
        {
            var (absorbed, absorbMessage) = defender.TryAbsorb(attackType, moveKey);
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

        if (moveKey is "counter" or "mirror-coat")
        {
            bool correctDamageType = attacker.LastDamageTakenAmountThisTurn > 0
                && attacker.LastDamageTakenWasSpecialThisTurn == (moveKey == "mirror-coat");
            int damage = correctDamageType ? attacker.LastDamageTakenAmountThisTurn * 2 : 0;
            int hpBefore = defender.CurrentHp;
            if (damage > 0)
            {
                defender.ApplyDirectDamage(damage, attacker, moveKey == "mirror-coat");
                context.LastHitDamage = hpBefore - defender.CurrentHp;
                context.TotalDamage = context.LastHitDamage;
                context.ActualHits = 1;
                await emit(BattleEvent.MessageLine(
                    $"{attacker.Data.Name}은(는) 받은 피해를 2배로 되돌려주었다!"));
            }
            else
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 {move.Name}은(는) 실패했다!"));
            }
            await emit(BattleEvent.MoveStep(
                BattleEventPhase.Impact, attacker, defender, attackerIsHero, move, moveKey,
                attackType, damage > 0 ? effectKind : "miss", target: "opponent",
                presentationKey: presentationKey, damage: context.LastHitDamage,
                hpBefore: hpBefore, hpAfter: defender.CurrentHp,
                statusResult: defender.Status.ToString()));
            return;
        }

        if (!move.IsStatus && move.Power > 0)
        {
            bool movedFirst = attackerMovedFirst
                ?? EffectiveSpeed(attacker, defender) >= EffectiveSpeed(defender, attacker);
            double power = MoveRuleMetadata.EffectivePower(
                moveKey, move, attacker, defender, movedFirst);
            bool stab = attacker.HasType(attackType);
            if (stab) power *= 1.5;
            if (attacker.ChargeBoostActive && attackType == PokemonType.Electric)
            {
                power *= 2;
                attacker.ClearChargeBoost();
            }

            var powerContext = new BattlePowerContext(
                attacker,
                defender,
                move,
                attackType,
                makesContact,
                power,
                moveKey,
                attackerMovedFirst ?? EffectiveSpeed(attacker, defender) >= EffectiveSpeed(defender, attacker),
                ActiveRunMeta,
                attackerIsHero);
            foreach (var handler in effectHandlers) handler.ModifyPower(powerContext);
            power = powerContext.Power;

            int hitCount = RollHitCount(attacker, move);
            for (int i = 0; i < hitCount; i++)
            {
                if (defender.IsFainted) break;

                bool isCritical = RollCriticalHit(attacker, defender, moveKey, attackerIsHero);
                if (isCritical)
                {
                    await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 급소에 맞았다!"));
                }
                // 급소마다 랭크업/랭크다운 무시 규칙을 적용해 스탯을 다시 계산한다.
                int attackStat = GetAttackStat(
                    attacker, defender, moveKey, move, ignoreNegativeStage: isCritical);
                int defenseStat = GetDefenseStat(
                    attacker, defender, moveKey, move, ignorePositiveStage: isCritical);
                int hpBefore = defender.CurrentHp;
                int scaledPower = (int)(((2.0 * attacker.Level / 5 + 2)
                    * power
                    * ((double)attackStat / Math.Max(defenseStat, 1))) / 50) + 2;
                if (isCritical && attacker.HasActiveAbility("스나이퍼", defender)) scaledPower = (int)(scaledPower * 1.5);
                defender.TakeDamage(
                    scaledPower,
                    attackType,
                    move.IsSpecial,
                    isCritical,
                    MoveRuleMetadata.SecondaryAttackType(moveKey),
                    moveKey == "freeze-dry"
                        && defender.HasType(PokemonType.Water) ? 2.0 : 1.0,
                    ignoresGroundImmunity: bypassesGroundImmunity,
                    attacker: attacker);
                context.LastHitDamage = hpBefore - defender.CurrentHp;
                context.TotalDamage += context.LastHitDamage;
                context.ActualHits++;
                string? criticalReaction = defender.TriggerCriticalHitAbility(attacker);
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

    private static IReadOnlyList<string> ApplyEntryHazards(
        Pokemon entrant,
        Pokemon opponent,
        BattleSideState side)
    {
        var messages = new List<string>();
        if (entrant.IsFainted) return messages;

        if (side.StealthRock)
        {
            double multiplier = TypeChart.GetMultiplier(PokemonType.Rock, entrant.CurrentType1);
            if (entrant.CurrentType2 != null)
                multiplier *= TypeChart.GetMultiplier(PokemonType.Rock, entrant.CurrentType2.Value);
            int damage = Math.Max(1, (int)(entrant.MaxHp * multiplier / 8));
            if (multiplier > 0)
            {
                entrant.CurrentHp = Math.Max(0, entrant.CurrentHp - damage);
                if (entrant.CurrentHp == 0) entrant.MarkFainted();
                messages.Add($"{entrant.Data.Name}은(는) 스텔스록에 의해 {damage}의 피해를 입었다!");
            }
        }

        if (entrant.IsFainted || !entrant.IsGrounded(opponent)) return messages;

        if (side.SpikesLayers > 0)
        {
            int damage = Math.Max(1, entrant.MaxHp * (side.SpikesLayers + 1) / 16);
            entrant.CurrentHp = Math.Max(0, entrant.CurrentHp - damage);
            if (entrant.CurrentHp == 0) entrant.MarkFainted();
            messages.Add($"{entrant.Data.Name}은(는) 압정뿌리기에 찔려 {damage}의 피해를 입었다!");
        }

        if (entrant.IsFainted) return messages;

        if (side.ToxicSpikesLayers > 0)
        {
            if (entrant.HasType(PokemonType.Poison))
            {
                side.ClearToxicSpikes();
                messages.Add($"{entrant.Data.Name}이(가) 독압정을 제거했다!");
            }
            else if (entrant.Status == StatusCondition.None)
            {
                string ailment = side.ToxicSpikesLayers >= 2 ? "toxic" : "poison";
                string? immunityMessage = entrant.GetAilmentImmunityMessage(ailment, opponent);
                if (immunityMessage != null)
                {
                    messages.Add(immunityMessage);
                }
                else
                {
                    entrant.ApplyAilment(ailment, opponent: opponent);
                    if (entrant.Status != StatusCondition.None)
                    {
                        messages.Add($"{entrant.Data.Name}은(는) 독압정 때문에 {(
                            ailment == "toxic" ? "맹독" : "독")} 상태가 되었다!");
                    }
                }
            }
        }

        if (!entrant.IsFainted && side.StickyWeb)
        {
            int before = entrant.StatStages["speed"];
            entrant.ChangeStage(
                "speed",
                -1,
                causedByOpponent: true,
                opponent: opponent);
            if (entrant.StatStages["speed"] < before)
                messages.Add($"{entrant.Data.Name}은(는) 끈적끈적네트에 걸려 스피드가 떨어졌다!");
        }
        return messages;
    }

    private static int GetAttackStat(
        Pokemon attacker,
        Pokemon defender,
        string moveKey,
        Move move,
        bool ignoreNegativeStage = false)
    {
        if (moveKey == "body-press")
        {
            return attacker.EffectiveDefAgainst(
                defender, ignoreNegativeStage: ignoreNegativeStage);
        }

        if (moveKey == "foul-play")
            return defender.EffectiveAtkAgainst(attacker, ignoreNegativeStage);

        return move.IsSpecial
            ? attacker.EffectiveSpAtkAgainst(
                defender, ignoreNegativeStage: ignoreNegativeStage)
            : attacker.EffectiveAtkAgainst(defender, ignoreNegativeStage);
    }

    private static int GetDefenseStat(
        Pokemon attacker,
        Pokemon defender,
        string moveKey,
        Move move,
        bool ignorePositiveStage = false)
    {
        if (moveKey is "secret-sword" or "psystrike" or "psyshock")
            return defender.EffectiveDefAgainst(attacker, ignorePositiveStage);
        return move.IsSpecial
            ? defender.EffectiveSpDefAgainst(attacker, ignorePositiveStage)
            : defender.EffectiveDefAgainst(attacker, ignorePositiveStage);
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

    private int CriticalStage(Pokemon attacker, string moveKey, bool attackerIsHero)
    {
        if (MoveRuleMetadata.GuaranteesCriticalHit(moveKey)) return 3;

        int stage = 0;
        if (MoveRuleMetadata.HasHighCriticalRate(moveKey)) stage++;
        if (attacker.SelectedAbility == "대운") stage++;
        if (attacker.HasActiveHeldItem() && attacker.HeldItem == "대파") stage += 2;
        if (attacker.HasActiveHeldItem() && attacker.HeldItem == "예리한손톱") stage++;
        if (attackerIsHero
            && ActiveRunMeta?.LegacyIds.Contains("hunters-eye") == true) stage++;
        return Math.Min(stage, 3);
    }

    private bool RollCriticalHit(
        Pokemon attacker,
        Pokemon defender,
        string moveKey,
        bool attackerIsHero)
    {
        if (defender.IsCriticalImmune(attacker)) return false;

        int stage = CriticalStage(attacker, moveKey, attackerIsHero);
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

    private static bool IsBlockedByAbility(Pokemon defender, Move move, Pokemon? attacker = null)
    {
        if (defender.HasActiveAbility("방음", attacker)
            && (move.Name.Contains("소리") || move.Name.Contains("노래") || move.Name is "울음소리" or "하이퍼보이스")) return true;
        if (defender.HasActiveAbility("방진", attacker)
            && (move.Name.Contains("가루") || move.Name is "버섯포자" or "목화포자")) return true;
        if (defender.HasActiveAbility("방탄", attacker)
            && (move.Name.Contains("볼") || move.Name.Contains("탄") || move.Name.Contains("폭탄"))) return true;
        return false;
    }

    private static bool TargetsOpponent(Move move)
    {
        if (!move.IsStatus) return true;
        if (move.AilmentName != "none") return true;
        return move.StatChanges.Any(change => !change.TargetsSelf);
    }

    private static bool IsTypeDisadvantaged(Pokemon defender, Pokemon opponent)
    {
        var opponentTypes = new[] { opponent.CurrentType1, opponent.CurrentType2 }
            .Where(type => type.HasValue)
            .Select(type => type!.Value);

        return opponentTypes.Any(attackType =>
        {
            double multiplier = TypeChart.GetMultiplier(attackType, defender.CurrentType1);
            if (defender.CurrentType2 != null)
                multiplier *= TypeChart.GetMultiplier(attackType, defender.CurrentType2.Value);
            return multiplier > 1.0;
        });
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

    private void ActivateEnvironment() =>
        BattleEnvironmentContext.Activate(environment);
}