using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieRedirectApp;

/// <summary>
/// The API-shaped controller. Nothing here is special except [ApiController],
/// which is itself the metadata the cookie handler reads.
/// </summary>
[ApiController]
[Route("mvc/attributed")]
[Authorize(Policy = "staff")]
public sealed class AttributedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Thing("widget", 3));
}

/// <summary>
/// The same action, the same JSON body, the same base class — without
/// [ApiController]. The routing attributes are all that keep it mapped.
/// </summary>
[Route("mvc/plain")]
[Authorize(Policy = "staff")]
public sealed class PlainController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Thing("widget", 3));
}

#if NET10_0_OR_GREATER

/// <summary>
/// [ApiController] plus the shipped [AllowCookieRedirect], which puts the
/// endpoint back on the redirecting side.
/// </summary>
[ApiController]
[Route("mvc/attributed-allowed")]
[Authorize(Policy = "staff")]
[Microsoft.AspNetCore.Http.AllowCookieRedirect]
public sealed class AttributedAllowedController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Thing("widget", 3));
}

/// <summary>
/// No [ApiController], so no automatic mark — and there is no shipped attribute
/// for the disable side, so the endpoint carries a hand-written one.
/// </summary>
[Route("mvc/plain-disabled")]
[Authorize(Policy = "staff")]
[DisableCookieRedirect]
public sealed class PlainDisabledController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Thing("widget", 3));
}

#endif
