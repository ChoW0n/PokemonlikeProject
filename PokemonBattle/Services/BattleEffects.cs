using PokemonBattle.Models;

namespace PokemonBattle.Services;

public sealed record BattleEvent(
    string? Message = null,
    int BaseDelayMs = 1400,
    string? EffectKind = null,
    bool AttackerIsHero = false,
    PokemonType? EffectType = null)
{
    public static BattleEvent MessageLine(string message, int baseDelayMs = 1400) =>
        new(Message: message, BaseDelayMs: baseDelayMs);

    public static BattleEvent Effect(string kind, bool attackerIsHero, PokemonType type) =>
        new(EffectKind: kind, AttackerIsHero: attackerIsHero, EffectType: type);
}

public sealed class BattleTurnResult
{
    public Pokemon? FaintedPokemon { get; set; }
}

public sealed record BattleTurnPlan(string? EnemyMoveKey, bool HeroFirst);

public sealed class BattleEffectContext
{
    public Pokemon Attacker { get; }
    public Pokemon Defender { get; }
    public Move Move { get; }
    public string MoveKey { get; }
    public PokemonType AttackType { get; }
    public bool MakesContact { get; }
    public bool AttackerIsHero { get; }
    public Random Random { get; }
    public Func<BattleEvent, Task> Emit { get; }
    public int TotalDamage { get; set; }
    public int LastHitDamage { get; set; }
    public int ActualHits { get; set; }
    public bool WasAbsorbed { get; set; }

    public BattleEffectContext(
        Pokemon attacker,
        Pokemon defender,
        Move move,
        bool attackerIsHero,
        Random random,
        Func<BattleEvent, Task> emit,
        string moveKey,
        PokemonType attackType,
        bool makesContact)
    {
        Attacker = attacker;
        Defender = defender;
        Move = move;
        MoveKey = moveKey;
        AttackType = attackType;
        MakesContact = makesContact;
        AttackerIsHero = attackerIsHero;
        Random = random;
        Emit = emit;
    }

    public Task ShowMessage(string message, int baseDelayMs = 1400) =>
        Emit(BattleEvent.MessageLine(message, baseDelayMs));
}

public sealed class BattlePowerContext
{
    public Pokemon Attacker { get; }
    public Pokemon Defender { get; }
    public Move Move { get; }
    public PokemonType AttackType { get; }
    public bool MakesContact { get; }
    public double Power { get; set; }

    public BattlePowerContext(
        Pokemon attacker,
        Pokemon defender,
        Move move,
        PokemonType attackType,
        bool makesContact,
        double power)
    {
        Attacker = attacker;
        Defender = defender;
        Move = move;
        AttackType = attackType;
        MakesContact = makesContact;
        Power = power;
    }
}

public sealed class BattleEndOfTurnContext
{
    public Pokemon Pokemon { get; }
    public Func<BattleEvent, Task> Emit { get; }

    public BattleEndOfTurnContext(Pokemon pokemon, Func<BattleEvent, Task> emit)
    {
        Pokemon = pokemon;
        Emit = emit;
    }

    public Task ShowMessage(string message, int baseDelayMs = 1400) =>
        Emit(BattleEvent.MessageLine(message, baseDelayMs));
}

public interface IBattleEffectHandler
{
    int Order => 0;
    void ModifyPower(BattlePowerContext context) { }
    Task AfterHitAsync(BattleEffectContext context) => Task.CompletedTask;
    Task AfterDamageAsync(BattleEffectContext context) => Task.CompletedTask;
    Task AfterDamageResultAsync(BattleEffectContext context) => Task.CompletedTask;
    Task AfterMoveAsync(BattleEffectContext context) => Task.CompletedTask;
    Task EndOfTurnAsync(BattleEndOfTurnContext context) => Task.CompletedTask;
}