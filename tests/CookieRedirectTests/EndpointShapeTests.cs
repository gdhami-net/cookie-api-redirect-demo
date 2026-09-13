using System.Net;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CookieRedirectTests;

/// <summary>
/// The table the post prints. One anonymous request and one forbidden request
/// per endpoint shape, with the status code and the Location header recorded
/// rather than assumed.
/// </summary>
public sealed class EndpointShapeTests(AppFixture app, ITestOutputHelper output) : IClassFixture<AppFixture>
{
    public static IEnumerable<object[]> Rows => Shapes.All;

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task An_anonymous_request_gets_the_status_the_table_says(string method, string path)
    {
        var shape = Shapes.ByPath(path);
        using var client = app.AnonymousClient();

        var response = await client.SendAsync(Request(method, path));

        Assert.Equal(shape.AnonymousStatus, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task A_forbidden_request_gets_the_status_the_table_says(string method, string path)
    {
        var shape = Shapes.ByPath(path);
        var client = await app.SignedInClient();

        var response = await client.SendAsync(Request(method, path));

        Assert.Equal(shape.ForbiddenStatus, (int)response.StatusCode);
    }

    /// <summary>
    /// The detail that trips people up: the 401 and the 403 still carry the
    /// Location header they would have redirected to. Only the status line
    /// differs between the two sides of the table.
    /// </summary>
    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Every_answer_carries_the_login_or_denied_Location(string method, string path)
    {
        using var anonymous = app.AnonymousClient();
        var signedIn = await app.SignedInClient();

        var challenge = await anonymous.SendAsync(Request(method, path));
        var forbid = await signedIn.SendAsync(Request(method, path));

        Assert.Equal(
            $"http://localhost/Account/Login?ReturnUrl={Uri.EscapeDataString(path)}",
            challenge.Headers.Location?.ToString());
        Assert.Equal(
            $"http://localhost/Account/Denied?ReturnUrl={Uri.EscapeDataString(path)}",
            forbid.Headers.Location?.ToString());
    }

    /// <summary>
    /// The pair the post opens with, asserted on its own so a change to it
    /// cannot hide inside the table.
    /// </summary>
    [Fact]
    public async Task TypedResults_and_Results_answer_the_same_anonymous_request_differently()
    {
        using var client = app.AnonymousClient();

        var typed = await client.GetAsync("/min/typed-results");
        var untyped = await client.GetAsync("/min/results-ok");

#if NET10_0_OR_GREATER
        Assert.Equal(HttpStatusCode.Unauthorized, typed.StatusCode);
        Assert.Equal(HttpStatusCode.Found, untyped.StatusCode);
#else
        Assert.Equal(HttpStatusCode.Found, typed.StatusCode);
        Assert.Equal(HttpStatusCode.Found, untyped.StatusCode);
#endif
    }

    /// <summary>
    /// Signing in with the claim the policy wants has to get past authorization,
    /// otherwise the whole table is measuring a broken app rather than the
    /// cookie handler. (The SignalR endpoint answers 400 to a bare GET with no
    /// transport negotiation, which is still past the point that matters here.)
    /// </summary>
    [Theory]
    [MemberData(nameof(Rows))]
    public async Task A_staff_request_is_neither_challenged_nor_forbidden(string method, string path)
    {
        var client = await app.SignedInClient("staff");

        var response = await client.SendAsync(Request(method, path));

        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain((int)response.StatusCode, new[] { 302, 401, 403 });
    }

    [Fact]
    public async Task Print_the_table()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"target framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        sb.AppendLine();
        sb.AppendLine($"{"endpoint shape",-58}{"anon",-8}{"forbidden",-10}");

        using var anonymous = app.AnonymousClient();
        var signedIn = await app.SignedInClient();

        foreach (var shape in Shapes.Everything)
        {
            var challenge = await anonymous.SendAsync(Request(shape.Method, shape.Path));
            var forbid = await signedIn.SendAsync(Request(shape.Method, shape.Path));
            sb.AppendLine($"{shape.Description,-58}{(int)challenge.StatusCode,-8}{(int)forbid.StatusCode,-10}");
        }

        output.WriteLine(sb.ToString());
    }

    internal static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = new StringContent(
                """{"name":"widget","count":3}""", Encoding.UTF8, "application/json");
        }

        return request;
    }
}
