using Xunit;

namespace CookieRedirectTests;

/// <summary>
/// The documented way back. Overriding OnRedirectToLogin and
/// OnRedirectToAccessDenied replaces the handler's whole decision, so every
/// shape redirects again — including the ones that never redirected, because
/// the XHR check lives in the same place.
/// </summary>
public sealed class RestoreRedirectsTests(AppFixture app) : IClassFixture<AppFixture>
{
    public static IEnumerable<object[]> Rows => Shapes.PortableOnly;

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Overriding_the_two_events_puts_every_shape_back_on_302(string method, string path)
    {
        var factory = app.WithRedirectsRestored();
        using var anonymous = AppFixture.AnonymousOn(factory);
        var signedIn = await AppFixture.SignInOn(factory);

        var challenge = await anonymous.SendAsync(EndpointShapeTests.Request(method, path));
        var forbid = await signedIn.SendAsync(EndpointShapeTests.Request(method, path));

        Assert.Equal(302, (int)challenge.StatusCode);
        Assert.Equal(302, (int)forbid.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task The_override_also_takes_the_XHR_exception_away(string method, string path)
    {
        var factory = app.WithRedirectsRestored();
        using var client = AppFixture.AnonymousOn(factory);

        var request = EndpointShapeTests.Request(method, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.SendAsync(request);

        Assert.Equal(302, (int)response.StatusCode);
    }
}
