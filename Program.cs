using Microsoft.EntityFrameworkCore;
using CertificateApp.Data;
using CertificateApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database provider switch ────────────────────────────────────────
//   appsettings.json → "DatabaseProvider": "Postgres" | "SqlServer"
//   Two connection strings live under "ConnectionStrings": "Postgres" and "SqlServer".
//   Override at runtime with --DatabaseProvider=SqlServer or env DatabaseProvider=SqlServer.
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
        var cs = builder.Configuration.GetConnectionString("Postgres")
                 ?? builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");
        options.UseNpgsql(cs);
    }
});

// MVC with Razor views
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Certificate PDF service
builder.Services.AddSingleton<CertificatePdfService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ─── Bring schema up to date ─────────────────────────────────────────
//   Postgres has hand-written migrations (with Postgres-specific SQL).
//   SQL Server has none — let EF materialize the schema from the model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
