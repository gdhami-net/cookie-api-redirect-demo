using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CookieRedirectTests;

/// <summary>
/// The status codes above are the symptom. This is the cause: a piece of
/// endpoint metadata that ASP.NET Core adds for you on some shapes and not on
/// others.
/// </summary>
public sealed class MetadataTests(AppFixture app, ITestOutputHelper output) : IClassFixture<AppFixture>
{
    /// <summary>
    /// The mark and the answer agree on every shape, which is the whole claim:
    /// the status code is decided by metadata, not by the URL or the verb.
    /// </summary>
    [Fact]
    public async Task The_mark_predicts_the_status_code_on_every_shape()
    {
        var marks = (await app.ReadEndpointMarks())
            .ToDictionary(m => m.Route.StartsWith('/') ? m.Route : "/" + m.Route);

        foreach (var shape in Shapes.Everything)
        {
            var mark = marks[shape.Path];
            var redirectsExpected = !mark.DisablesRedirect || mark.AllowsRedirect;

            Assert.Equal(redirectsExpected, shape.Redirects);
        }
    }

#if NET10_0_OR_GREATER
    /// <summary>
    /// The interface the breaking-change notice names (IApiEndpointMetadata)
    /// was a preview spelling. These are the types that actually shipped.
    /// </summary>
    [Fact]
    public void The_shipped_type_names()
    {
        var abstractions = typeof(Microsoft.AspNetCore.Http.Endpoint).Assembly;
        var extensions = typeof(Microsoft.AspNetCore.Http.EndpointDescriptionAttribute).Assembly;

        Assert.NotNull(abstractions.GetType("Microsoft.AspNetCore.Http.Metadata.IDisableCookieRedirectMetadata"));
        Assert.NotNull(abstractions.GetType("Microsoft.AspNetCore.Http.Metadata.IAllowCookieRedirectMetadata"));
        Assert.Null(abstractions.GetType("Microsoft.AspNetCore.Http.Metadata.IApiEndpointMetadata"));

        // The concrete class exists but is not public, so DisableCookieRedirect()
        // and a hand-written attribute are the two ways to apply the mark.
        var concrete = abstractions.GetType("Microsoft.AspNetCore.Http.Metadata.DisableCookieRedirectMetadata");
        Assert.NotNull(concrete);
        Assert.False(concrete!.IsPublic);

        // The allow side does ship an attribute. The disable side does not.
        Assert.NotNull(extensions.GetType("Microsoft.AspNetCore.Http.AllowCookieRedirectAttribute"));
        Assert.DoesNotContain(extensions.GetExportedTypes(),
            t => t.Name == "DisableCookieRedirectAttribute");
    }

    /// <summary>
    /// [ApiController] is not merely correlated with the mark. The attribute
    /// instance IS the metadata the cookie handler finds.
    /// </summary>
    [Fact]
    public void ApiControllerAttribute_implements_the_marker_interface_itself()
    {
        Assert.True(typeof(Microsoft.AspNetCore.Http.Metadata.IDisableCookieRedirectMetadata)
            .IsAssignableFrom(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute)));
    }

#else
    /// <summary>
    /// Nothing on .NET 9 carries either mark, because neither interface exists.
    /// </summary>
    [Fact]
    public async Task Nothing_is_marked_on_dotnet_9()
    {
        var marks = await app.ReadEndpointMarks();

        Assert.NotEmpty(marks);
        Assert.All(marks, m => Assert.False(m.DisablesRedirect || m.AllowsRedirect));
    }
#endif

    /// <summary>
    /// Which object supplies the mark on each endpoint, printed rather than
    /// asserted, because the concrete types are internal and may be renamed.
    /// </summary>
    [Fact]
    public async Task Print_what_marks_each_endpoint()
    {
        var sb = new StringBuilder();
        foreach (var mark in await app.ReadEndpointMarks())
        {
            sb.AppendLine($"{mark.Route,-30} disable={mark.DisablesRedirect,-6} allow={mark.AllowsRedirect,-6} {mark.MarkedBy}");
        }

        output.WriteLine(sb.ToString());
    }
}
