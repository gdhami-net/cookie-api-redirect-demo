#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Http.Metadata;

namespace CookieRedirectApp;

/// <summary>
/// ASP.NET Core 10.0 ships AllowCookieRedirectAttribute but no attribute for
/// the other direction, and the class behind DisableCookieRedirect() is
/// internal. The interface is public, so an MVC controller or action that wants
/// the mark declares it like this. Endpoint routing copies attributes on the
/// controller and the action into the endpoint's metadata, which is where the
/// cookie handler looks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableCookieRedirectAttribute : Attribute, IDisableCookieRedirectMetadata;
#endif
