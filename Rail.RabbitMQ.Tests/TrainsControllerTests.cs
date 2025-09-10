using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests;

public class TrainsControllerTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactoryFixture<Program> _factory;

    public TrainsControllerTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTrains_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/trains");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTrains_WithValidAuth_ReturnsPagedResult()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/trains?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("items", out _));
        Assert.True(result.TryGetProperty("totalCount", out _));
        Assert.True(result.TryGetProperty("pageNumber", out _));
    }

    [Fact]
    public async Task GetTrainStats_ExistingTrain_ReturnsStats()
    {
        // Arrange
        await SeedTestDataAsync();
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/trains/7478%20035%206980/stats");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.Equal("7478 035 6980", stats.GetProperty("normalizedIndex").GetString());
        Assert.True(stats.GetProperty("totalWagons").GetInt32() > 0);
    }

    [Fact]
    public async Task GetTrainStats_NonexistentTrain_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/trains/9999%20999%209999/stats");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin123"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return loginResponse!.Token;
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();

        // Clear existing data
        context.Wagons.RemoveRange(context.Wagons);
        context.Trains.RemoveRange(context.Trains);
        await context.SaveChangesAsync();

        // Add test train
        var train = new Train
        {
            NormalizedIndex = "7478 035 6980",
            CreatedAt = DateTime.UtcNow
        };
        context.Trains.Add(train);
        await context.SaveChangesAsync();

        // Add test wagons
        var wagons = new[]
        {
            new Wagon
            {
                Number = "WAG001",
                IsLoaded = true,
                WeightKg = 45.5m,
                Date = DateTime.UtcNow,
                TrainId = train.Id
            },
            new Wagon
            {
                Number = "WAG002",
                IsLoaded = false,
                WeightKg = 0m,
                Date = DateTime.UtcNow,
                TrainId = train.Id
            }
        };

        context.Wagons.AddRange(wagons);
        await context.SaveChangesAsync();
    }
}