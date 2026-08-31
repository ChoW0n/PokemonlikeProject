using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Models;

namespace PokemonBattle.Services;

public class SkillRatingService
{
    private readonly AppDbContext _db;

    public SkillRatingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerSkillRating> GetOrCreateAsync(string username)
    {
        var rating = await _db.PlayerSkillRatings
            .FirstOrDefaultAsync(item => item.Username == username);
        if (rating != null)
        {
            rating.Rating = Math.Clamp(rating.Rating, 400, 2000);
            return rating;
        }

        rating = new PlayerSkillRating
        {
            Username = username,
            Rating = SkillRatingCalculator.DefaultRating,
            CompletedRuns = 0,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        _db.PlayerSkillRatings.Add(rating);
        await _db.SaveChangesAsync();
        return rating;
    }

    public async Task<double> UpdateForRunAsync(
        string username,
        RunPerformanceSummary summary)
    {
        var rating = await GetOrCreateAsync(username);
        rating.Rating = SkillRatingCalculator.UpdateRating(rating.Rating, summary);
        rating.CompletedRuns++;
        rating.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return rating.Rating;
    }
}