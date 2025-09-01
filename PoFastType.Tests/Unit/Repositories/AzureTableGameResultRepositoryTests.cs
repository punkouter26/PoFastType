using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;

namespace PoFastType.Tests.Unit.Repositories;

/// <summary>
/// Unit tests for AzureTableGameResultRepository using Repository Pattern (GoF)
/// Tests data access layer with mocked configuration
/// </summary>
public class AzureTableGameResultRepositoryTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AzureTableGameResultRepository>> _mockLogger;

    public AzureTableGameResultRepositoryTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AzureTableGameResultRepository>>();

        // Setup configuration mock
        _mockConfiguration.Setup(x => x["AzureTableStorage:ConnectionString"])
                         .Returns("UseDevelopmentStorage=true");
        _mockConfiguration.Setup(x => x["AzureTableStorage:TableName"])
                         .Returns("TestTable");
    }

    [Fact]
    public void Constructor_ShouldCreateRepository_WhenValidConfigurationProvided()
    {
        // Act & Assert
        var repository = new AzureTableGameResultRepository(_mockConfiguration.Object, _mockLogger.Object);
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenConnectionStringMissing()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["AzureTableStorage:ConnectionString"]).Returns((string)null!);
        mockConfig.Setup(x => x["AzureTableStorage:TableName"]).Returns("TestTable");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new AzureTableGameResultRepository(mockConfig.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenTableNameMissing()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["AzureTableStorage:ConnectionString"]).Returns("UseDevelopmentStorage=true");
        mockConfig.Setup(x => x["AzureTableStorage:TableName"]).Returns((string)null!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new AzureTableGameResultRepository(mockConfig.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureTableGameResultRepository(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AzureTableGameResultRepository(_mockConfiguration.Object, null!));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowArgumentNullException_WhenGameResultIsNull()
    {
        // Arrange
        var repository = new AzureTableGameResultRepository(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await repository.Invoking(x => x.AddAsync(null!))
                       .Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserResultsAsync_ShouldThrowArgumentException_WhenInvalidUserId(string userId)
    {
        // Arrange
        var repository = new AzureTableGameResultRepository(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await repository.Invoking(x => x.GetUserResultsAsync(userId))
                       .Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetTopResultsAsync_ShouldThrowArgumentException_WhenInvalidCount(int count)
    {
        // Arrange
        var repository = new AzureTableGameResultRepository(_mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await repository.Invoking(x => x.GetTopResultsAsync(count))
                       .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Repository_ShouldImplementCorrectInterface()
    {
        // Arrange
        var repository = new AzureTableGameResultRepository(_mockConfiguration.Object, _mockLogger.Object);

        // Assert
        repository.Should().BeAssignableTo<IGameResultRepository>();
    }
}
