using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PokemonBattle.Data;

namespace PokemonBattle.Services;

/// <summary>
/// Runs one logical database operation on an isolated context.
/// A new context is created for every execution-strategy attempt so a failed
/// PostgreSQL connection never leaves tracked state in the next attempt.
/// </summary>
public sealed class DatabaseContextExecutor
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<DatabaseContextExecutor> _logger;
    private readonly bool _disposeContexts;
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    [ActivatorUtilitiesConstructor]
    public DatabaseContextExecutor(
        IDbContextFactory<AppDbContext> factory,
        ILogger<DatabaseContextExecutor> logger)
    {
        _factory = factory;
        _logger = logger;
        _disposeContexts = true;
    }

    // Kept for the regression tests and small in-process callers that provide
    // an already-created context. Production DI always uses the factory ctor.
    public DatabaseContextExecutor(AppDbContext context)
    {
        _factory = new ExistingContextFactory(context);
        _logger = NullLogger<DatabaseContextExecutor>.Instance;
        _disposeContexts = false;
    }

    public async Task ExecuteAsync(
        string operation,
        Func<AppDbContext, Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(
            operation,
            async db =>
            {
                await action(db);
                return null;
            },
            cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<AppDbContext, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var strategyContext = await CreateContextAsync(cancellationToken);
            var strategy = strategyContext.Database.CreateExecutionStrategy();

            try
            {
                var result = await strategy.ExecuteAsync(async () =>
                {
                    var db = await CreateContextAsync(cancellationToken);
                    try
                    {
                        return await action(db);
                    }
                    finally
                    {
                        await DisposeContextAsync(db);
                    }
                });

                _logger.LogDebug("Database operation {Operation} completed.", operation);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Database operation {Operation} was cancelled.", operation);
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Database operation {Operation} failed after transient retries.",
                    operation);
                throw;
            }
            finally
            {
                await DisposeContextAsync(strategyContext);
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task<AppDbContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        var context = await _factory.CreateDbContextAsync(cancellationToken);
        return context;
    }

    private async ValueTask DisposeContextAsync(AppDbContext context)
    {
        if (_disposeContexts)
        {
            await context.DisposeAsync();
        }
    }

    private sealed class ExistingContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly AppDbContext _context;

        public ExistingContextFactory(AppDbContext context) => _context = context;

        public AppDbContext CreateDbContext() => _context;

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_context);
    }
}