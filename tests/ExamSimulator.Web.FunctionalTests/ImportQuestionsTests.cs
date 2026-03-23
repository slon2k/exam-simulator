using ExamSimulator.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ExamSimulator.Web.FunctionalTests;

public class ImportQuestionsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportQuestionsTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("ImportQuestionsTests"));
            });
        });
    }

    [Fact]
    public async Task ImportPage_WhenUnauthenticated_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/questions/import");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task ImportPage_WhenAuthenticatedWithoutAdminRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(role: null);

        var response = await client.GetAsync("/questions/import");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ImportPage_WhenAuthenticatedAsAdmin_ReturnsOk()
    {
        var client = CreateAuthenticatedClient(role: "Admin");

        var response = await client.GetAsync("/questions/import");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string? role)
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
                    options.UseInMemoryDatabase("ImportQuestionsTests-Authenticated"));

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });

                services.AddAuthentication()
                    .AddScheme<TestAuthOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        opts => opts.Role = role);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }
}
