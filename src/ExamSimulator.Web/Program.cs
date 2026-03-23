using ExamSimulator.Web.Components;
using ExamSimulator.Web.Domain.Identity;
using ExamSimulator.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ExamSimulatorDbContext>();

builder.Services.AddAuthorizationBuilder();

builder.Services.AddDbContext<ExamSimulatorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

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
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                await DbSeeder.SeedAsync(db, userManager, roleManager, app.Configuration);
            }
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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();

public partial class Program { }
