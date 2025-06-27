using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using PoFastType.Api.Services;

namespace PoFastType.Tests.Unit.Services;

/// <summary>
/// Unit tests for TextGenerationService - tests the service in isolation using Strategy pattern
/// </summary>
public class TextGenerationServiceTests
{
    private readonly Mock<ITextGenerationStrategy> _mockStrategy;
    private readonly Mock<ILogger<TextGenerationService>> _mockLogger;
    private readonly TextGenerationService _service;

    public TextGenerationServiceTests()
    {
        _mockStrategy = new Mock<ITextGenerationStrategy>();
        _mockLogger = new Mock<ILogger<TextGenerationService>>();
        _service = new TextGenerationService(_mockStrategy.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnText_WhenStrategyReturnsValidText()
    {
        // Arrange
        const string expectedText = "Sample text for typing";
        _mockStrategy.Setup(x => x.GenerateTextAsync())
                    .ReturnsAsync(expectedText);
        _mockStrategy.Setup(x => x.StrategyName)
                    .Returns("TestStrategy");

        // Act
        var result = await _service.GenerateTextAsync();

        // Assert
        result.Should().Be(expectedText);
        _mockStrategy.Verify(x => x.GenerateTextAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldThrowException_WhenStrategyReturnsNull()
    {
        // Arrange
        _mockStrategy.Setup(x => x.GenerateTextAsync())
                    .ReturnsAsync((string)null!);
        _mockStrategy.Setup(x => x.StrategyName)
                    .Returns("TestStrategy");

        // Act & Assert
        await _service.Invoking(x => x.GenerateTextAsync())
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Generated text cannot be null or empty");
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldThrowException_WhenStrategyReturnsEmpty()
    {
        // Arrange
        _mockStrategy.Setup(x => x.GenerateTextAsync())
                    .ReturnsAsync(string.Empty);
        _mockStrategy.Setup(x => x.StrategyName)
                    .Returns("TestStrategy");

        // Act & Assert
        await _service.Invoking(x => x.GenerateTextAsync())
                     .Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("Generated text cannot be null or empty");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenStrategyIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TextGenerationService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TextGenerationService(_mockStrategy.Object, null!));
    }
}
