using PokemonBattle.Components;
using PokemonBattle.Data;
using PokemonBattle.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

string connectionString = BuildConnectionString();
builder.Services.AddDataProtection()
    .SetApplicationName("PokemonBattle");
builder.Services.Configure<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>(
    options => options.XmlRepository = new PostgresXmlRepository(connectionString));

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(
    connectionString,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null)));

builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes;
});

builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<DatabaseContextExecutor>(serviceProvider =>
    new DatabaseContextExecutor(
        serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>(),
        serviceProvider.GetRequiredService<ILogger<DatabaseContextExecutor>>()));
builder.Services.AddScoped<AuthService>(serviceProvider =>
    new AuthService(serviceProvider.GetRequiredService<DatabaseContextExecutor>()));
builder.Services.AddScoped<UnlockService>(serviceProvider =>
    new UnlockService(
        serviceProvider.GetRequiredService<DatabaseContextExecutor>(),
        serviceProvider.GetRequiredService<CurrentUserService>()));
builder.Services.AddScoped<RunStore>(serviceProvider =>
    new RunStore(serviceProvider.GetRequiredService<DatabaseContextExecutor>()));
builder.Services.AddScoped<SkillRatingService>(serviceProvider =>
    new SkillRatingService(serviceProvider.GetRequiredService<DatabaseContextExecutor>()));
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<PlayerProgressionStore>(serviceProvider =>
    new PlayerProgressionStore(serviceProvider.GetRequiredService<DatabaseContextExecutor>()));
builder.Services.AddScoped<PokemonMasteryStore>(serviceProvider =>
    new PokemonMasteryStore(serviceProvider.GetRequiredService<DatabaseContextExecutor>()));
builder.Services.AddScoped<AdminDashboardService>(serviceProvider =>
    new AdminDashboardService(
        serviceProvider.GetRequiredService<DatabaseContextExecutor>(),
        serviceProvider.GetRequiredService<CurrentUserService>()));
builder.Services.AddScoped<AdminOperationsService>(serviceProvider =>
    new AdminOperationsService(
        serviceProvider.GetRequiredService<DatabaseContextExecutor>(),
        serviceProvider.GetRequiredService<CurrentUserService>()));
builder.Services.AddScoped<BattleEngine>();
foreach (var handlerType in typeof(BattleEngine).Assembly.GetTypes()
    .Where(type => type is { IsClass: true, IsAbstract: false }
        && typeof(IBattleEffectHandler).IsAssignableFrom(type)))
{
    builder.Services.AddScoped(typeof(IBattleEffectHandler), handlerType);
}

builder.Services.AddSingleton<IScoreStore, InMemoryScoreStore>();
builder.Services.AddScoped<IPresetStore>(serviceProvider =>
    new PostgresPresetStore(
        serviceProvider.GetRequiredService<DatabaseContextExecutor>(),
        serviceProvider.GetRequiredService<CurrentUserService>()));
