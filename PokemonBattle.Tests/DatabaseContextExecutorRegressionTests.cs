using Microsoft.EntityFrameworkCore;
using PokemonBattle.Data;
using PokemonBattle.Services;
using Xunit;

namespace PokemonBattle.Tests;

public sealed class DatabaseContextExecutorRegressionTests
{
    [Fact]
    public async Task Concurrent_operations_receive_independent_contexts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        var factory = new CountingContextFactory(options);
        var executor = new DatabaseContextExecutor(
            factory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseContextExecutor>.Instance);
        var contextIds = new System.Collections.Concurrent.ConcurrentBag<int>();
        int activeOperations = 0;
        int maximumConcurrentOperations = 0;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            executor.ExecuteAsync("regression.concurrent-context", async db =>
            {
                contextIds.Add(db.GetHashCode());
                int active = Interlocked.Increment(ref activeOperations);
                InterlockedMax(ref maximumConcurrentOperations, active);
                await Task.Delay(5);
                Interlocked.Decrement(ref activeOperations);
            })));

        // One context is used to build the execution strategy and one is used
        // for the operation attempt. Both must be independent per call.
        Assert.Equal(16, factory.CreatedCount);
        Assert.Equal(8, contextIds.Distinct().Count());
        Assert.Equal(1, maximumConcurrentOperations);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref location);
            if (current >= value) return;
        }
        while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    private sealed class CountingContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private int _createdCount;

        public CountingContextFactory(DbContextOptions<AppDbContext> options) => _options = options;

        public int CreatedCount => _createdCount;

        public AppDbContext CreateDbContext()
        {
            Interlocked.Increment(ref _createdCount);
            return new AppDbContext(_options);
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}