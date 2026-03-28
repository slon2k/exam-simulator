using ExamSimulator.Web.Domain.Attempts;
using ExamSimulator.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSimulator.Web.FunctionalTests;

public class HistoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HistoryTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("HistoryFunctionalTests"));
            });
        });
    }

    [Fact]
    public async Task History_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/history");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task History_PageLoads()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/history");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task History_WithProfileFilter_PageLoads()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/history?profileId=az-204");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExamAttempts_WhenFilteredByProfileId_ReturnsOnlyMatchingAttempts()
    {
        var options = new DbContextOptionsBuilder<ExamSimulatorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ExamSimulatorDbContext(options);

        var userId = "test-user";
        db.ExamAttempts.AddRange(
            new ExamAttempt(Guid.NewGuid(), userId, "az-204", DateTime.UtcNow.AddDays(-1), 40, 60, ["app-service"], ["Easy"], true),
            new ExamAttempt(Guid.NewGuid(), userId, "az-900", DateTime.UtcNow, 80, 80, ["cloud"], ["Easy"], false)
        );
        await db.SaveChangesAsync();

        var az204 = await db.ExamAttempts.Where(a => a.UserId == userId && a.ProfileId == "az-204").ToListAsync();
        var all = await db.ExamAttempts.Where(a => a.UserId == userId).ToListAsync();

        Assert.Single(az204);
        Assert.Equal(2, all.Count);
        Assert.Equal("az-204", az204[0].ProfileId);
    }
}
