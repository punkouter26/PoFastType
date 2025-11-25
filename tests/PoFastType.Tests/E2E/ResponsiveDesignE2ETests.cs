using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace PoFastType.Tests.E2E;

/// <summary>
/// End-to-End tests for Responsive Design across all pages.
/// Tests desktop (1920x1080) and mobile (390x844) viewports on Chromium.
/// Validates navigation, layout, and mobile menu functionality.
/// </summary>
[Collection("Sequential")]
public class ResponsiveDesignE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string AppUrl = TestConstants.BaseUrl;
    private const int PageLoadTimeout = 10000;

    private readonly string[] _testPages = new[]
    {
        "/",
        "/leaderboard",
        "/stats",
        "/diag"
    };

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

    [Theory]
    [InlineData("/")]
    [InlineData("/leaderboard")]
    [InlineData("/stats")]
    [InlineData("/diag")]
    public async Task AllPages_ShouldLoad_OnDesktop(string pagePath)
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync($"{AppUrl}{pagePath}", new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var title = await page.TitleAsync();
            title.Should().Contain("PoFastType", $"page {pagePath} should load with correct title");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/leaderboard")]
    [InlineData("/stats")]
    [InlineData("/diag")]
    public async Task AllPages_ShouldLoad_OnMobile(string pagePath)
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync($"{AppUrl}{pagePath}", new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var content = await page.ContentAsync();
            content.Should().NotBeNullOrEmpty($"page {pagePath} should render content on mobile");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/leaderboard")]
    [InlineData("/stats")]
    public async Task AllPages_ShouldNotHaveHorizontalScroll_OnMobile(string pagePath)
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync($"{AppUrl}{pagePath}", new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var bodyWidth = await page.EvaluateAsync<int>("() => document.body.scrollWidth");
            bodyWidth.Should().BeLessThanOrEqualTo(390, $"page {pagePath} should not cause horizontal scrolling on mobile");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Navigation_ShouldWork_OnDesktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            // Act
            await page.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Check navigation exists
            var navLinks = await page.Locator("nav a, .navbar a").CountAsync();
            navLinks.Should().BeGreaterThan(0, "desktop should have navigation links");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Navigation_ShouldWork_OnMobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Check mobile navigation exists
            var navElements = await page.Locator("nav, .navbar, [role='navigation']").CountAsync();
            navElements.Should().BeGreaterThan(0, "mobile should have navigation");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Layout_ShouldAdapt_BetweenDesktopAndMobile()
    {
        // Test desktop first
        var desktopPage = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        await desktopPage.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
        await desktopPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var desktopWidth = await desktopPage.EvaluateAsync<int>("() => document.body.clientWidth");

        await desktopPage.CloseAsync();

        // Test mobile
        var mobilePage = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        await mobilePage.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
        await mobilePage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var mobileWidth = await mobilePage.EvaluateAsync<int>("() => document.body.clientWidth");

        await mobilePage.CloseAsync();

        // Assert - Layouts should be different
        desktopWidth.Should().BeGreaterThan(mobileWidth, "desktop layout should use more width than mobile");
    }

    [Fact]
    public async Task BlazorWebAssembly_ShouldLoad_OnAllPages_Desktop()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            foreach (var pagePath in _testPages)
            {
                // Act
                await page.GotoAsync($"{AppUrl}{pagePath}", new PageGotoOptions { Timeout = PageLoadTimeout });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Assert
                var blazorScript = await page.Locator("script[src*='blazor']").CountAsync();
                blazorScript.Should().BeGreaterThan(0, $"Blazor should be loaded on page {pagePath}");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/leaderboard")]
    [InlineData("/stats")]
    public async Task Pages_ShouldLoad_WithoutConsoleErrors_Desktop(string pagePath)
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
            await page.GotoAsync($"{AppUrl}{pagePath}", new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            consoleErrors.Should().BeEmpty($"page {pagePath} should load without console errors");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task FontSize_ShouldBeReadable_OnMobile()
    {
        // Arrange
        var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });

        try
        {
            // Act
            await page.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - Check computed font size
            var fontSize = await page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).fontSize");
            fontSize.Should().NotBeNullOrEmpty("font size should be set on mobile");
            
            // Parse fontSize (e.g., "16px") and verify it's at least 14px
            if (fontSize.EndsWith("px"))
            {
                var sizeValue = double.Parse(fontSize.Replace("px", ""));
                sizeValue.Should().BeGreaterThanOrEqualTo(14, "mobile font size should be at least 14px for readability");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
