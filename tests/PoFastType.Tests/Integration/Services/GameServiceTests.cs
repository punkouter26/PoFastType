using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using PoFastType.Api.Services;
using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;

namespace PoFastType.Tests.Integration.Services;

/// <summary>
/// Integration tests for GameService - tests the service layer with repository integration
/// Tests business logic and data flow between service and repository layers
/// </summary>
public class GameServiceTests
{
    private readonly Mock<IGameResultRepository> _mockRepository;
    private readonly Mock<ILogger<GameService>> _mockLogger;
    private readonly GameService _service;

    public GameServiceTests()
    {
        _mockRepository = new Mock<IGameResultRepository>();
        _mockLogger = new Mock<ILogger<GameService>>();
        _service = new GameService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitGameResultAsync_ShouldReturnGameResult_WhenValidDataProvided()
    {
        // Arrange
        var gameResult = new GameResult
        {
            NetWPM = 60,
            Accuracy = 95,
            GrossWPM = 65,
            ProblemKeysJson = "{}"
        };
        const string userId = "user123";
        const string username = "testuser";

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<GameResult>()))
                      .ReturnsAsync(gameResult);

        // Act
        var result = await _service.SubmitGameResultAsync(gameResult, userId, username);

        // Assert
        result.Should().NotBeNull();
        result.PartitionKey.Should().Be(userId);
        result.Username.Should().Be(username);
        result.NetWPM.Should().Be(60);
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<GameResult>()), Times.Once);
    }

    [Theory]
    [InlineData(-1, 95)] // Negative WPM
    [InlineData(301, 95)] // WPM too high
    [InlineData(60, -1)] // Negative accuracy
    [InlineData(60, 101)] // Accuracy too high
    public async Task SubmitGameResultAsync_ShouldThrowArgumentException_WhenInvalidValues(double wpm, double accuracy)
    {
        // Arrange
        var gameResult = new GameResult { NetWPM = wpm, Accuracy = accuracy };

        // Act & Assert
        await _service.Invoking(x => x.SubmitGameResultAsync(gameResult, "user123", "testuser"))
                     .Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "testuser")]
    [InlineData("user123", "")]
    public async Task SubmitGameResultAsync_ShouldThrowArgumentException_WhenInvalidUserData(string userId, string username)
    {
        // Arrange
        var gameResult = new GameResult { NetWPM = 60, Accuracy = 95 };

        // Act & Assert
        await _service.Invoking(x => x.SubmitGameResultAsync(gameResult, userId, username))
                     .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetUserStatsAsync_ShouldReturnResults_WhenValidUserId()
    {
        // Arrange
        const string userId = "user123";
        var expectedResults = new List<GameResult>
        {
            new() { NetWPM = 60, Accuracy = 95, Timestamp = DateTime.UtcNow },
            new() { NetWPM = 65, Accuracy = 97, Timestamp = DateTime.UtcNow.AddMinutes(-5) }
        };

        _mockRepository.Setup(x => x.GetUserResultsAsync(userId))
                      .ReturnsAsync(expectedResults);

        // Act
        var result = await _service.GetUserStatsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        _mockRepository.Verify(x => x.GetUserResultsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldReturnLeaderboard_WhenCalled()
    {
        // Arrange
        const int topCount = 5;
        var topResults = new List<GameResult>
        {
            new() { Username = "user1", NetWPM = 100, Timestamp = DateTime.UtcNow },
            new() { Username = "user2", NetWPM = 95, Timestamp = DateTime.UtcNow },
        };

        _mockRepository.Setup(x => x.GetTopResultsAsync(topCount))
                      .ReturnsAsync(topResults);

        // Act
        var result = await _service.GetLeaderboardAsync(topCount);

        // Assert
        var leaderboard = result.ToList();
        leaderboard.Should().HaveCount(2);
        leaderboard[0].Rank.Should().Be(1);
        leaderboard[0].Username.Should().Be("user1");
        leaderboard[1].Rank.Should().Be(2);
        leaderboard[1].Username.Should().Be("user2");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetLeaderboardAsync_ShouldThrowArgumentException_WhenInvalidCount(int count)
    {
        // Act & Assert
        await _service.Invoking(x => x.GetLeaderboardAsync(count))
                     .Should().ThrowAsync<ArgumentException>();
    }
}
