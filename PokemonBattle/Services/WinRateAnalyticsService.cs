using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public sealed class WinRateAnalyticsService
{
    private readonly DatabaseContextExecutor _database;
    private readonly CurrentUserService _currentUser;

    // 관리자 승률 집계 서비스를 구성한다.
    [ActivatorUtilitiesConstructor]
    public WinRateAnalyticsService(
        DatabaseContextExecutor database,
        CurrentUserService currentUser)
    {
        _database = database;
        _currentUser = currentUser;
    }

    // 테스트에서 기존 DB 컨텍스트로 서비스를 구성한다.
    public WinRateAnalyticsService(AppDbContext db, CurrentUserService currentUser)
        : this(new DatabaseContextExecutor(db), currentUser)
    {
    }

    // 관리자용 전투 승률 집계를 DB 그룹 결과로 불러온다.
    public async Task<WinRateAnalyticsSnapshot?> LoadAsync() =>
        await _database.ExecuteAsync("admin-win-rate.load", async db =>
        {
            if (!_currentUser.IsLoggedIn
                || !await db.Users.AnyAsync(user =>
                    user.Username == _currentUser.Username && user.IsAdmin))
            {
                return null;
            }

            var results = db.BattleResults.AsNoTracking();
            var overall = await SummarizeAsync(results);
            var rival = await SummarizeAsync(results.Where(result => result.IsRivalBattle));
            var normal = await SummarizeAsync(results.Where(result => !result.IsRivalBattle));
            var legendary = await SummarizeAsync(
                results.Where(result => result.IsLegendaryBattle));

            var roundGroups = await results
                .GroupBy(result => result.Round >= 20 ? 20 : result.Round)
                .Select(group => new
                {
                    Key = group.Key,
                    BattleCount = group.Count(),
                    WinCount = group.Count(result => result.Won)
                })
                .OrderBy(group => group.Key)
                .ToListAsync();

            var rivalGroups = await results
                .Where(result => result.IsRivalBattle)
                .GroupBy(result => result.RivalNumber)
                .Select(group => new
                {
                    Key = group.Key,
                    BattleCount = group.Count(),
                    WinCount = group.Count(result => result.Won)
                })
                .OrderBy(group => group.Key)
                .ToListAsync();

            return new WinRateAnalyticsSnapshot(
                overall,
                rival,
                normal,
                legendary,
                roundGroups.Select(group => ToGroup(
                    group.Key >= 20 ? "20+" : group.Key.ToString(),
                    group.BattleCount,
                    group.WinCount)).ToList(),
                rivalGroups.Select(group => ToGroup(
                    $"{group.Key}회차",
                    group.BattleCount,
                    group.WinCount)).ToList());
        });

    // 한 조건의 전투 수와 승리 수를 DB에서 한 행으로 집계한다.
    private static async Task<WinRateSummary> SummarizeAsync(
        IQueryable<BattleResult> results)
    {
        var aggregate = await results
            .GroupBy(_ => 1)
            .Select(group => new
            {
                BattleCount = group.Count(),
                WinCount = group.Count(result => result.Won)
            })
            .SingleOrDefaultAsync();

        return aggregate == null
            ? new WinRateSummary(0, 0, 0)
            : new WinRateSummary(
                aggregate.BattleCount,
                aggregate.WinCount,
                CalculateRate(aggregate.BattleCount, aggregate.WinCount));
    }

    // 그룹 집계 결과를 화면용 승률 행으로 변환한다.
    private static WinRateGroup ToGroup(
        string label,
        int battleCount,
        int winCount) =>
        new(
            label,
            battleCount,
            winCount,
            CalculateRate(battleCount, winCount));

    // 전투 수를 기준으로 승률을 계산한다.
    private static double CalculateRate(int battleCount, int winCount) =>
        battleCount == 0 ? 0 : winCount * 100d / battleCount;
}

public sealed record WinRateAnalyticsSnapshot(
    WinRateSummary Overall,
    WinRateSummary Rival,
    WinRateSummary Normal,
    WinRateSummary Legendary,
    IReadOnlyList<WinRateGroup> ByRound,
    IReadOnlyList<WinRateGroup> ByRivalNumber)
{
    public bool HasBattleResults => Overall.BattleCount > 0;
}

public sealed record WinRateSummary(
    int BattleCount,
    int WinCount,
    double WinRatePercent);

public sealed record WinRateGroup(
    string Label,
    int BattleCount,
    int WinCount,
    double WinRatePercent);