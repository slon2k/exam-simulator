using ExamSimulator.Web.Domain.ExamProfiles;
using ExamSimulator.Web.Domain.Questions;
using ExamSimulator.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSimulator.Web.FunctionalTests;

public class QuestionAdminTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public QuestionAdminTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ExamSimulatorDbContext>))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddDbContext<ExamSimulatorDbContext>(options =>
                    options.UseInMemoryDatabase("FunctionalTests"));
            });
        });
    }

    [Fact]
    public async Task ListQuestions_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/questions");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuestion_WhenSaved_AppearsInDatabase()
    {
        var options = new DbContextOptionsBuilder<ExamSimulatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ExamSimulatorDbContext(options);

        var question = new Question(
            Guid.NewGuid(),
            "az-204",
            QuestionType.SingleChoice,
            Difficulty.Medium,
            "What is Azure App Service?",
            ["A PaaS offering", "An IaaS offering", "A SaaS offering", "A FaaS offering"],
            [0],
            "app-service");

        db.Questions.Add(question);
        await db.SaveChangesAsync();

        var saved = await db.Questions.FindAsync(question.Id);

        Assert.NotNull(saved);
        Assert.Equal("az-204", saved.ExamProfileId);
        Assert.Equal("What is Azure App Service?", saved.Prompt);
        Assert.Equal(4, saved.Options.Count);
        Assert.Equal([0], saved.CorrectOptionIndices);
        Assert.Equal("app-service", saved.TopicTag);
        Assert.Equal(QuestionType.SingleChoice, saved.Type);
        Assert.Equal(Difficulty.Medium, saved.Difficulty);
    }

    [Fact]
    public async Task EditQuestion_PageLoads_ForExistingQuestion()
    {
        var questionId = Guid.NewGuid();
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ExamSimulatorDbContext>))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ExamSimulatorDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();
                db.Questions.Add(new Question(
                    questionId,
                    "az-204",
                    QuestionType.SingleChoice,
                    Difficulty.Medium,
                    "What is Azure?",
                    ["Cloud", "Server", "Database", "Network"],
                    [0],
                    "azure-basics"));
                db.SaveChanges();
            });
        });

        var client = factory.CreateClient();

        var response = await client.GetAsync($"/questions/{questionId}/edit");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditQuestion_PageLoads_ForNonExistentQuestion()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/questions/{Guid.NewGuid()}/edit");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrderingQuestion_WhenSaved_PermutationPersistedCorrectly()
    {
        var options = new DbContextOptionsBuilder<ExamSimulatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ExamSimulatorDbContext(options);

        var question = new Question(
            Guid.NewGuid(),
            "az-204",
            QuestionType.Ordering,
            Difficulty.Medium,
            "Arrange the steps in order.",
            ["Step A", "Step B", "Step C"],
            [2, 0, 1],
            "ordering");

        db.Questions.Add(question);
        await db.SaveChangesAsync();

        var saved = await db.Questions.FindAsync(question.Id);

        Assert.NotNull(saved);
        Assert.Equal(QuestionType.Ordering, saved.Type);
        Assert.Equal(3, saved.Options.Count);
        Assert.Equal([2, 0, 1], saved.CorrectOptionIndices);
    }

    [Fact]
    public async Task ExamSession_PageLoads_ForExistingProfileWithQuestions()
    {
        const string profileId = "az-204-session-test";

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ExamSimulatorDbContext>))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ExamSimulatorDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();
                db.ExamProfiles.Add(new ExamProfile(profileId, "AZ-204 Session Test"));
                db.Questions.Add(new Question(
                    Guid.NewGuid(), profileId, QuestionType.SingleChoice, Difficulty.Easy,
                    "What is Azure?", ["Cloud", "Server", "Database", "Network"], [0], "azure"));
                db.SaveChanges();
            });
        });

        var client = factory.CreateClient();

        var response = await client.GetAsync($"/exams/{profileId}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamSession_PageLoads_ForNonExistentProfile()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/exams/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
