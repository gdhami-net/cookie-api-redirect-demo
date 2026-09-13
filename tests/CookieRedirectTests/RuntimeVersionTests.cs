using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CookieRedirectTests;

/// <summary>
/// Prints the runtime and assembly versions this suite measured, so a reader
/// comparing numbers with the post can see which framework produced them.
/// </summary>
public sealed class RuntimeVersionTests(ITestOutputHelper output)
{
    [Fact]
    public void Report_the_versions_this_suite_is_running_against()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"RuntimeInformation.FrameworkDescription  {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"Environment.Version                      {Environment.Version}");
        sb.AppendLine();

        foreach (var type in new[]
                 {
                     typeof(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationHandler),
                     typeof(Microsoft.AspNetCore.Http.Endpoint),
                     typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute),
                 })
        {
            var assembly = type.Assembly;
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
            sb.AppendLine($"{assembly.GetName().Name}");
            sb.AppendLine($"    informational version  {informational}");
            sb.AppendLine($"    location               {assembly.Location}");
        }

        output.WriteLine(sb.ToString());
        Assert.NotEmpty(RuntimeInformation.FrameworkDescription);
    }
}
