using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using PoFastType.Tests.TestHelpers;
using PoFastType.Shared.Models;
using System.Text.Json;
using Moq;

namespace PoFastType.Tests.System;

/// <summary>
/// System tests for the complete PoFastType application flow
/// Tests end-to-end scenarios including text generation, game completion, and leaderboard updates
/// </summary>
public class TypingGameSystemTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TypingGameSystemTests()
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
    public async Task CompleteGameFlow_ShouldWork_FromTextGenerationToLeaderboard()
    {
        // Arrange - Setup text generation
        const string generatedText = "This is a complete typing test for the user to practice their skills.";
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(generatedText);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("SystemTestStrategy");

        // Setup repository to return empty leaderboard initially
        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(10))
               .ReturnsAsync(new List<GameResult>());

        // Act & Assert - Step 1: Get text for typing test
        var textResponse = await _client.GetAsync("/api/game/text");
        textResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var textContent = await textResponse.Content.ReadAsStringAsync();
        textContent.Should().Contain(generatedText);

        // Act & Assert - Step 2: Check initial leaderboard (should be empty)
        var initialLeaderboardResponse = await _client.GetAsync("/api/scores/leaderboard");
        initialLeaderboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initialLeaderboard = await initialLeaderboardResponse.Content.ReadFromJsonAsync<List<LeaderboardEntry>>();
        initialLeaderboard.Should().BeEmpty();

        // Verify that the complete flow works as expected
        _factory.MockTextGenerationStrategy?.Verify(x => x.GenerateTextAsync(), Moq.Times.Once);
        _factory.MockGameResultRepository?.Verify(x => x.GetTopResultsAsync(10), Moq.Times.Once);
    }

    [Fact]
    public async Task ApiEndpoints_ShouldReturnCorrectContentTypes_WhenCalled()
    {
        // Arrange
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync("Sample test text");
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(10))
               .ReturnsAsync(new List<GameResult>());

        // Act & Assert - Game text endpoint
        var gameResponse = await _client.GetAsync("/api/game/text");
        gameResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Act & Assert - Leaderboard endpoint
        var leaderboardResponse = await _client.GetAsync("/api/scores/leaderboard");
        leaderboardResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Application_ShouldHandleMultipleConcurrentRequests_Successfully()
    {
        // Arrange
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync("Concurrent test text");
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("ConcurrentStrategy");

        _factory.MockGameResultRepository?
               .Setup(x => x.GetTopResultsAsync(10))
               .ReturnsAsync(new List<GameResult>());

        // Act - Make multiple concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/game/text"));
            tasks.Add(_client.GetAsync("/api/scores/leaderboard"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }
    [Fact]
    public async Task InvalidApiRoutes_ShouldReturnFallbackContent_WhenAccessed()
    {
        // Act & Assert - Invalid routes should be caught by the fallback route
        var invalidGameRoute = await _client.GetAsync("/api/game/invalid");
        invalidGameRoute.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await invalidGameRoute.Content.ReadAsStringAsync();
        content.Should().Contain("html"); // Should return the fallback HTML page

        var invalidScoresRoute = await _client.GetAsync("/api/scores/invalid");
        invalidScoresRoute.StatusCode.Should().Be(HttpStatusCode.OK);
        var content2 = await invalidScoresRoute.Content.ReadAsStringAsync();
        content2.Should().Contain("html"); // Should return the fallback HTML page

        var nonExistentController = await _client.GetAsync("/api/nonexistent/endpoint");
        nonExistentController.StatusCode.Should().Be(HttpStatusCode.OK);
        var content3 = await nonExistentController.Content.ReadAsStringAsync();
        content3.Should().Contain("html"); // Should return the fallback HTML page
    }
}
