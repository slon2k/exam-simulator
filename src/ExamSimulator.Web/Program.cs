using ExamSimulator.Web.Components;
using ExamSimulator.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ExamSimulatorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (builder.Environment.IsDevelopment())
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});

var app = builder.Build();

// Startup diagnostics — log key env vars to help debug config issues
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ASPNETCORE_ENVIRONMENT: {Env}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(not set)");
logger.LogInformation("SQLAZURECONNSTR_DefaultConnection present: {Present}",
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection")));
var resolvedConnStr = app.Configuration.GetConnectionString("DefaultConnection");
logger.LogInformation("ConnectionStrings:DefaultConnection present: {Present}, starts with: {Prefix}",
    !string.IsNullOrEmpty(resolvedConnStr),
    resolvedConnStr?.Length >= 20 ? resolvedConnStr[..20] : resolvedConnStr);

if (!app.Configuration.GetValue<bool>("SkipMigrations"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();
        if (db.Database.IsRelational())
        {
            if (app.Environment.IsDevelopment())
                db.Database.EnsureDeleted();

            db.Database.Migrate();

            if (app.Environment.IsDevelopment())
                DbSeeder.Seed(db);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed — app will start without applying migrations");
        // Write to persistent storage so the error survives the container restart
        var logDir = "/home/LogFiles/Application";
        try
        {
            Directory.CreateDirectory(logDir);
            File.WriteAllText(Path.Combine(logDir, "migration-error.txt"),
                $"[{DateTime.UtcNow:O}] Migration failed:\n{ex}");
        }
        catch { /* ignore write failures */ }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
