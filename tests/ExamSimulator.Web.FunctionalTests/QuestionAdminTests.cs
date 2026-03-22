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
                // EF Core 8+ accumulates IDbContextOptionsConfiguration<T> registrations to build
                // DbContextOptions. Remove the SQLite one before adding InMemory.
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
}
