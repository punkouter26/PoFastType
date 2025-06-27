using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using PoFastType.Api.Services;

namespace PoFastType.Tests.Unit.Services;

/// <summary>
/// Unit tests for HardcodedTextStrategy using Strategy Pattern (GoF)
/// Tests concrete strategy implementation following AAA pattern
/// </summary>
public class HardcodedTextStrategyTests
{
    private readonly Mock<ILogger<HardcodedTextStrategy>> _mockLogger;
    private readonly HardcodedTextStrategy _strategy;

    public HardcodedTextStrategyTests()
    {
        _mockLogger = new Mock<ILogger<HardcodedTextStrategy>>();
        _strategy = new HardcodedTextStrategy(_mockLogger.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnNonEmptyText_WhenCalled()
    {
        // Act
        var result = await _strategy.GenerateTextAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(50); // Ensure meaningful text length
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnDifferentTexts_WhenCalledMultipleTimes()
    {
        // Arrange
        var results = new HashSet<string>();
        const int iterations = 20; // Should be enough to get some variety

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var text = await _strategy.GenerateTextAsync();
            results.Add(text);
        }

        // Assert
        results.Should().HaveCountGreaterThan(1, "Strategy should provide variety in text selection");
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnConsistentFormat_WhenCalled()
    {
        // Act
        var result = await _strategy.GenerateTextAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
        result.Should().MatchRegex(@"^[A-Z].*[.!?]$"); // Should start with capital and end with punctuation
    }

    [Fact]
    public void StrategyName_ShouldReturnCorrectName()
    {
        // Act
        var name = _strategy.StrategyName;

        // Assert
        name.Should().Be("Hardcoded");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HardcodedTextStrategy(null!));
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldContainTypingPracticeContent_WhenCalled()
    {
        // Arrange
        var results = new List<string>();
        const int iterations = 10;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            results.Add(await _strategy.GenerateTextAsync());
        }

        // Assert
        results.Should().OnlyContain(text => 
            text.Length > 100 && 
            text.Contains(' ') && // Should contain spaces for word separation
            !text.Contains('\n') && // Should not contain line breaks
            !text.Contains('\t')); // Should not contain tabs
    }

    [Fact]
    public async Task GenerateTextAsync_ShouldBeThreadSafe_WhenCalledConcurrently()
    {
        // Arrange
        const int taskCount = 10;
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(_strategy.GenerateTextAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().OnlyContain(text => !string.IsNullOrEmpty(text));
        results.Should().HaveCount(taskCount);
    }
}
