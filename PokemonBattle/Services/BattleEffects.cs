using PokemonBattle.Models;

namespace PokemonBattle.Services;

public enum BattleEventPhase
{
    Message,
    Announce,
    Windup,
    Impact,
    Recovery,
    Status,
    TurnEnd,
    Switch,
    Faint
}

public sealed record BattleEvent(
    string? Message = null,
    int BaseDelayMs = 1400,
    string? EffectKind = null,
    bool AttackerIsHero = false,
    PokemonType? EffectType = null,
    string? MoveName = null,
    BattleEventPhase Phase = BattleEventPhase.Message,
    string? MoveKey = null,
    string? AttackerActorId = null,
    string? DefenderActorId = null,
    string? AttackerSpecies = null,
    string? DefenderSpecies = null,
    string? AttackerForm = null,
    string? DefenderForm = null,
    string? Target = null,
    string? MoveCategory = null,
    string? PresentationKey = null,
    int HitIndex = 0,
    int HitCount = 0,
    int Damage = 0,
    int HpBefore = 0,
    int HpAfter = 0,
    bool IsCritical = false,
    double Effectiveness = 1.0,
    string? StatusResult = null,
    string? AccessibleMessage = null)
{
    public static BattleEvent MessageLine(string message, int baseDelayMs = 1400) =>
        new(Message: message, BaseDelayMs: baseDelayMs);

    public static BattleEvent TurnEnd(string message, int baseDelayMs = 900) =>
        new(Message: message, BaseDelayMs: baseDelayMs, Phase: BattleEventPhase.TurnEnd);

    public static BattleEvent ActorStep(
        BattleEventPhase phase,
        Pokemon actor,
        bool actorIsHero,
        string? message = null) =>
        new(
            Message: message,
            BaseDelayMs: phase == BattleEventPhase.Faint ? 850 : 650,
            EffectKind: phase == BattleEventPhase.Faint ? "faint" : "switch",
            AttackerIsHero: actorIsHero,
            EffectType: actor.Data.Type1,
            MoveName: actor.Data.Name,
            Phase: phase,
            AttackerActorId: actor.ActorId,
            AttackerSpecies: actor.Data.EnglishName,
            AttackerForm: actor.FormKey,
            Target: "self",
            AccessibleMessage: message);

    public static BattleEvent Effect(
        string kind,
        bool attackerIsHero,
        PokemonType type,
        string? moveName = null) =>
        new(
            EffectKind: kind,
            AttackerIsHero: attackerIsHero,
            EffectType: type,
            MoveName: moveName,
            Phase: BattleEventPhase.Impact);

    public static BattleEvent MoveStep(
        BattleEventPhase phase,
        Pokemon attacker,
        Pokemon defender,
        bool attackerIsHero,
        Move move,
        string moveKey,
        PokemonType attackType,
        string effectKind,
        string? message = null,
        string? target = "opponent",
        string? presentationKey = null,
        int hitIndex = 0,
        int hitCount = 0,
        int damage = 0,
        int hpBefore = 0,
        int hpAfter = 0,
        bool isCritical = false,
        double effectiveness = 1.0,
        string? statusResult = null) =>
        new(
            Message: message,
            BaseDelayMs: phase == BattleEventPhase.Announce ? 1000 : phase == BattleEventPhase.Recovery ? 260 : 700,
            EffectKind: effectKind,
            AttackerIsHero: attackerIsHero,
            EffectType: attackType,
            MoveName: move.Name,
            Phase: phase,
            MoveKey: moveKey,
            AttackerActorId: attacker.ActorId,
            DefenderActorId: defender.ActorId,
            AttackerSpecies: attacker.Data.EnglishName,
            DefenderSpecies: defender.Data.EnglishName,
            AttackerForm: attacker.FormKey,
            DefenderForm: defender.FormKey,
            Target: target,
            MoveCategory: move.IsStatus ? "status" : move.IsSpecial ? "special" : "physical",
            PresentationKey: presentationKey ?? moveKey,
            HitIndex: hitIndex,
            HitCount: hitCount,
            Damage: damage,
            HpBefore: hpBefore,
            HpAfter: hpAfter,
            IsCritical: isCritical,
            Effectiveness: effectiveness,
            StatusResult: statusResult,
            AccessibleMessage: message);
}

public sealed class BattleTurnResult
{
    public Pokemon? FaintedPokemon { get; set; }
    public Pokemon? OtherFaintedPokemon { get; set; }
    public Pokemon? ForcedSwitchPokemon { get; set; }
    public string? ForcedSwitchReason { get; set; }
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
    public bool RequestSwitch { get; set; }
    public Pokemon? SwitchPokemon { get; set; }
    public string? SwitchReason { get; set; }

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
    public string MoveKey { get; }
    public PokemonType AttackType { get; }
    public bool MakesContact { get; }
    public double Power { get; set; }

    public BattlePowerContext(
        Pokemon attacker,
        Pokemon defender,
        Move move,
        PokemonType attackType,
        bool makesContact,
        double power,
        string moveKey = "")
    {
        Attacker = attacker;
        Defender = defender;
        Move = move;
        MoveKey = moveKey;
        AttackType = attackType;
        MakesContact = makesContact;
        Power = power;
    }
}

public sealed class BattleEndOfTurnContext
{
    public Pokemon Pokemon { get; }
    public Pokemon? Opponent { get; }
    public Random Random { get; }
    public Func<BattleEvent, Task> Emit { get; }

    public BattleEndOfTurnContext(
        Pokemon pokemon,
        Func<BattleEvent, Task> emit,
        Pokemon? opponent = null,
        Random? random = null)
    {
        Pokemon = pokemon;
        Opponent = opponent;
        Random = random ?? System.Random.Shared;
        Emit = emit;
    }

    public Task ShowMessage(string message, int baseDelayMs = 1400) =>
        Emit(BattleEvent.MessageLine(message, baseDelayMs));
}

public sealed class BattleEndOfBattleContext
{
    public Pokemon Pokemon { get; }
    public Random Random { get; }
    public Func<BattleEvent, Task> Emit { get; }

    public BattleEndOfBattleContext(
        Pokemon pokemon,
        Random random,
        Func<BattleEvent, Task> emit)
    {
        Pokemon = pokemon;
        Random = random;
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
    Task AfterBattleAsync(BattleEndOfBattleContext context) => Task.CompletedTask;
}