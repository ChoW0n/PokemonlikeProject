using PokemonBattle.Models;
using PokemonBattle.Services;
using Xunit;
using Xunit.Abstractions;

namespace PokemonBattle.Tests;

public sealed class SleepPowderRegressionTests
{
    private readonly ITestOutputHelper output;

    public SleepPowderRegressionTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task Sleep_powder_matrix_has_no_silent_failures()
    {
        var rows = new List<SleepPowderResult>();
        var ruleMismatches = new List<SleepPowderResult>();
        try
        {
            foreach (string field in new[]
            {
                BattleField.None,
                BattleField.Electric,
                BattleField.Misty,
                BattleField.Grassy,
                BattleField.Psychic,
                BattleField.Calm
            })
            {
                foreach (var targetProfile in TargetProfiles())
                foreach (bool hasStatus in new[] { false, true })
                foreach (bool hasSubstitute in new[] { false, true })
                foreach (bool sheerForce in new[] { false, true })
                {
                    BattleWeather.Reset();
                    BattleField.Set(field);
                    var attacker = CreateAttacker(sheerForce);
                    var target = CreateTarget(targetProfile);
                    if (hasStatus) target.ApplyAilment("burn", new AlwaysHitRandom());
                    if (hasSubstitute) Assert.True(target.TryCreateSubstitute());

                    var events = new List<BattleEvent>();
                    await CreateEngine().TakeTurnAsync(
                        attacker,
                        target,
                        "sleep-powder",
                        attackerIsHero: true,
                        Capture(events));

                    string[] messages = events
                        .Where(battleEvent => !string.IsNullOrWhiteSpace(battleEvent.Message))
                        .Select(battleEvent => battleEvent.Message!)
                        .ToArray();
                    bool slept = target.Status == StatusCondition.Sleep;
                    bool expectedSleep = !hasStatus
                        && !hasSubstitute
                        && targetProfile.CanBeSlept
                        && (!targetProfile.IsGrounded
                            || field is not BattleField.Electric and not BattleField.Misty);
                    var row = new SleepPowderResult(
                        field,
                        targetProfile.Label,
                        hasStatus,
                        hasSubstitute,
                        sheerForce,
                        slept,
                        expectedSleep,
                        messages);
                    rows.Add(row);
                    if (expectedSleep != slept) ruleMismatches.Add(row);
                }
            }
        }
        finally
        {
            BattleWeather.Reset();
            BattleField.Reset();
        }

        Assert.Equal(432, rows.Count);
        output.WriteLine(
            "| 필드 | 대상 조건 | 기존 상태 | 대타출동 | 시전자 우격다짐 | 잠듦 | 예상 | 남은 메시지 |");
        output.WriteLine(
            "|---|---|---:|---:|---:|---:|---:|---|");
        foreach (var row in rows)
        {
            output.WriteLine(
                $"| {row.Field} | {row.TargetLabel} | {YesNo(row.HasStatus)} | "
                + $"{YesNo(row.HasSubstitute)} | {YesNo(row.SheerForce)} | "
                + $"{YesNo(row.Slept)} | {YesNo(row.ExpectedSleep)} | "
                + $"{(row.Messages.Length == 0 ? "없음" : string.Join(" / ", row.Messages))} |");
        }

        var silentFailures = rows
            .Where(row => !row.Slept && row.Messages.Length == 0)
            .ToArray();
        Assert.True(
            silentFailures.Length == 0,
            "잠듦에 실패했지만 로그가 비어 있는 조합:\n"
            + string.Join(
                "\n",
                silentFailures.Select(row =>
                    $"{row.Field}, {row.TargetLabel}, 기존 상태={row.HasStatus}, "
                    + $"대타출동={row.HasSubstitute}, 우격다짐={row.SheerForce}")));
        Assert.True(
            ruleMismatches.Count == 0,
            "예상 규칙과 실제 결과가 다른 조합:\n"
            + string.Join(
                "\n",
                ruleMismatches.Select(row =>
                    $"{row.Field}, {row.TargetLabel}, 기존 상태={row.HasStatus}, "
                    + $"대타출동={row.HasSubstitute}, 우격다짐={row.SheerForce}, "
                    + $"예상={row.ExpectedSleep}, 실제={row.Slept}, "
                    + $"메시지={string.Join(" / ", row.Messages)}")));
    }

    private static IEnumerable<TargetProfile> TargetProfiles()
    {
        yield return new("접지", 25, "", "없음", true, true);
        yield return new("비행 타입", 6, "", "없음", false, true);
        yield return new("부유", 437, "부유", "없음", false, true);
        yield return new("불면", 25, "불면", "없음", true, false);
        yield return new("의기양양", 25, "의기양양", "없음", true, false);
        yield return new("스위트베일", 25, "스위트베일", "없음", true, false);
        yield return new("인분", 25, "인분", "없음", true, false);
        yield return new("방진", 25, "방진", "없음", true, false);
        yield return new("방진고글", 25, "", "방진고글", true, false);
    }

    private static Pokemon CreateAttacker(bool sheerForce) =>
        new(
            PokemonDatabase.All[1],
            new List<string> { "sleep-powder" },
            sheerForce ? "우격다짐" : "",
            "없음",
            level: 50);

    private static Pokemon CreateTarget(TargetProfile profile) =>
        new(
            PokemonDatabase.All[profile.PokemonId],
            new List<string> { "tackle" },
            profile.Ability,
            profile.HeldItem,
            level: 50);

    private static BattleEngine CreateEngine() => new(
        new AlwaysHitRandom(),
        new IBattleEffectHandler[] { new MoveEffectHandler() });

    private static Func<BattleEvent, Task> Capture(List<BattleEvent> events) =>
        battleEvent =>
        {
            events.Add(battleEvent);
            return Task.CompletedTask;
        };

    private static string YesNo(bool value) => value ? "예" : "아니오";

    private sealed record TargetProfile(
        string Label,
        int PokemonId,
        string Ability,
        string HeldItem,
        bool IsGrounded,
        bool CanBeSlept);

    private sealed record SleepPowderResult(
        string Field,
        string TargetLabel,
        bool HasStatus,
        bool HasSubstitute,
        bool SheerForce,
        bool Slept,
        bool ExpectedSleep,
        string[] Messages);

    private sealed class AlwaysHitRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }
}