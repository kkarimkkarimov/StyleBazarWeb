using System.Net;
using FluentAssertions;
using Xunit;

namespace Stylebazar.Tests.Integration;

/// <summary>
/// End-to-end smoke tests over the real middleware pipeline: health, the public
/// home page, the security-header middleware, and cookie-driven localization.
/// Marked [Trait("Category","Integration")] so a DB-less run can filter them out:
///     dotnet test --filter "Category!=Integration"
/// </summary>
[Trait("Category", "Integration")]
public sealed class PipelineSmokeTests : IClassFixture<StyleBazarMvcFactory>
{
    private readonly StyleBazarMvcFactory _factory;

    public PipelineSmokeTests(StyleBazarMvcFactory factory)
    {
        _factory = factory;
        _factory.DatabaseReachable.Should().BeTrue(
            $"integration tests need SQL Server at '{StyleBazarMvcFactory.ConnectionString}'. " +
            "Start it (or set TEST_DB_CONNECTION), or run unit-only: " +
            "dotnet test --filter \"Category!=Integration\"");
    }

    [Fact]
    public async Task Health_endpoint_returns_200_Healthy()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/health");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Home_page_returns_200()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/");

        res.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).Should().BeTrue();
        nosniff!.Should().Contain("nosniff");
        res.Headers.Contains("Content-Security-Policy").Should().BeTrue();
        res.Headers.Contains("X-Frame-Options").Should().BeTrue();
    }

    [Fact]
    public async Task Culture_cookie_switches_html_lang_to_ru()
    {
        var client = _factory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/");
        // CookieRequestCultureProvider format: c=<culture>|uic=<ui-culture>
        req.Headers.Add("Cookie", ".AspNetCore.Culture=c=ru|uic=ru");
        var res = await client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res.Content.ReadAsStringAsync()).Should().Contain("lang=\"ru\"");
    }
}
