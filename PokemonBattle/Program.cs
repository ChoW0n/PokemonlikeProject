using PokemonBattle.Components;
using PokemonBattle.Data;
using PokemonBattle.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

string connectionString = BuildConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UnlockService>();
builder.Services.AddScoped<RunStore>();
builder.Services.AddScoped<SkillRatingService>();
builder.Services.AddScoped<PlayerProgressionStore>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminOperationsService>();
builder.Services.AddScoped<BattleEngine>();
foreach (var handlerType in typeof(BattleEngine).Assembly.GetTypes()
    .Where(type => type is { IsClass: true, IsAbstract: false }
        && typeof(IBattleEffectHandler).IsAssignableFrom(type)))
{
    builder.Services.AddScoped(typeof(IBattleEffectHandler), handlerType);
}

builder.Services.AddSingleton<IScoreStore, InMemoryScoreStore>();
builder.Services.AddScoped<IPresetStore, PostgresPresetStore>();
builder.Services.AddScoped<GameState>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    ");

    string? bootstrapAdminPassword =
        Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD");
    string bootstrapAdminUsername =
        Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_USERNAME") ?? "admin";

    if (!string.IsNullOrWhiteSpace(bootstrapAdminPassword)
        && !db.Users.Any(u => u.Username == bootstrapAdminUsername))
    {
        db.Users.Add(new UserAccount
        {
            Username = bootstrapAdminUsername,
            PasswordHash = PasswordHasher.Hash(bootstrapAdminPassword),
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
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

string BuildConnectionString(IConfiguration configuration)
{
    string? raw = new[]
    {
        Environment.GetEnvironmentVariable("DATABASE_URL"),
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
        configuration.GetConnectionString("DefaultConnection")
    }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    if (string.IsNullOrEmpty(raw))
    {
        throw new InvalidOperationException(
            "데이터베이스 연결 설정이 없습니다. DATABASE_URL 또는 ConnectionStrings__DefaultConnection 환경변수를 설정하세요.");
    }

    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return raw;
    }

    var uri = new Uri(raw);
    int userInfoSeparator = uri.UserInfo.IndexOf(':');
    string user = Uri.UnescapeDataString(
        userInfoSeparator >= 0 ? uri.UserInfo[..userInfoSeparator] : uri.UserInfo);
    string password = userInfoSeparator >= 0
        ? Uri.UnescapeDataString(uri.UserInfo[(userInfoSeparator + 1)..])
        : "";
    string host = uri.Host;
    int port = uri.Port > 0 ? uri.Port : 5432;
    string database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

    string sslMode = "Prefer";
    if (!string.IsNullOrEmpty(uri.Query))
    {
        var queryParams = uri.Query.TrimStart('?').Split('&');
        foreach (var p in queryParams)
        {
            var kv = p.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                sslMode = Uri.UnescapeDataString(kv[1]);
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