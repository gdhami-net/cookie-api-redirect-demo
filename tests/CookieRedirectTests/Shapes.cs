namespace CookieRedirectTests;

/// <summary>
/// One row per endpoint shape: how to call it, and the status codes the running
/// framework actually answers with. The expectations are compiled per target
/// framework, so the same source file is the .NET 9 "before" table and the
/// .NET 10 "after" table.
/// </summary>
public sealed record Shape(
    string Method,
    string Path,
    string Description,
    int AnonymousStatus,
    int ForbiddenStatus)
{
    public bool Redirects => AnonymousStatus == 302;
}

public static class Shapes
{
#if NET10_0_OR_GREATER
    private const int Api = 401;
    private const int ApiForbidden = 403;
#else
    private const int Api = 302;
    private const int ApiForbidden = 302;
#endif

    /// <summary>
    /// The shapes that exist on both .NET 9 and .NET 10.
    /// </summary>
    public static readonly Shape[] Portable =
    [
        new("GET", "/mvc/attributed", "controller with [ApiController]", Api, ApiForbidden),
        new("GET", "/mvc/plain", "controller without [ApiController], returning JSON", 302, 302),
        new("GET", "/min/typed-results", "minimal API returning TypedResults.Ok(obj)", Api, ApiForbidden),
        new("GET", "/min/results-ok", "minimal API returning Results.Ok(obj) as IResult", 302, 302),
        new("GET", "/min/plain-object", "minimal API returning a plain object", Api, ApiForbidden),
        new("GET", "/min/string", "minimal API returning a string", 302, 302),
        new("POST", "/min/reads-json", "minimal API reading a JSON body, returning a string", Api, ApiForbidden),
        new("GET", "/Secret", "Razor Page", 302, 302),
        new("GET", "/hub/ping", "SignalR hub endpoint", Api, ApiForbidden),
    ];

    /// <summary>
    /// The shapes that only compile on .NET 10, where the two marks exist.
    /// </summary>
    public static readonly Shape[] ExplicitlyMarked =
#if NET10_0_OR_GREATER
    [
        new("GET", "/min/string-disabled", "string endpoint + DisableCookieRedirect()", 401, 403),
        new("GET", "/min/typed-results-allowed", "TypedResults endpoint + AllowCookieRedirect()", 302, 302),
        new("GET", "/mvc/attributed-allowed", "[ApiController] + [AllowCookieRedirect]", 302, 302),
        new("GET", "/mvc/plain-disabled", "plain controller + a hand-written disable attribute", 401, 403),
    ];
#else
    [];
#endif

    public static Shape[] Everything => [.. Portable, .. ExplicitlyMarked];

    // xUnit serializes theory data, so the rows go over as primitives and the
    // test looks the Shape back up by path.
    public static IEnumerable<object[]> All =>
        Everything.Select(s => new object[] { s.Method, s.Path });

    public static IEnumerable<object[]> PortableOnly =>
        Portable.Select(s => new object[] { s.Method, s.Path });

    public static Shape ByPath(string path) =>
        Everything.Single(s => s.Path == path);
}
