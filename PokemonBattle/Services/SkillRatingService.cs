using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class SkillRatingService
{
    private readonly DatabaseContextExecutor _database;

    [ActivatorUtilitiesConstructor]
    public SkillRatingService(DatabaseContextExecutor database)
    {
        _database = database;
    }

    public SkillRatingService(AppDbContext db) : this(new DatabaseContextExecutor(db))
    {
    }

    public async Task<PlayerSkillRating> GetOrCreateAsync(string username)
    {
        return await _database.ExecuteAsync("skill-rating.load", async db =>
        {
            var rating = await db.PlayerSkillRatings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Username == username);
            if (rating == null)
            {
                rating = new PlayerSkillRating
                {
                    Username = username,
                    Rating = SkillRatingCalculator.DefaultRating,
                    CompletedRuns = 0,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };
                db.PlayerSkillRatings.Add(rating);
                try
                {
                    await db.SaveChangesAsync();
                    return rating;
                }
                catch (DbUpdateException exception) when (
                    exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    db.Entry(rating).State = EntityState.Detached;
                    return await db.PlayerSkillRatings.AsNoTracking()
                        .SingleAsync(item => item.Username == username);
                }
            }

            rating.Rating = Math.Clamp(rating.Rating, 400, 2000);
            return rating;
        });
    }

    public async Task<double> UpdateForRunAsync(
        string username,
        RunPerformanceSummary summary)
    {
        return await _database.ExecuteAsync("skill-rating.update", async db =>
        {
            var rating = await db.PlayerSkillRatings
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(item => item.Username == username);
            if (rating == null)
            {
                rating = new PlayerSkillRating
                {
                    Username = username,
                    Rating = SkillRatingCalculator.DefaultRating,
                    CompletedRuns = 0
                };
                db.PlayerSkillRatings.Add(rating);
            }

            rating.Rating = SkillRatingCalculator.UpdateRating(
                Math.Clamp(rating.Rating, 400, 2000),
                summary);
            rating.CompletedRuns++;
            rating.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return rating.Rating;
        });
    }
}