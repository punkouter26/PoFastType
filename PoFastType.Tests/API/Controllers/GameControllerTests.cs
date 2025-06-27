using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;
using FluentAssertions;
using System.Net.Http.Json;
using System.Net;
using PoFastType.Api.Repositories;
using PoFastType.Api.Services;
using PoFastType.Shared.Models;
using PoFastType.Tests.TestHelpers;

namespace PoFastType.Tests.API.Controllers;

/// <summary>
/// API tests for GameController using Factory Pattern (GoF)
/// Tests HTTP endpoints and response handling
/// </summary>
public class GameControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameControllerTests()
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
    public async Task GetText_ShouldReturnOk_WhenTextGenerationSucceeds()
    {
        // Arrange
        const string expectedText = "Sample text for typing practice";
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(expectedText);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedText);
    }

    [Fact]
    public async Task GetText_ShouldReturnInternalServerError_WhenTextGenerationFails()
    {
        // Arrange
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ThrowsAsync(new InvalidOperationException("Text generation failed"));
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetText_ShouldReturnJsonContent_WhenSuccessful()
    {
        // Arrange
        const string expectedText = "Test typing text content";
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(expectedText);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetText_ShouldLogGeneration_WhenCalled()
    {
        // Arrange
        const string expectedText = "Logging test text";
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(expectedText);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.Should().NotBeNull();
        _factory.MockTextGenerationStrategy?.Verify(x => x.GenerateTextAsync(), Times.Once);
    }

    [Fact]
    public async Task GetText_ShouldHandleEmptyText_Gracefully()
    {
        // Arrange
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(string.Empty);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetText_ShouldReturnDifferentTexts_OnMultipleCalls()
    {
        // Arrange
        var texts = new[] { "First text", "Second text", "Third text" };
        var callCount = 0;
        
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(() => texts[callCount++ % texts.Length]);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response1 = await _client.GetAsync("/api/game/text");
        var response2 = await _client.GetAsync("/api/game/text");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBe(content2);
    }

    [Fact]
    public async Task GetText_ShouldHaveCorrectRoute_WhenAccessed()
    {
        // Arrange
        const string expectedText = "Route test text";
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.GenerateTextAsync())
               .ReturnsAsync(expectedText);
        _factory.MockTextGenerationStrategy?
               .Setup(x => x.StrategyName)
               .Returns("TestStrategy");

        // Act
        var response = await _client.GetAsync("/api/game/text");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }    [Fact]
    public async Task GetText_ShouldReturnFallbackContent_WhenInvalidRoute()
    {
        // Act
        var response = await _client.GetAsync("/api/game/invalid");

        // Assert - Invalid routes should be caught by the fallback route
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("html"); // Should return the fallback HTML page
    }
}
