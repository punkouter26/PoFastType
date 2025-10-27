using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using PoFastType.Api.Repositories;
using PoFastType.Shared.Models;

namespace PoFastType.Tests.Integration;

/// <summary>
/// Integration tests for Azure Table Storage using Azurite local emulator.
/// These tests verify actual CRUD operations against the storage emulator.
/// 
/// Prerequisites:
/// - Azurite must be running (azurite --silent --location ./AzuriteData)
/// - Connection string: UseDevelopmentStorage=true
/// </summary>
[Collection("Sequential")] // Run sequentially to avoid table conflicts
public class AzureTableStorageIntegrationTests : IAsyncLifetime
{
    private readonly AzureTableGameResultRepository _repository;
    private readonly string _testUserId;
    private readonly List<GameResult> _testResults;

    public AzureTableStorageIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureTableStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureTableStorage:TableName"] = $"IntegrationTest{Guid.NewGuid():N}"
            })
            .Build();

        var logger = new Mock<ILogger<AzureTableGameResultRepository>>().Object;
        _repository = new AzureTableGameResultRepository(configuration, logger);
        
        _testUserId = $"testuser{Guid.NewGuid():N}";
        _testResults = new List<GameResult>();
    }

    public Task InitializeAsync()
    {
        // Setup runs before each test
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Cleanup after tests - this is best effort, table cleanup happens in Azure
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddAsync_ShouldSuccessfullyAddGameResult_ToAzurite()
    {
        // Arrange
        var gameResult = new GameResult
        {
            PartitionKey = _testUserId,
            Username = "IntegrationTestUser",
            NetWPM = 85.5,
            Accuracy = 97.3,
            GrossWPM = 90.2,
            CompositeScore = 175.5,
            ProblemKeysJson = "{\"q\":1,\"p\":2}",
            GameTimestamp = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(gameResult);

        // Assert
        result.Should().NotBeNull();
        result.PartitionKey.Should().Be(_testUserId);
        result.RowKey.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("IntegrationTestUser");
        result.NetWPM.Should().Be(85.5);
        result.Accuracy.Should().Be(97.3);
        result.CompositeScore.Should().Be(175.5);

        _testResults.Add(result);
    }

    [Fact]
    public async Task AddAsync_ShouldGenerateUniqueRowKeys_ForMultipleResults()
    {
        // Arrange
        var results = new List<GameResult>
        {
            new() { PartitionKey = _testUserId, Username = "User1", NetWPM = 60, Accuracy = 95, GrossWPM = 65, CompositeScore = 100 },
            new() { PartitionKey = _testUserId, Username = "User1", NetWPM = 70, Accuracy = 96, GrossWPM = 75, CompositeScore = 110 },
            new() { PartitionKey = _testUserId, Username = "User1", NetWPM = 80, Accuracy = 97, GrossWPM = 85, CompositeScore = 120 }
        };

        // Act
        var addedResults = new List<GameResult>();
        foreach (var result in results)
        {
            await Task.Delay(10); // Small delay to ensure different timestamps
            addedResults.Add(await _repository.AddAsync(result));
        }

        // Assert
        addedResults.Should().HaveCount(3);
        var rowKeys = addedResults.Select(r => r.RowKey).ToList();
        rowKeys.Should().OnlyHaveUniqueItems("each game result should have a unique RowKey");

        _testResults.AddRange(addedResults);
    }

    [Fact]
    public async Task GetUserResultsAsync_ShouldReturnAllResults_ForSpecificUser()
    {
        // Arrange
        var testUser = $"queryuser{Guid.NewGuid():N}";
        var gamesToAdd = new List<GameResult>
        {
            new() { PartitionKey = testUser, Username = "QueryUser", NetWPM = 50, Accuracy = 90, GrossWPM = 55, CompositeScore = 90 },
            new() { PartitionKey = testUser, Username = "QueryUser", NetWPM = 60, Accuracy = 92, GrossWPM = 65, CompositeScore = 100 },
            new() { PartitionKey = testUser, Username = "QueryUser", NetWPM = 70, Accuracy = 94, GrossWPM = 75, CompositeScore = 110 }
        };

        foreach (var game in gamesToAdd)
        {
            await Task.Delay(10);
            _testResults.Add(await _repository.AddAsync(game));
        }

        // Act
        var results = await _repository.GetUserResultsAsync(testUser);

        // Assert
        var resultList = results.ToList();
        resultList.Should().HaveCount(3);
        resultList.Should().OnlyContain(r => r.PartitionKey == testUser);
        resultList.Should().BeInDescendingOrder(r => r.GameTimestamp, "results should be ordered by timestamp descending");
    }

    [Fact]
    public async Task GetUserResultsAsync_ShouldReturnEmptyList_WhenUserHasNoResults()
    {
        // Arrange
        var nonExistentUser = $"nonexistent{Guid.NewGuid():N}";

        // Act
        var results = await _repository.GetUserResultsAsync(nonExistentUser);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopResultsAsync_ShouldReturnResultsOrderedByCompositeScore()
    {
        // Arrange
        var leaderboardUser1 = $"leader1{Guid.NewGuid():N}";
        var leaderboardUser2 = $"leader2{Guid.NewGuid():N}";
        
        var gamesToAdd = new List<GameResult>
        {
            new() { PartitionKey = leaderboardUser1, Username = "Leader1", NetWPM = 50, Accuracy = 90, GrossWPM = 55, CompositeScore = 50 },
            new() { PartitionKey = leaderboardUser1, Username = "Leader1", NetWPM = 100, Accuracy = 98, GrossWPM = 105, CompositeScore = 200 },
            new() { PartitionKey = leaderboardUser2, Username = "Leader2", NetWPM = 75, Accuracy = 95, GrossWPM = 80, CompositeScore = 150 },
            new() { PartitionKey = leaderboardUser2, Username = "Leader2", NetWPM = 60, Accuracy = 92, GrossWPM = 65, CompositeScore = 100 }
        };

        foreach (var game in gamesToAdd)
        {
            await Task.Delay(10);
            _testResults.Add(await _repository.AddAsync(game));
        }

        // Act
        var topResults = await _repository.GetTopResultsAsync(10);

        // Assert
        var topList = topResults.ToList();
        topList.Should().NotBeEmpty();
        topList.Should().BeInDescendingOrder(r => r.CompositeScore, "results should be ordered by CompositeScore descending");
        
        // The highest score should be the 200 composite score
        var highestScore = topList.FirstOrDefault(r => r.PartitionKey == leaderboardUser1 && r.CompositeScore == 200);
        highestScore.Should().NotBeNull("the result with composite score 200 should be in top results");
    }

    [Fact]
    public async Task GetTopResultsAsync_ShouldRespectCountLimit()
    {
        // Arrange - add several results
        var limitTestUser = $"limit{Guid.NewGuid():N}";
        for (int i = 0; i < 5; i++)
        {
            var game = new GameResult
            {
                PartitionKey = limitTestUser,
                Username = "LimitTest",
                NetWPM = 50 + (i * 10),
                Accuracy = 90 + i,
                GrossWPM = 55 + (i * 10),
                CompositeScore = 100 + (i * 20)
            };
            await Task.Delay(10);
            _testResults.Add(await _repository.AddAsync(game));
        }

        // Act
        var top3Results = await _repository.GetTopResultsAsync(3);

        // Assert
        var resultList = top3Results.ToList();
        resultList.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Arrange
        var existsTestUser = $"exists{Guid.NewGuid():N}";
        var gameResult = new GameResult
        {
            PartitionKey = existsTestUser,
            Username = "ExistsTest",
            NetWPM = 65,
            Accuracy = 93,
            GrossWPM = 70,
            CompositeScore = 120
        };

        var added = await _repository.AddAsync(gameResult);
        _testResults.Add(added);

        // Act
        var exists = await _repository.ExistsAsync(added.PartitionKey, added.RowKey);

        // Assert
        exists.Should().BeTrue("the entity should exist after being added");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        // Arrange
        var nonExistentUser = $"noexist{Guid.NewGuid():N}";
        var nonExistentRowKey = "9999999999999999999";

        // Act
        var exists = await _repository.ExistsAsync(nonExistentUser, nonExistentRowKey);

        // Assert
        exists.Should().BeFalse("the entity should not exist");
    }

    [Fact]
    public async Task AddAsync_ShouldPreserveJsonData()
    {
        // Arrange
        var jsonTestUser = $"json{Guid.NewGuid():N}";
        var problemKeysJson = "{\"a\":5,\"s\":3,\"d\":2,\"f\":1}";
        var gameResult = new GameResult
        {
            PartitionKey = jsonTestUser,
            Username = "JsonTest",
            NetWPM = 72,
            Accuracy = 96.5,
            GrossWPM = 78,
            CompositeScore = 140,
            ProblemKeysJson = problemKeysJson
        };

        // Act
        var added = await _repository.AddAsync(gameResult);
        _testResults.Add(added);

        // Retrieve and verify
        var retrieved = await _repository.GetUserResultsAsync(jsonTestUser);
        var retrievedResult = retrieved.FirstOrDefault();

        // Assert
        retrievedResult.Should().NotBeNull();
        retrievedResult!.ProblemKeysJson.Should().Be(problemKeysJson, "JSON data should be preserved exactly");
    }

    [Fact]
    public async Task GetUserResultsAsync_ShouldMapAllProperties_Correctly()
    {
        // Arrange
        var mappingTestUser = $"mapping{Guid.NewGuid():N}";
        var expectedGame = new GameResult
        {
            PartitionKey = mappingTestUser,
            Username = "MappingTest",
            NetWPM = 88.7,
            Accuracy = 99.1,
            GrossWPM = 92.3,
            CompositeScore = 185.5,
            ProblemKeysJson = "{\"z\":1}",
            GameTimestamp = DateTime.UtcNow
        };

        var added = await _repository.AddAsync(expectedGame);
        _testResults.Add(added);

        // Act
        var results = await _repository.GetUserResultsAsync(mappingTestUser);
        var actualGame = results.FirstOrDefault();

        // Assert
        actualGame.Should().NotBeNull();
        actualGame!.PartitionKey.Should().Be(expectedGame.PartitionKey);
        actualGame.Username.Should().Be(expectedGame.Username);
        actualGame.NetWPM.Should().Be(expectedGame.NetWPM);
        actualGame.Accuracy.Should().Be(expectedGame.Accuracy);
        actualGame.GrossWPM.Should().Be(expectedGame.GrossWPM);
        actualGame.CompositeScore.Should().Be(expectedGame.CompositeScore);
        actualGame.ProblemKeysJson.Should().Be(expectedGame.ProblemKeysJson);
        actualGame.GameTimestamp.Should().BeCloseTo(expectedGame.GameTimestamp, TimeSpan.FromSeconds(1));
    }
}
