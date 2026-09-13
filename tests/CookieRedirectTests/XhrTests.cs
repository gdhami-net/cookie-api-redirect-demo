using Xunit;

namespace CookieRedirectTests;

/// <summary>
/// The exception the old behaviour had. A request carrying
/// X-Requested-With: XMLHttpRequest never got a redirect, and the question
/// worth answering is whether that is still true on .NET 10 for the shapes
/// that do still redirect.
/// </summary>
public sealed class XhrTests(AppFixture app) : IClassFixture<AppFixture>
{
    public static IEnumerable<object[]> Rows => Shapes.All;

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task The_XHR_header_turns_every_shape_into_401_and_403(string method, string path)
    {
        using var anonymous = app.AnonymousClient();
        var signedIn = await app.SignedInClient();

        var challenge = await anonymous.SendAsync(Xhr(method, path));
        var forbid = await signedIn.SendAsync(Xhr(method, path));

        Assert.Equal(401, (int)challenge.StatusCode);
        Assert.Equal(403, (int)forbid.StatusCode);
    }

    /// <summary>
    /// The header beats an explicit AllowCookieRedirect too, so the XHR check
    /// runs before the metadata is consulted.
    /// </summary>
    [Fact]
    public async Task The_XHR_header_beats_an_explicit_allow_mark()
    {
        using var client = app.AnonymousClient();

#if NET10_0_OR_GREATER
        var withHeader = await client.SendAsync(Xhr("GET", "/min/typed-results-allowed"));
        var without = await client.GetAsync("/min/typed-results-allowed");

        Assert.Equal(401, (int)withHeader.StatusCode);
        Assert.Equal(302, (int)without.StatusCode);
#else
        // The allow mark does not exist on .NET 9; the header still wins on the
        // shape that stands in for it.
        var withHeader = await client.SendAsync(Xhr("GET", "/min/typed-results"));
        var without = await client.GetAsync("/min/typed-results");

        Assert.Equal(401, (int)withHeader.StatusCode);
        Assert.Equal(302, (int)without.StatusCode);
#endif
    }

    /// <summary>
    /// The query string spelling the handler also accepts.
    /// </summary>
    [Fact]
    public async Task The_same_value_in_the_query_string_also_counts()
    {
        using var client = app.AnonymousClient();

        var response = await client.GetAsync("/min/string?X-Requested-With=XMLHttpRequest");

        Assert.Equal(401, (int)response.StatusCode);
    }

    private static HttpRequestMessage Xhr(string method, string path)
    {
        var request = EndpointShapeTests.Request(method, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return request;
    }
}
