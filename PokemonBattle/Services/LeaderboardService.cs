using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public sealed class LeaderboardService
{
    private readonly AppDbContext _db;
    private readonly CurrentUserService _currentUser;

    public LeaderboardService(AppDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LeaderboardSnapshot?> LoadAsync()
    {
        if (!_currentUser.IsLoggedIn)
        {
            return null;
        }

        var users = await _db.Users
            .AsNoTracking()
            .Where(user => !user.IsAdmin)
            .Select(user => user.Username)
            .ToListAsync();
        var ratings = await _db.PlayerSkillRatings
            .AsNoTracking()
            .Where(rating => users.Contains(rating.Username))
            .ToListAsync();

        var entries = users
            .Select(username =>
            {
                var rating = ratings.FirstOrDefault(item => item.Username == username);
                return new
                {
                    Username = username,
                    Rating = rating?.Rating ?? SkillRatingCalculator.DefaultRating,
                    CompletedRuns = rating?.CompletedRuns ?? 0
                };
            })
            .OrderByDescending(item => item.Rating)
            .ThenByDescending(item => item.CompletedRuns)
            .ThenBy(item => item.Username, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new LeaderboardEntry(
                index + 1,
                item.Username,
                Math.Clamp(item.Rating, 400, 2000),
                Math.Max(0, item.CompletedRuns),
                string.Equals(item.Username, _currentUser.Username, StringComparison.Ordinal)))
            .ToList();

        return new LeaderboardSnapshot(entries);
    }
}

public sealed record LeaderboardSnapshot(IReadOnlyList<LeaderboardEntry> Entries)
{
    public LeaderboardEntry? CurrentUser =>
        Entries.FirstOrDefault(entry => entry.IsCurrentUser);
}

public sealed record LeaderboardEntry(
    int Rank,
    string Username,
    double Rating,
    int CompletedRuns,
    bool IsCurrentUser);