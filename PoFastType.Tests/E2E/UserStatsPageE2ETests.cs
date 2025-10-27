using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace PoFastType.Tests.E2E;

/// <summary>
/// End-to-End tests for the User Statistics page using Playwright.
/// Tests desktop (1920x1080) and mobile (390x844) viewports on Chromium.
/// </summary>
[Collection("Sequential")]
public class UserStatsPageE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string UserStatsUrl = "http://localhost:5208/user-stats";
    private const int PageLoadTimeout = 10000;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-web-security" }
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task UserStatsPage_ShouldLoad_Successfully_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var title = await page.TitleAsync();
            title.Should().Contain("PoFastType");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task UserStatsPage_ShouldLoad_Successfully_Mobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var content = await page.ContentAsync();
            content.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task UserStatsPage_ShouldBe_Responsive_Mobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var bodyWidth = await page.EvaluateAsync<int>("() => document.body.scrollWidth");
            bodyWidth.Should().BeLessThanOrEqualTo(390);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task UserStatsPage_ShouldLoad_WithoutErrors_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
                consoleErrors.Add(msg.Text);
        };

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            consoleErrors.Should().BeEmpty("page should load without console errors");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task UserStatsPage_ShouldHave_Navigation_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var navLinks = await page.Locator("nav a, .navbar a").CountAsync();
            navLinks.Should().BeGreaterThan(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task UserStatsPage_ShouldDisplay_Stats_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(UserStatsUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Look for statistics-related content
            var content = await page.ContentAsync();
            var hasStats = content.Contains("WPM", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("Accuracy", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("Games", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("Statistics", StringComparison.OrdinalIgnoreCase);

            hasStats.Should().BeTrue("user stats page should display statistics");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
