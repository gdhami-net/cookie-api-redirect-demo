using System.Security.Claims;
using CookieRedirectApp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Denied";
    });

builder.Services.AddAuthorization(o =>
    o.AddPolicy("staff", p => p.RequireClaim("role", "staff")));

builder.Services.AddControllers();
builder.Services.AddRazorPages(o => o.Conventions.AuthorizePage("/Secret", "staff"));
builder.Services.AddSignalR();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Anonymous plumbing: the two pages the cookie handler points at, and a
// sign-in endpoint so a test can hold a real auth ticket without a browser.
// ---------------------------------------------------------------------------

app.MapGet("/Account/Login", () => Results.Content("<h1>Login</h1>", "text/html"));
app.MapGet("/Account/Denied", () => Results.Content("<h1>Denied</h1>", "text/html"));

app.MapGet("/sign-in", async (HttpContext http, string? role) =>
{
    var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
    if (!string.IsNullOrEmpty(role))
    {
        claims.Add(new Claim("role", role));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Content("signed in", "text/plain");
});

// ---------------------------------------------------------------------------
// The shapes under test. Every one sits behind the same "staff" policy, so an
// anonymous request is a challenge and a signed-in request without the claim
// is a forbid. Nothing else differs between them.
// ---------------------------------------------------------------------------

var thing = new Thing("widget", 3);

app.MapGet("/min/typed-results", () => TypedResults.Ok(thing)).RequireAuthorization("staff");
app.MapGet("/min/results-ok", () => Results.Ok(thing)).RequireAuthorization("staff");
app.MapGet("/min/plain-object", () => thing).RequireAuthorization("staff");
app.MapGet("/min/string", () => "widget").RequireAuthorization("staff");
app.MapPost("/min/reads-json", (Thing body) => body.Name).RequireAuthorization("staff");

app.MapHub<PingHub>("/hub/ping").RequireAuthorization("staff");

app.MapRazorPages();
app.MapControllers();

#if NET10_0_OR_GREATER
// The two explicit marks, both from
// Microsoft.AspNetCore.Builder.CookieRedirectEndpointConventionBuilderExtensions.
// DisableCookieRedirect() puts IDisableCookieRedirectMetadata on an endpoint
// that would otherwise redirect. AllowCookieRedirect() puts
// IAllowCookieRedirectMetadata on one that would otherwise answer 401, and
// wins whatever the order.
app.MapGet("/min/string-disabled", () => "widget")
   .RequireAuthorization("staff")
   .DisableCookieRedirect();

app.MapGet("/min/typed-results-allowed", () => TypedResults.Ok(thing))
   .RequireAuthorization("staff")
   .AllowCookieRedirect();
#endif

// ---------------------------------------------------------------------------
// The inspector: every routed endpoint, whether it carries either mark, and
// which metadata object supplies the disable mark. This is the check you can
// run against your own app.
// ---------------------------------------------------------------------------

app.MapGet("/__endpoints", (EndpointDataSource endpoints) =>
    endpoints.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e =>
        {
#if NET10_0_OR_GREATER
            var disable = e.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IDisableCookieRedirectMetadata>();
            var allow = e.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IAllowCookieRedirectMetadata>();
            return new EndpointMark(
                e.RoutePattern.RawText ?? "",
                disable is not null,
                allow is not null,
                disable?.GetType().FullName ?? "");
#else
            return new EndpointMark(e.RoutePattern.RawText ?? "", false, false, "");
#endif
        })
        .OrderBy(e => e.Route, StringComparer.Ordinal));

app.Run();

public sealed record Thing(string Name, int Count);

public sealed record EndpointMark(string Route, bool DisablesRedirect, bool AllowsRedirect, string MarkedBy);

public partial class Program;