builder.Services.AddScoped<GameState>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
         // 끊긴 회로 보존량을 제한해 메모리 사용을 줄인다.
         options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
         options.DisconnectedCircuitMaxRetained = 150;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
        options.MaxBufferedUnacknowledgedRenderBatches = 20;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Users"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""PasswordHash"" TEXT NOT NULL,
            ""IsAdmin"" BOOLEAN NOT NULL
        );
        CREATE TABLE IF NOT EXISTS ""UnlockedPokemons"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""PokemonId"" INTEGER NOT NULL
        );
        CREATE TABLE IF NOT EXISTS ""PokemonMasteries"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""PokemonId"" INTEGER NOT NULL,
            ""VictoryContributions"" INTEGER NOT NULL DEFAULT 0
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PokemonMasteries_Username_PokemonId""
            ON ""PokemonMasteries"" (""Username"", ""PokemonId"");
        CREATE TABLE IF NOT EXISTS ""PlayerRuns"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""CurrentScore"" INTEGER NOT NULL,
            ""HighScore"" INTEGER NOT NULL DEFAULT 0,
            ""LoadoutsJson"" TEXT NOT NULL,
            ""LegendaryProgressPercent"" INTEGER NOT NULL DEFAULT 0,
            ""LegendaryEncounterHistoryJson"" TEXT NOT NULL DEFAULT '[]',
            ""DifficultyAdjustment"" INTEGER NOT NULL DEFAULT 0,
            ""RoundPerformancesJson"" TEXT NOT NULL DEFAULT '[]',
            ""RunMetaStateJson"" TEXT NOT NULL DEFAULT '{{}}'
        );
        CREATE TABLE IF NOT EXISTS ""PlayerSkillRatings"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""Rating"" DOUBLE PRECISION NOT NULL DEFAULT 1000,
            ""CompletedRuns"" INTEGER NOT NULL DEFAULT 0,
            ""UpdatedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS ""UserPresets"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""LoadoutsJson"" TEXT NOT NULL,
            ""UpdatedAt"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserPresets_Username_Name""
            ON ""UserPresets"" (""Username"", ""Name"");
        CREATE TABLE IF NOT EXISTS ""AdminAuditLogs"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""AdminUsername"" TEXT NOT NULL,
            ""Action"" TEXT NOT NULL,
            ""TargetUsername"" TEXT NOT NULL,
            ""Details"" TEXT NOT NULL DEFAULT '',
            ""CreatedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""HighScore"" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""LegendaryProgressPercent"" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""LegendaryEncounterHistoryJson"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""DifficultyAdjustment"" INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""RoundPerformancesJson"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""RunMetaStateJson"" TEXT NOT NULL DEFAULT '{{}}';
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PlayerSkillRatings_Username""
            ON ""PlayerSkillRatings"" (""Username"");
        CREATE TABLE IF NOT EXISTS ""PlayerProgressions"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""CompletedBattles"" INTEGER NOT NULL DEFAULT 0,
            ""RivalPending"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""RivalNumber"" INTEGER NOT NULL DEFAULT 0,
            ""LatestLoadoutsJson"" TEXT NOT NULL DEFAULT '[]',
            ""MovePreferencesJson"" TEXT NOT NULL DEFAULT '{{}}',
            ""UpdatedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PlayerProgressions_Username""
            ON ""PlayerProgressions"" (""Username"");
        CREATE TABLE IF NOT EXISTS ""MailboxMessages"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""DeduplicationKey"" TEXT NOT NULL,
            ""Title"" TEXT NOT NULL,
            ""Body"" TEXT NOT NULL,
            ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""CreatedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MailboxMessages_Username_DeduplicationKey""
            ON ""MailboxMessages"" (""Username"", ""DeduplicationKey"");
        CREATE TABLE IF NOT EXISTS ""TechnicalMachines"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""MoveKey"" TEXT NOT NULL,
            ""Quantity"" INTEGER NOT NULL DEFAULT 0
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_TechnicalMachines_Username_MoveKey""
            ON ""TechnicalMachines"" (""Username"", ""MoveKey"");
        CREATE TABLE IF NOT EXISTS ""BattleResults"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""CreatedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""IsRivalBattle"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""RivalNumber"" INTEGER NOT NULL DEFAULT 0,
            ""Won"" BOOLEAN NOT NULL,
            ""Round"" INTEGER NOT NULL DEFAULT 1,
            ""Turns"" INTEGER NOT NULL DEFAULT 0,
            ""PlayerHpRatio"" DOUBLE PRECISION NOT NULL DEFAULT 0,
            ""DifficultyAdjustment"" INTEGER NOT NULL DEFAULT 0,
            ""SkillRating"" DOUBLE PRECISION NOT NULL DEFAULT 1000
        );
        CREATE INDEX IF NOT EXISTS ""IX_BattleResults_Username_CreatedAtUtc""
            ON ""BattleResults"" (""Username"", ""CreatedAtUtc"" DESC);
        CREATE TABLE IF NOT EXISTS ""AppMaintenanceMarkers"" (
            ""Key"" TEXT PRIMARY KEY,
            ""AppliedAtUtc"" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""Details"" TEXT NOT NULL DEFAULT ''
        );
    ");

    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new UserAccount
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("admin"),
            IsAdmin = true
        });
        db.SaveChanges();
    }

    ConsolidateAdminAccounts(db);
    db.Database.ExecuteSqlRaw("""
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username"
            ON "Users" ("Username");
        """);

    db.Database.ExecuteSqlRaw("""
        INSERT INTO "PlayerSkillRatings" ("Username")
        SELECT "Username" FROM "Users"
        ON CONFLICT ("Username") DO NOTHING;
        """);

    await PlayerRunItemCleanup.ApplyOnceAsync(
        db,
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("PlayerRunItemCleanup"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // 정적 파일은 캐시해 매 요청의 전송량을 줄인다.
        context.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

string BuildConnectionString()
{
    string? raw = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(raw))
    {
        throw new Exception("DATABASE_URL 환경변수가 없습니다. Replit에서 Database 도구를 먼저 활성화해주세요.");
    }

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':');
    string user = userInfo[0];
    string password = userInfo.Length > 1 ? userInfo[1] : "";
    string host = uri.Host;
    int port = uri.Port > 0 ? uri.Port : 5432;
    string database = uri.AbsolutePath.TrimStart('/');

    string sslMode = "Disable";
    if (!string.IsNullOrEmpty(uri.Query))
    {
        var queryParams = uri.Query.TrimStart('?').Split('&');
        foreach (var p in queryParams)
        {
            var kv = p.Split('=');
            if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                sslMode = kv[1];
            }
        }
    }

    return $"Host={host};Port={port};Username={user};Password={password};Database={database};SSL Mode={sslMode};Trust Server Certificate=true";
}

