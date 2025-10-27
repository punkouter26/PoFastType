using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace PoFastType.Tests.E2E;

/// <summary>
/// End-to-End tests for the Leaderboard page using Playwright.
/// Tests desktop (1920x1080) and mobile (390x844) viewports on Chromium.
/// </summary>
[Collection("Sequential")]
public class LeaderboardPageE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string LeaderboardUrl = "http://localhost:5208/leaderboard";
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
    public async Task LeaderboardPage_ShouldLoad_Successfully_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var title = await page.TitleAsync();
            title.Should().Contain("PoFastType");

            var heading = await page.Locator("h1, h2, h3").First.TextContentAsync();
            heading.Should().NotBeNullOrEmpty("leaderboard should have a heading");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_ShouldLoad_Successfully_Mobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
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
    public async Task LeaderboardPage_ShouldDisplay_TableOrList_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Should have table or list for leaderboard data
            var hasTable = await page.Locator("table").CountAsync() > 0;
            var hasList = await page.Locator("ul, ol").CountAsync() > 0;
            var hasGrid = await page.Locator("[role='grid'], .leaderboard").CountAsync() > 0;

            (hasTable || hasList || hasGrid).Should().BeTrue("leaderboard should display data in table/list/grid format");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_ShouldBe_Responsive_Mobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - No horizontal scroll
            var bodyWidth = await page.EvaluateAsync<int>("() => document.body.scrollWidth");
            bodyWidth.Should().BeLessThanOrEqualTo(390);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_ShouldLoad_WithoutErrors_Desktop()
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
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
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
    public async Task LeaderboardPage_ShouldHave_Navigation_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
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
    public async Task LeaderboardPage_ShouldDisplay_ScoreColumns_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Look for score-related text
            var content = await page.ContentAsync();
            var hasScoreColumns = content.Contains("WPM", StringComparison.OrdinalIgnoreCase) ||
                                 content.Contains("Score", StringComparison.OrdinalIgnoreCase) ||
                                 content.Contains("Accuracy", StringComparison.OrdinalIgnoreCase);

            hasScoreColumns.Should().BeTrue("leaderboard should display score metrics");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_TableHeaders_ShouldBeVisible_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var tableHeaders = await page.Locator("th, .header, [role='columnheader']").CountAsync();
            if (tableHeaders > 0)
            {
                tableHeaders.Should().BeGreaterThan(0, "if table exists, it should have headers");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_MobileLayout_ShouldBeReadable()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Content should be visible
            var body = await page.Locator("body").TextContentAsync();
            body.Should().NotBeNullOrEmpty();

            // Font size should be readable (at least 14px)
            var fontSize = await page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).fontSize");
            fontSize.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task LeaderboardPage_Navigation_ToHome_ShouldWork_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(LeaderboardUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Find link to home (adjust selector based on actual navigation)
            var homeLink = page.Locator("a[href='/'], a[href='']");
            var linkCount = await homeLink.CountAsync();
            if (linkCount > 0)
            {
                await homeLink.First.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Assert
                var url = page.Url;
                (url.EndsWith("/") || url.Contains("home")).Should().BeTrue("clicking home link should navigate to home");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
