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

    public int EffectiveSpeed(Pokemon pokemon)
    {
        double speed = pokemon.EffectiveSpd;
        if (pokemon.HeldItem == "구애스카프") speed *= 1.5;
        return (int)speed;
    }

    public double PreviewMultiplier(Move move, Pokemon target)
    {
        double multiplier = TypeChart.GetMultiplier(move.Type, target.Data.Type1);
        if (target.Data.Type2 != null) multiplier *= TypeChart.GetMultiplier(move.Type, target.Data.Type2.Value);
        if (target.SelectedAbility == "부유" && move.Type == PokemonType.Ground) multiplier = 0;
        if (target.SelectedAbility == "피뢰침" && move.Type == PokemonType.Electric) multiplier = 0;
        return multiplier;
    }

    public void InitializeWeather(Pokemon hero, Pokemon enemy)
    {
        if (hero.SelectedAbility == "가뭄" || enemy.SelectedAbility == "가뭄") BattleWeather.Current = "쾌청";
        else if (hero.SelectedAbility == "잔비" || enemy.SelectedAbility == "잔비") BattleWeather.Current = "비";
        else BattleWeather.Current = "맑음";
    }

    public void PrepareSwitchOut(Pokemon pokemon)
    {
        if (pokemon.SelectedAbility == "재생력" && !pokemon.IsFainted)
        {
            int heal = pokemon.MaxHp / 3;
            pokemon.CurrentHp = Math.Min(pokemon.MaxHp, pokemon.CurrentHp + heal);
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
        bool heroFirst = (heroMove?.Priority ?? 0) != (enemyMove?.Priority ?? 0)
            ? (heroMove?.Priority ?? 0) > (enemyMove?.Priority ?? 0)
            : EffectiveSpeed(hero) >= EffectiveSpeed(enemy);
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
                bool stab = move.Type == enemy.Data.Type1 || enemy.Data.Type2 == move.Type;
                double averageHits = (move.MinHits + move.MaxHits) / 2.0;
                score = move.Power * averageHits * (stab ? 1.5 : 1.0)
                    * PreviewMultiplier(move, hero)
                    * ((move.AlwaysHits ? 100 : move.Accuracy) / 100.0);
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
        foreach (var pokemon in activePokemon)
        {
            if (pokemon.IsFainted) continue;

            var statusMessage = pokemon.ApplyEndOfTurnStatusDamage();
            if (statusMessage != null) await emit(BattleEvent.MessageLine(statusMessage, 1100));

            var context = new BattleEndOfTurnContext(pokemon, emit);
            foreach (var handler in effectHandlers) await handler.EndOfTurnAsync(context);
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

        if (attacker.Flinched)
        {
            attacker.Flinched = false;
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 움찔해서 움직일 수 없었다!"));
            return result;
        }

        var (canAct, statusMessage) = attacker.CheckActionPrevention();
        if (statusMessage != null) await emit(BattleEvent.MessageLine(statusMessage));
        if (!canAct)
        {
            if (attacker.IsFainted) result.FaintedPokemon = attacker;
            return result;
        }

        if (moveKey == null)
        {
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 사용할 수 있는 기술이 없어 몸부림쳤다!"));
            int damage = Math.Max(1, (int)(((2.0 * attacker.Level / 5 + 2) * 50 * attacker.EffectiveAtk
                / Math.Max(defender.EffectiveDef, 1)) / 50) + 2);
            defender.CurrentHp = Math.Max(0, defender.CurrentHp - damage);
            defender.LastMultiplier = 1.0;
            defender.IsFainted = defender.CurrentHp == 0;
            await emit(BattleEvent.Effect(TypeColors.GetEffectKind(PokemonType.Normal, false), attackerIsHero, PokemonType.Normal));
        }
        else
        {
            if (!MoveDatabase.All.TryGetValue(moveKey, out var move) || !attacker.TryUseMove(moveKey))
            {
                await emit(BattleEvent.MessageLine($"{attacker.Data.Name}은(는) 그 기술을 사용할 수 없다!"));
                return result;
            }

            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 {move.Name}!"));
            await ExecuteMoveAsync(attacker, defender, move, attackerIsHero, emit);
        }

        if (defender.IsFainted) result.FaintedPokemon = defender;
        return result;
    }

    private async Task ExecuteMoveAsync(
        Pokemon attacker,
        Pokemon defender,
        Move move,
        bool attackerIsHero,
        Func<BattleEvent, Task> emit)
    {
        int effectiveAccuracy = attacker.SelectedAbility == "의욕" && !move.AlwaysHits
            ? (int)(move.Accuracy * 0.8)
            : move.Accuracy;
        bool hit = move.AlwaysHits || rng.Next(100) < effectiveAccuracy;
        if (!hit)
        {
            await emit(BattleEvent.MessageLine($"{attacker.Data.Name}의 공격이 빗나갔다!"));
            return;
        }

        string effectKind = TypeColors.GetEffectKind(move.Type, move.IsStatus);
        var context = new BattleEffectContext(attacker, defender, move, attackerIsHero, rng, emit);

        if (!move.IsStatus && move.Power > 0)
        {
            var (absorbed, absorbMessage) = defender.TryAbsorb(move.Type);
            if (absorbed)
            {
                context.WasAbsorbed = true;
                if (absorbMessage != null) await emit(BattleEvent.MessageLine(absorbMessage));
                return;
            }

            int attackStat = GetAttackStat(attacker, move.IsSpecial);
            int defenseStat = move.IsSpecial ? defender.EffectiveSpDef : defender.EffectiveDef;
            double power = move.Power;
            bool stab = move.Type == attacker.Data.Type1 || attacker.Data.Type2 == move.Type;
            if (stab) power *= 1.5;

            var powerContext = new BattlePowerContext(attacker, defender, move, power);
            foreach (var handler in effectHandlers) handler.ModifyPower(powerContext);
            power = powerContext.Power;

            int hitCount = RollHitCount(move);
            for (int i = 0; i < hitCount; i++)
            {
                if (defender.IsFainted) break;

                int hpBefore = defender.CurrentHp;
                int scaledPower = (int)(power * ((double)attackStat / Math.Max(defenseStat, 1)));
                defender.TakeDamage(scaledPower, move.Type);
                context.TotalDamage += hpBefore - defender.CurrentHp;
                context.ActualHits++;
                await emit(BattleEvent.Effect(effectKind, attackerIsHero, move.Type));
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
            await emit(BattleEvent.Effect(effectKind, attackerIsHero, move.Type));
        }

        foreach (var handler in effectHandlers) await handler.AfterMoveAsync(context);
    }

    private int GetAttackStat(Pokemon attacker, bool isSpecial)
    {
        if (attacker.SelectedAbility == "근성" && attacker.Status != StatusCondition.None)
        {
            return isSpecial ? attacker.SpAtk : (int)(attacker.Atk * 1.5);
        }
        return isSpecial ? attacker.EffectiveSpAtk : attacker.EffectiveAtk;
    }

    private int RollHitCount(Move move)
    {
        if (move.MinHits == move.MaxHits) return move.MinHits;
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

    private static string? EffectivenessLine(double multiplier)
    {
        if (multiplier >= 2.0) return "효과가 굉장했다!";
        if (multiplier > 0 && multiplier < 1.0) return "효과가 별로인 듯하다...";
        if (multiplier == 0) return "효과가 없는 것 같다...";
        return null;
    }
}