void ConsolidateAdminAccounts(AppDbContext db)
{
    var adminAccounts = db.Users
        .Where(user => user.Username.ToLower() == "admin")
        .OrderBy(user => user.Id)
        .ToList();

    if (adminAccounts.Count == 0)
    {
        return;
    }

    var keeper = adminAccounts.FirstOrDefault(user => user.Username == "admin") ?? adminAccounts[0];
    keeper.Username = "admin";

    foreach (var duplicate in adminAccounts.Where(user => user.Id != keeper.Id).ToList())
    {
        var duplicateUsername = duplicate.Username;

        var keeperUnlocks = db.UnlockedPokemons
            .Where(unlock => unlock.Username == keeper.Username)
            .Select(unlock => unlock.PokemonId)
            .ToHashSet();
        var duplicateUnlocks = db.UnlockedPokemons
            .Where(unlock => unlock.Username == duplicateUsername)
            .ToList();
        foreach (var unlock in duplicateUnlocks)
        {
            if (keeperUnlocks.Add(unlock.PokemonId))
            {
                unlock.Username = keeper.Username;
            }
            else
            {
                db.UnlockedPokemons.Remove(unlock);
            }
        }

        var keeperPresetNames = db.UserPresets
            .Where(preset => preset.Username == keeper.Username)
            .Select(preset => preset.Name)
            .ToHashSet(StringComparer.Ordinal);
        var duplicatePresets = db.UserPresets
            .Where(preset => preset.Username == duplicateUsername)
            .ToList();
        foreach (var preset in duplicatePresets)
        {
            if (keeperPresetNames.Add(preset.Name))
            {
                preset.Username = keeper.Username;
            }
            else
            {
                db.UserPresets.Remove(preset);
            }
        }

        foreach (var run in db.PlayerRuns.Where(run => run.Username == duplicateUsername).ToList())
        {
            run.Username = keeper.Username;
        }

        var keeperMasteries = db.PokemonMasteries
            .Where(mastery => mastery.Username == keeper.Username)
            .ToDictionary(mastery => mastery.PokemonId);
        var duplicateMasteries = db.PokemonMasteries
            .Where(mastery => mastery.Username == duplicateUsername)
            .ToList();
        foreach (var duplicateMastery in duplicateMasteries)
        {
            if (keeperMasteries.TryGetValue(duplicateMastery.PokemonId, out var keeperMastery))
            {
                keeperMastery.VictoryContributions = Math.Min(
                    int.MaxValue,
                    Math.Max(0, keeperMastery.VictoryContributions)
                    + Math.Max(0, duplicateMastery.VictoryContributions));
                db.PokemonMasteries.Remove(duplicateMastery);
            }
            else
            {
                duplicateMastery.Username = keeper.Username;
                keeperMasteries[duplicateMastery.PokemonId] = duplicateMastery;
            }
        }

        var keeperRating = db.PlayerSkillRatings
            .FirstOrDefault(rating => rating.Username == keeper.Username);
        var duplicateRating = db.PlayerSkillRatings
            .FirstOrDefault(rating => rating.Username == duplicateUsername);
        if (duplicateRating != null)
        {
            if (keeperRating == null)
            {
                duplicateRating.Username = keeper.Username;
            }
            else
            {
                db.PlayerSkillRatings.Remove(duplicateRating);
            }
        }

        db.Users.Remove(duplicate);
    }

    var adminRuns = db.PlayerRuns
        .Where(run => run.Username == keeper.Username)
        .OrderByDescending(run => run.HighScore)
        .ThenByDescending(run => run.CurrentScore)
        .ThenByDescending(run => run.Id)
        .ToList();
    foreach (var duplicateRun in adminRuns.Skip(1))
    {
        db.PlayerRuns.Remove(duplicateRun);
    }

    db.SaveChanges();
}