using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using System.Net;
using PoFastType.Api.Repositories;
using PoFastType.Api.Services;
using PoFastType.Shared.Models;
using System.Text.Json;
using PoFastType.Tests.TestHelpers;

namespace PoFastType.Tests.API.Controllers;

/// <summary>
/// API tests for ScoresController using Factory Pattern (GoF)
/// Tests HTTP endpoints for game results and leaderboard
/// </summary>
public class ScoresControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ScoresControllerTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnOk_WhenLeaderboardExists()
    {        // Arrange
        var leaderboardData = new List<LeaderboardEntry>
        {
            new() { Rank = 1, Username = "player1", NetWPM = 100, Timestamp = DateTime.UtcNow },
            new() { Rank = 2, Username = "player2", NetWPM = 95, Timestamp = DateTime.UtcNow }
        };

        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(It.IsAny<int>()))
               .ReturnsAsync(leaderboardData.Select(l => new GameResult 
               { 
                   Username = l.Username, 
                   NetWPM = l.NetWPM, 
                   GameTimestamp = l.Timestamp 
               }));

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("player1");
        content.Should().Contain("player2");
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnEmptyArray_WhenNoResults()
    {
        // Arrange
        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(It.IsAny<int>()))
               .ReturnsAsync(new List<GameResult>());

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<LeaderboardEntry[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        leaderboard.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnCorrectContentType_WhenSuccessful()
    {
        // Arrange
        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(It.IsAny<int>()))
               .ReturnsAsync(new List<GameResult>());

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnInternalServerError_WhenRepositoryFails()
    {
        // Arrange
        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(It.IsAny<int>()))
               .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldHandleCustomTopCount_WhenProvided()
    {
        // Arrange
        const int customCount = 5;
        var results = Enumerable.Range(1, customCount)                               .Select(i => new GameResult 
                               { 
                                   Username = $"user{i}", 
                                   NetWPM = 100 - i, 
                                   Accuracy = 95 + i,
                                   GameTimestamp = DateTime.UtcNow 
                               })
                               .ToList();

        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(customCount))
               .ReturnsAsync(results);

        // Act
        var response = await _client.GetAsync($"/api/scores/leaderboard?top={customCount}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.MockGameResultRepository?.Verify(x => x.GetTopResultsAsync(customCount), Times.Once);
    }    [Fact]
    public async Task GetLeaderboard_ShouldReturnCorrectRanking_WhenMultipleResults()
    {
        // Arrange
        var gameResults = new List<GameResult>
        {
            new() { Username = "fastest", NetWPM = 120, Accuracy = 99, GameTimestamp = DateTime.UtcNow },
            new() { Username = "second", NetWPM = 110, Accuracy = 98, GameTimestamp = DateTime.UtcNow },
            new() { Username = "third", NetWPM = 100, Accuracy = 97, GameTimestamp = DateTime.UtcNow }
        };

        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(It.IsAny<int>()))
               .ReturnsAsync(gameResults);

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var leaderboard = JsonSerializer.Deserialize<LeaderboardEntry[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        leaderboard.Should().HaveCount(3);
        leaderboard![0].Rank.Should().Be(1);
        leaderboard[0].Username.Should().Be("fastest");
        leaderboard[1].Rank.Should().Be(2);
        leaderboard[1].Username.Should().Be("second");
        leaderboard[2].Rank.Should().Be(3);
        leaderboard[2].Username.Should().Be("third");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetLeaderboard_ShouldReturnBadRequest_WhenInvalidTopCount(int invalidCount)
    {
        // Act
        var response = await _client.GetAsync($"/api/scores/leaderboard?top={invalidCount}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldUseDefaultTopCount_WhenParameterNotProvided()
    {
        // Arrange
        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(10)) // Default is 10
               .ReturnsAsync(new List<GameResult>());

        // Act
        var response = await _client.GetAsync("/api/scores/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.MockGameResultRepository?.Verify(x => x.GetTopResultsAsync(10), Times.Once);
    }    [Fact]
    public async Task GetLeaderboard_ShouldReturnFallbackContent_WhenInvalidRoute()
    {
        // Act
        var response = await _client.GetAsync("/api/scores/invalid");

        // Assert - Invalid routes should be caught by the fallback route
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("html"); // Should return the fallback HTML page
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnMethodNotAllowed_WhenWrongHttpMethod()
    {
        // Act
        var response = await _client.PostAsync("/api/scores/leaderboard", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
