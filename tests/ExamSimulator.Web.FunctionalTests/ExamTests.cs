using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using ExamSimulator.Web.Infrastructure;
using ExamSimulator.Web.Domain.ExamProfiles;
using ExamSimulator.Web.Domain.Questions;
using ExamSimulator.Web.Domain.Attempts;

namespace ExamSimulator.Web.FunctionalTests;

public class ExamTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExamTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("ExamFunctionalTests"));
            });
        });
    }

    [Fact]
    public async Task ListExamProfiles_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/exam-profiles");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamList_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/exams");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamSession_WithProfileAndQuestions_ReturnsSuccessStatusCode()
    {
        // Arrange — seed a profile + one question so the session page renders
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();
        var profile = new ExamProfile("test-profile-session", "Test Profile");
        db.ExamProfiles.Add(profile);
        db.Questions.Add(new Question(
            Guid.NewGuid(), "test-profile-session", QuestionType.SingleChoice, Difficulty.Easy,
            "What is 2+2?", ["3", "4", "5"], [1], "arithmetic", null, null));
        await db.SaveChangesAsync();

        var client = CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/exams/test-profile-session");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamSession_WithUnknownProfileId_ReturnsSuccessStatusCode()
    {
        // The page always returns 200 — the "not found" message is Blazor rendered content
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/exams/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamAttempts_AfterSaving_CanBeRetrievedByUserAndProfile()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExamSimulatorDbContext>();

        var attemptId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var attempt = new ExamAttempt(
            attemptId, "test-user-id", "az-204", DateTime.UtcNow,
            7, 10, ["compute"], ["Medium"], true);
        db.ExamAttempts.Add(attempt);
        db.ExamAttemptAnswers.Add(new ExamAttemptAnswer(Guid.NewGuid(), attemptId, questionId, true));
        await db.SaveChangesAsync();

        // Act
        var saved = await db.ExamAttempts
            .Where(a => a.UserId == "test-user-id" && a.ProfileId == "az-204")
            .ToListAsync();
        var answers = await db.ExamAttemptAnswers
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync();

        // Assert
        Assert.Single(saved);
        Assert.Equal(7, saved[0].Score);
        Assert.Equal(10, saved[0].Total);
        Assert.Single(answers);
        Assert.True(answers[0].IsCorrect);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ExamSimulatorDbContext>))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddDbContext<ExamSimulatorDbContext>(options =>
                    options.UseInMemoryDatabase("ExamFunctionalTests-Auth"));

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });

                services.AddAuthentication()
                    .AddScheme<TestAuthOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        opts => opts.Role = null);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }
}

