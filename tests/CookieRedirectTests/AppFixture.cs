using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CookieRedirectTests;

/// <summary>
/// One in-memory server for the whole suite. Every client it hands out has
/// AllowAutoRedirect off, because a followed redirect is exactly the evidence
/// this suite exists to record.
/// </summary>
public sealed class AppFixture : WebApplicationFactory<Program>
{
    public HttpClient AnonymousClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false,
    });

    /// <summary>
    /// A client carrying a real cookie-auth ticket. Pass no role to get an
    /// authenticated user the "staff" policy refuses, which is what produces a
    /// forbid rather than a challenge.
    /// </summary>
    public Task<HttpClient> SignedInClient(string? role = null) => SignInOn(this, role);

    /// <summary>
    /// The same app with the two event handlers the breaking-change notice
    /// recommends for restoring the old redirects everywhere.
    /// </summary>
    public WebApplicationFactory<Program> WithRedirectsRestored() =>
        WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.PostConfigure<CookieAuthenticationOptions>(
                CookieAuthenticationDefaults.AuthenticationScheme,
                o =>
                {
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            context.Response.Redirect(context.RedirectUri);
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            context.Response.Redirect(context.RedirectUri);
                            return Task.CompletedTask;
                        },
                    };
                })));

    public static async Task<HttpClient> SignInOn(WebApplicationFactory<Program> factory, string? role = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync(role is null ? "/sign-in" : $"/sign-in?role={role}");
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static HttpClient AnonymousOn(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

    public async Task<IReadOnlyList<EndpointMark>> ReadEndpointMarks()
    {
        using var client = AnonymousClient();
        return await client.GetFromJsonAsync<List<EndpointMark>>("/__endpoints")
               ?? throw new InvalidOperationException("/__endpoints returned null");
    }
}
