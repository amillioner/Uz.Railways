using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests.RailApi.Tests.IntegrationTests;

public class WagonUpdateProcessingTests : IClassFixture<WebApplicationFactoryFixture<Program>>
{
    private readonly WebApplicationFactoryFixture<Program> _factory;

    public WagonUpdateProcessingTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessWagonUpdate_ValidMessage_CreatesTrainAndWagon()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IWagonUpdateProcessor>();
        var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();

        var message = new WagonUpdateMessage
        {
            Wagon = "TEST001",
            LoadFlag = 1,
            Weight = 50.5m,
            TrainIndexRaw = "7478-035-6980",
            Date = DateTime.UtcNow,
            Source = "test-source",
            EventId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await processor.ProcessWagonUpdateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.TrainId);
        Assert.NotNull(result.WagonId);

        // Verify data in database
        var train = await context.Trains.FindAsync(result.TrainId);
        Assert.NotNull(train);
        Assert.Equal("7478 035 6980", train.NormalizedIndex);

        var wagon = await context.Wagons.FindAsync(result.WagonId);
        Assert.NotNull(wagon);
        Assert.Equal("TEST001", wagon.Number);
        Assert.True(wagon.IsLoaded);
        Assert.Equal(50.5m, wagon.WeightKg);
    }

    [Fact]
    public async Task ProcessWagonUpdate_DuplicateEventId_ReturnsSuccess()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IWagonUpdateProcessor>();
        var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();

        var eventId = Guid.NewGuid().ToString();
        var message = new WagonUpdateMessage
        {
            Wagon = "TEST002",
            LoadFlag = 1,
            Weight = 60.0m,
            TrainIndexRaw = "7478-035-6981",
            Date = DateTime.UtcNow,
            Source = "test-source",
            EventId = eventId
        };

        // Process first time
        await processor.ProcessWagonUpdateAsync(message);

        // Act - Process same message again
        var result = await processor.ProcessWagonUpdateAsync(message);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("already processed", result.ErrorMessage ?? "");

        // Verify only one train was created
        var trainCount = await context.Trains.CountAsync(t => t.NormalizedIndex == "7478 035 6981");
        Assert.Equal(1, trainCount);
    }

    [Fact]
    public async Task ProcessWagonUpdate_InvalidTrainIndex_ReturnsFailure()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IWagonUpdateProcessor>();

        var message = new WagonUpdateMessage
        {
            Wagon = "TEST003",
            LoadFlag = 1,
            Weight = 60.0m,
            TrainIndexRaw = "invalid-index",
            Date = DateTime.UtcNow,
            Source = "test-source",
            EventId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await processor.ProcessWagonUpdateAsync(message);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.ShouldRetry);
        Assert.Contains("Invalid train index", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ProcessWagonUpdate_ExistingWagon_UpdatesWagon()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IWagonUpdateProcessor>();
        var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();

        // Create initial train and wagon
        var train = new Train
        {
            NormalizedIndex = "7478 035 6982",
            CreatedAt = DateTime.UtcNow
        };
        context.Trains.Add(train);
        await context.SaveChangesAsync();

        var existingWagon = new Wagon
        {
            Number = "TEST004",
            IsLoaded = false,
            WeightKg = 0m,
            Date = DateTime.UtcNow.AddHours(-1),
            TrainId = train.Id
        };
        context.Wagons.Add(existingWagon);
        await context.SaveChangesAsync();

        var message = new WagonUpdateMessage
        {
            Wagon = "TEST004",
            LoadFlag = 1,
            Weight = 75.0m,
            TrainIndexRaw = "7478-035-6982",
            Date = DateTime.UtcNow,
            Source = "test-source",
            EventId = Guid.NewGuid().ToString()
        };

        // Act
        var result = await processor.ProcessWagonUpdateAsync(message);

        // Assert
        Assert.True(result.Success);

        // Verify wagon was updated
        var updatedWagon = await context.Wagons.FindAsync(existingWagon.Id);
        Assert.NotNull(updatedWagon);
        Assert.True(updatedWagon.IsLoaded);
        Assert.Equal(75.0m, updatedWagon.WeightKg);
        Assert.True(updatedWagon.Date > existingWagon.Date);

        // Verify only one wagon exists with this number for this train
        var wagonCount = await context.Wagons.CountAsync(w => w.Number == "TEST004" && w.TrainId == train.Id);
        Assert.Equal(1, wagonCount);
    }
}