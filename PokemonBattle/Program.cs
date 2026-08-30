using PokemonBattle.Components;
using PokemonBattle.Data;
using PokemonBattle.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string connectionString = BuildConnectionString();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UnlockService>();
builder.Services.AddScoped<RunStore>();
builder.Services.AddScoped<BattleEngine>();
foreach (var handlerType in typeof(BattleEngine).Assembly.GetTypes()
    .Where(type => type is { IsClass: true, IsAbstract: false }
        && typeof(IBattleEffectHandler).IsAssignableFrom(type)))
{
    builder.Services.AddScoped(typeof(IBattleEffectHandler), handlerType);
}

builder.Services.AddSingleton<IScoreStore, InMemoryScoreStore>();
builder.Services.AddSingleton<IPresetStore, InMemoryPresetStore>();
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
            ""LoadoutsJson"" TEXT NOT NULL,
            ""LegendaryProgressPercent"" INTEGER NOT NULL DEFAULT 0
        );
        ALTER TABLE ""PlayerRuns""
            ADD COLUMN IF NOT EXISTS ""LegendaryProgressPercent"" INTEGER NOT NULL DEFAULT 0;
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
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
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