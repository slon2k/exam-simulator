using ExamSimulator.Web.Components;
using ExamSimulator.Web.Domain.Questions;
using ExamSimulator.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    db.Questions.AddRange(
        new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Easy,
            "Which Azure service provides a fully managed platform for running containerized applications without managing the underlying infrastructure?",
            ["Azure Virtual Machines", "Azure Container Apps", "Azure Kubernetes Service", "Azure App Service"],
            [1], "compute",
            "Azure Container Apps is a fully managed serverless container service built on top of Kubernetes."),

        new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Medium,
            "What is the default time-to-live (TTL) for an Azure Blob Storage access tier transition in a lifecycle management policy?",
            ["1 day", "7 days", "30 days", "No default — must be set explicitly"],
            [3], "storage",
            "Lifecycle management policies require explicitly defining the number of days before transitioning or deleting blobs."),

        new Question(Guid.NewGuid(), "az-204", QuestionType.SingleChoice, Difficulty.Hard,
            "An Azure Function uses a Service Bus trigger. After processing a message the function throws an unhandled exception. What happens to the message by default?",
            ["It is immediately deleted from the queue", "It is moved to the dead-letter queue", "It is abandoned and retried up to the max delivery count, then dead-lettered", "It is re-queued indefinitely"],
            [2], "messaging",
            "When the function fails, the SDK abandons the message. After the max delivery count is reached it moves to the dead-letter queue."),

        new Question(Guid.NewGuid(), "az-204", QuestionType.MultipleChoice, Difficulty.Medium,
            "Which of the following are valid Azure App Service deployment slots? (Select all that apply)",
            ["production", "staging", "testing", "development", "preview"],
            [0, 1, 2, 3, 4], "app-service",
            "App Service supports up to 20 named deployment slots; any name is valid including production, staging, testing, development, and preview."),

        new Question(Guid.NewGuid(), "az-204", QuestionType.MultipleChoice, Difficulty.Hard,
            "Which authentication flows are supported by MSAL for a web API that calls a downstream API on behalf of a signed-in user? (Select all that apply)",
            ["Authorization code flow", "On-Behalf-Of (OBO) flow", "Client credentials flow", "Device code flow"],
            [1, 2], "authentication",
            "A web API uses the On-Behalf-Of flow to obtain tokens for downstream APIs. Client credentials is used for daemon apps, not on behalf of users.")
    );
    db.SaveChanges();
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
