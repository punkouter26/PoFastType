using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using PoFastType.Tests.TestHelpers;

namespace PoFastType.Tests.API.Controllers;

/// <summary>
/// API tests for DiagController - Health Check Endpoints
/// Tests comprehensive diagnostic functionality including Azure connectivity,
/// Table Storage, and service health monitoring
/// </summary>
public class DiagControllerTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DiagControllerTests()
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
    public async Task Health_ShouldReturnOk_WhenEndpointIsCalled()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ShouldReturnJsonResponse_WithCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Health_ShouldReturnDiagHealthResponse_WithRequiredFields()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("\"status\":", "Response should contain status field");
        content.Should().Contain("\"timestamp\":", "Response should contain timestamp field");
        content.Should().Contain("\"checks\":", "Response should contain checks array field");
    }

    [Fact]
    public async Task Health_ShouldIncludeInternetConnectivityCheck()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Internet Connectivity", "Health check should include internet connectivity");
    }

    [Fact]
    public async Task Health_ShouldIncludeAzureConnectivityCheck()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Azure Connectivity", "Health check should include Azure connectivity");
    }

    [Fact]
    public async Task Health_ShouldIncludeTableStorageCheck()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Azure Table Storage", "Health check should include Table Storage status");
    }

    [Fact]
    public async Task Health_ShouldIncludeHealthCheckStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Health Check", "Health check should include self-check status");
    }

    [Fact]
    public async Task Health_ShouldIncludeBackendAPICheck()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Backend API", "Health check should include API status");
    }

    [Fact]
    public async Task Health_ShouldIncludeAzureOpenAICheck()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Azure OpenAI", "Health check should include OpenAI status");
    }

    [Fact]
    public async Task Health_ShouldReturnTimestamp_InUtcFormat()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().MatchRegex(@"""timestamp"":\s*""202\d-\d{2}-\d{2}T", 
            "Timestamp should be in ISO 8601 format");
    }

    [Fact]
    public async Task Health_ShouldReturnAllChecks_WithStatusFields()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("\"name\":", "Each check should have a name");
        content.Should().Contain("\"status\":", "Each check should have a status");
        content.Should().Contain("\"message\":", "Each check should have a message");
    }

    [Fact]
    public async Task Health_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(15); // Health checks should complete within 15 seconds
        using var cts = new CancellationTokenSource(timeout);

        // Act
        var response = await _client.GetAsync("/api/diag/health", cts.Token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "Health check should complete successfully within timeout");
    }

    [Fact]
    public async Task Health_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Act - Call health check 3 times
        var response1 = await _client.GetAsync("/api/diag/health");
        var response2 = await _client.GetAsync("/api/diag/health");
        var response3 = await _client.GetAsync("/api/diag/health");

        // Assert - All calls should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ShouldHandleTableStorageConnection_Gracefully()
    {
        // This test verifies that even if Table Storage has issues,
        // the health endpoint still returns a response

        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "Health endpoint should return OK even if some checks fail");
        content.Should().Contain("Azure Table Storage", 
            "Response should include Table Storage check result");
    }

    [Fact]
    public async Task Health_ShouldNotThrowException_WhenDependenciesAreUnavailable()
    {
        // This test ensures the health endpoint is resilient to failures

        // Act
        Func<Task> act = async () => await _client.GetAsync("/api/diag/health");

        // Assert
        await act.Should().NotThrowAsync("Health check should handle all errors gracefully");
    }

    [Fact]
    public async Task Health_ResponseStructure_ShouldMatchExpectedSchema()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Verify JSON structure
        content.Should().StartWith("{", "Response should be valid JSON object");
        content.Should().EndWith("}", "Response should be valid JSON object");
        content.Should().Contain("\"checks\":[", "Checks should be an array");
    }

    [Fact]
    public async Task Health_ShouldReturnMultipleChecks_InChecksArray()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Count occurrences of "name": which indicates individual checks
        var checkCount = global::System.Text.RegularExpressions.Regex.Matches(content, @"""name"":").Count;

        // Assert
        checkCount.Should().BeGreaterThan(3, 
            "Health check should return multiple diagnostic checks");
    }

    [Fact]
    public async Task Health_ShouldIncludeAzuriteReference_InTableStorageMessage()
    {
        // Act
        var response = await _client.GetAsync("/api/diag/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("Azurite", 
            "Table Storage check should reference Azurite local emulator");
    }
}
