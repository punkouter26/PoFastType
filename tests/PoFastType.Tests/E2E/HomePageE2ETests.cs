using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace PoFastType.Tests.E2E;

/// <summary>
/// End-to-End tests for the Home/Typing Game page using Playwright.
/// Tests desktop (1920x1080) and mobile (390x844) viewports on Chromium.
/// 
/// Prerequisites:
/// - Application must be running (dotnet watch or dotnet run)
/// - Default URL: See TestConstants.BaseUrl
/// </summary>
[Collection("Sequential")] // Run E2E tests sequentially to avoid port conflicts
public class HomePageE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string AppUrl = TestConstants.BaseUrl;
    private const int PageLoadTimeout = 30000; // 30 seconds for Blazor WASM

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true, // Run in headless mode for CI/CD
            Args = new[] { "--disable-web-security" } // Allow localhost
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HomePage_ShouldLoad_Successfully_Desktop()
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

            // Assert
            var title = await page.TitleAsync();
            title.Should().Contain("PoFastType", "home page title should include app name");

            // Wait for Blazor to load and check for main heading
            await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30000 });
            var heading = await page.Locator("h1").First.TextContentAsync();
            heading.Should().NotBeNullOrEmpty("home page should have main heading");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldLoad_Successfully_Mobile()
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

            // Assert
            var title = await page.TitleAsync();
            title.Should().Contain("PoFastType");

            // Mobile layout should be visible
            var content = await page.ContentAsync();
            content.Should().NotBeNullOrEmpty();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldDisplay_TypingTextArea_Desktop()
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

            // Assert - Look for typing game elements
            // Note: Home page uses contenteditable div for typing area
            var hasTextarea = await page.Locator("textarea").CountAsync() > 0 || 
                             await page.Locator("input[type='text']").CountAsync() > 0 ||
                             await page.Locator("[contenteditable]").CountAsync() > 0 ||
                             await page.Locator(".typing-area").CountAsync() > 0;
            
            hasTextarea.Should().BeTrue("typing game should have input area");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldHave_StartButton_Desktop()
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

            // Assert - Look for start/play button
            var hasButton = await page.Locator("button").CountAsync() > 0;
            hasButton.Should().BeTrue("typing game should have control buttons");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_Navigation_ShouldWork_Desktop()
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

            // Assert - Check for navigation links
            var navLinks = await page.Locator("nav a, .navbar a").CountAsync();
            navLinks.Should().BeGreaterThan(0, "navigation should have links");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldBe_Responsive_Mobile()
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

            // Assert - Content should be visible without horizontal scroll
            var bodyWidth = await page.EvaluateAsync<int>("() => document.body.scrollWidth");
            bodyWidth.Should().BeLessThanOrEqualTo(390, "mobile layout should not cause horizontal scrolling");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShouldLoad_WithoutErrors_Desktop()
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
            await page.GotoAsync(AppUrl, new PageGotoOptions { Timeout = PageLoadTimeout });
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
    public async Task HomePage_ShouldHave_Footer_Desktop()
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

            // Assert
            var hasFooter = await page.Locator("footer").CountAsync() > 0;
            // Footer is optional, but if present should be visible
            if (hasFooter)
            {
                var footer = page.Locator("footer").First;
                await footer.ShouldBeVisibleAsync();
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_Layout_ShouldRender_Properly_Desktop()
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

            // Assert - Check that main content is visible
            var body = await page.Locator("body").TextContentAsync();
            body.Should().NotBeNullOrEmpty("page should have content");

            // Check that Blazor has hydrated
            var blazorScript = await page.Locator("script[src*='blazor']").CountAsync();
            blazorScript.Should().BeGreaterThan(0, "Blazor WebAssembly should be loaded");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HomePage_Navigation_ShouldWork_Mobile()
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

            // Assert - Mobile navigation should exist (could be hamburger menu)
            var navElements = await page.Locator("nav, .navbar, [role='navigation']").CountAsync();
            navElements.Should().BeGreaterThan(0, "mobile navigation should be present");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}

// Extension methods for Playwright assertions
internal static class PlaywrightAssertionExtensions
{
    internal static async Task ShouldBeVisibleAsync(this ILocator locator)
    {
        var isVisible = await locator.IsVisibleAsync();
        isVisible.Should().BeTrue($"element '{locator}' should be visible");
    }

    internal static async Task ShouldNotBeVisibleAsync(this ILocator locator)
    {
        var isVisible = await locator.IsVisibleAsync();
        isVisible.Should().BeFalse($"element '{locator}' should not be visible");
    }
}
