using ExamSimulator.Web.Domain.Attempts;
using ExamSimulator.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSimulator.Web.FunctionalTests;

public class AttemptReviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AttemptReviewTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("AttemptReviewFunctionalTests"));
            });
        });
    }

    [Fact]
    public async Task AttemptReview_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/attempts/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task AttemptReview_PageLoads_ForNonExistentAttempt()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/attempts/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamAttemptAnswer_WhenSaved_SelectedOptionIndicesPersistedCorrectly()
    {
        var options = new DbContextOptionsBuilder<ExamSimulatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ExamSimulatorDbContext(options);

        var attemptId = Guid.NewGuid();
        var attempt = new ExamAttempt(attemptId, "user-1", "az-204", DateTime.UtcNow, 1, 1, ["app-service"], ["Easy"], false);
        db.ExamAttempts.Add(attempt);

        var answer = new ExamAttemptAnswer(Guid.NewGuid(), attemptId, Guid.NewGuid(), true, [1, 2]);
        db.ExamAttemptAnswers.Add(answer);
        await db.SaveChangesAsync();

        var saved = await db.ExamAttemptAnswers.FindAsync(answer.Id);

        Assert.NotNull(saved);
        Assert.NotNull(saved.SelectedOptionIndices);
        Assert.Equal([1, 2], saved.SelectedOptionIndices);
    }

    [Fact]
    public async Task ExamAttemptAnswer_WhenSavedWithNullIndices_SelectedOptionIndicesIsNull()
    {
        var options = new DbContextOptionsBuilder<ExamSimulatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ExamSimulatorDbContext(options);

        var attemptId = Guid.NewGuid();
        var attempt = new ExamAttempt(attemptId, "user-1", "az-204", DateTime.UtcNow, 0, 1, ["app-service"], ["Easy"], false);
        db.ExamAttempts.Add(attempt);

        var answer = new ExamAttemptAnswer(Guid.NewGuid(), attemptId, Guid.NewGuid(), false, null);
        db.ExamAttemptAnswers.Add(answer);
        await db.SaveChangesAsync();

        var saved = await db.ExamAttemptAnswers.FindAsync(answer.Id);

        Assert.NotNull(saved);
        Assert.Null(saved.SelectedOptionIndices);
    }
}
