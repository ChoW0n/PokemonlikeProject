using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

public static class PlayerRunItemCleanup
{
    public const string MarkerKey = "player-runs-clear-equipped-items";

    public static async Task<CleanupResult> ApplyOnceAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            int acquired = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AppMaintenanceMarkers" ("Key", "AppliedAtUtc", "Details")
                VALUES ({MarkerKey}, CURRENT_TIMESTAMP, 'running')
                ON CONFLICT ("Key") DO NOTHING;
                """, cancellationToken);

            if (acquired == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return new CleanupResult(false, 0, 0);
            }

            var runs = await db.PlayerRuns.ToListAsync(cancellationToken);
            int changedRuns = 0;
            int clearedLoadouts = 0;

            foreach (var run in runs)
            {
                // Deserialize deliberately propagates JsonException: malformed data must
                // roll back both the marker and every earlier row update.
                var loadouts = LoadoutJson.Deserialize(run.LoadoutsJson);
                var cleared = LoadoutJson.ClearChosenItems(loadouts);
                string normalizedJson = LoadoutJson.Serialize(cleared);
                if (string.Equals(run.LoadoutsJson, normalizedJson, StringComparison.Ordinal))
                {
                    continue;
                }

                run.LoadoutsJson = normalizedJson;
                changedRuns++;
                clearedLoadouts += loadouts.Count(loadout =>
                    TeamLoadoutRules.NormalizeItemName(loadout.ChosenItem) != TeamLoadoutRules.NoItem);
            }

            string details = $"changed-runs={changedRuns}; cleared-loadouts={clearedLoadouts}";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE "AppMaintenanceMarkers"
                SET "Details" = {details}
                WHERE "Key" = {MarkerKey};
                """, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "One-time PlayerRuns item cleanup completed: {Details}",
                details);
            return new CleanupResult(true, changedRuns, clearedLoadouts);
        });
    }

    public sealed record CleanupResult(
        bool Applied,
        int ChangedRuns,
        int ClearedLoadouts);
}