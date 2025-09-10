using System.Data.Common;

namespace RailApi.Tests.IntegrationTests;

public class WebApplicationFactoryFixture<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RailDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection));
            if (dbConnectionDescriptor != null)
                services.Remove(dbConnectionDescriptor);

            // Add in-memory database for testing
            services.AddDbContext<RailDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Replace RabbitMQ service with mock for testing
            var rabbitMqDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IRabbitMqService));
            if (rabbitMqDescriptor != null)
                services.Remove(rabbitMqDescriptor);

            services.AddSingleton<IRabbitMqService, MockRabbitMqService>();

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RailDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplicationFactoryFixture<TProgram>>>();

            try
            {
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the test database");
            }
        });

        builder.UseEnvironment("Testing");
    }
}