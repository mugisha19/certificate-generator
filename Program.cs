using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using CertificateApp.Data;
using CertificateApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database provider switch ────────────────────────────────────────
//   "DatabaseProvider" config key → "Postgres" (default) or "SqlServer".
//
//   Connection-string lookup order:
//     1. DB_HOST + DB_PORT/USER/PASSWORD/NAME env vars (Render-style)
//     2. ConnectionStrings:<provider> in appsettings.json / user secrets
//     3. ConnectionStrings:DefaultConnection (legacy)
var provider = (builder.Configuration["DatabaseProvider"] ?? "Postgres").Trim();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var cs = builder.Configuration.GetConnectionString("SqlServer")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:SqlServer.");
        options.UseSqlServer(cs);
    }
    else
    {
        var cs = BuildPostgresConnectionStringFromEnv(builder.Configuration)
                 ?? builder.Configuration.GetConnectionString("Postgres")
                 ?? builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing Postgres connection string.");
        options.UseNpgsql(cs);
    }
});

// MVC with Razor views
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Certificate PDF service
builder.Services.AddSingleton<CertificatePdfService>();

// Trust X-Forwarded-* headers when running behind a TLS-terminating proxy (Render, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opts.KnownNetworks.Clear();
    opts.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS redirection only in dev — production sits behind a proxy that already terminates TLS.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ─── Bring schema up to date ─────────────────────────────────────────
//   EnsureCreated() materialises the schema directly from the current model
//   for both providers, which works for fresh deploys (Render Postgres) and
//   leaves an existing schema untouched.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ─────────────────────────────────────────────────────────────────────
static string? BuildPostgresConnectionStringFromEnv(IConfiguration cfg)
{
    // 1. DATABASE_URL — Render / Heroku style postgres:// URL.
    var url = cfg["DATABASE_URL"];
    if (!string.IsNullOrWhiteSpace(url) &&
        url.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
    {
        return ConvertPostgresUrlToNpgsql(url);
    }

    // 2. Individual DB_* env vars.
    var host = cfg["DB_HOST"];
    if (!string.IsNullOrWhiteSpace(host))
    {
        var port = cfg["DB_PORT"] ?? "5432";
        var db   = cfg["DB_NAME"];
        var user = cfg["DB_USER"];
        var pwd  = cfg["DB_PASSWORD"];

        return $"Host={host};Port={port};Database={db};Username={user};Password={pwd};" +
               "SSL Mode=Require;Trust Server Certificate=true";
    }

    return null;
}

static string ConvertPostgresUrlToNpgsql(string url)
{
    // postgres://user:pass@host:port/dbname?sslmode=require → Npgsql key/value form.
    var u = new Uri(url);
    var creds = u.UserInfo.Split(':', 2);
    var user  = Uri.UnescapeDataString(creds[0]);
    var pwd   = creds.Length > 1 ? Uri.UnescapeDataString(creds[1]) : "";
    var db    = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/'));
    var port  = u.Port > 0 ? u.Port : 5432;

    return $"Host={u.Host};Port={port};Database={db};Username={user};Password={pwd};" +
           "SSL Mode=Require;Trust Server Certificate=true";
}
