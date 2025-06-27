using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PoFastType.Api.Services;
using PoFastType.Api.Repositories;

namespace PoFastType.Tests.TestHelpers;

/// <summary>
/// Test Factory using Factory Pattern (GoF) for creating test instances
/// This factory is used across Unit, Integration, API, and System tests
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IGameResultRepository>? MockGameResultRepository { get; private set; }
    public Mock<ITextGenerationStrategy>? MockTextGenerationStrategy { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing services
            var descriptors = services.Where(d => 
                d.ServiceType == typeof(IGameResultRepository) ||
                d.ServiceType == typeof(ITextGenerationStrategy))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Create mocks
            MockGameResultRepository = new Mock<IGameResultRepository>();
            MockTextGenerationStrategy = new Mock<ITextGenerationStrategy>();

            // Add mocked services
            services.AddSingleton(MockGameResultRepository.Object);
            services.AddSingleton(MockTextGenerationStrategy.Object);
        });
    }
}
