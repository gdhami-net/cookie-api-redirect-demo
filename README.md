# cookie-api-redirect-demo

Companion to the post **"The endpoint that still redirects to login"**.

In .NET 10, cookie authentication stopped sending unauthenticated and
unauthorized requests to the login and access-denied pages for endpoints it
recognises as API endpoints, and answers 401 and 403 instead. Which endpoints
count is decided by endpoint metadata that ASP.NET Core adds automatically for
some shapes and not for others. Two endpoints that look alike in a code review
can therefore answer the same anonymous request differently.

This repository measures which shapes land on which side, on .NET 10 and on
.NET 9, and asserts every number the post prints.

## What it proves

One ASP.NET Core app (`src/CookieRedirectApp`) with cookie authentication, a
`LoginPath` of `/Account/Login`, an `AccessDeniedPath` of `/Account/Denied`, and
one endpoint per shape. Every endpoint sits behind the same `staff` policy, so
an anonymous request produces a challenge and a signed-in request without the
claim produces a forbid. Nothing else differs between them.

`tests/CookieRedirectTests` drives the app through
`Microsoft.AspNetCore.Mvc.Testing`'s in-memory server with `AllowAutoRedirect`
turned off, and asserts the status code and the `Location` header for each. The
test project multi-targets `net9.0` and `net10.0`, and the expected status codes
are compiled per target framework, so the same source file is the "before" table
and the "after" table.

Measured on .NET 10.0.5:

| endpoint shape | anonymous | forbidden |
| --- | --- | --- |
| controller with `[ApiController]` | 401 | 403 |
| controller without `[ApiController]`, returning JSON | 302 | 302 |
| minimal API returning `TypedResults.Ok(obj)` | 401 | 403 |
| minimal API returning `Results.Ok(obj)` as `IResult` | 302 | 302 |
| minimal API returning a plain object | 401 | 403 |
| minimal API returning a string | 302 | 302 |
| minimal API reading a JSON body, returning a string | 401 | 403 |
| Razor Page | 302 | 302 |
| SignalR hub endpoint | 401 | 403 |
| string endpoint + `DisableCookieRedirect()` | 401 | 403 |
| `TypedResults` endpoint + `AllowCookieRedirect()` | 302 | 302 |
| `[ApiController]` + `[AllowCookieRedirect]` | 302 | 302 |
| plain controller + a hand-written disable attribute | 401 | 403 |

On .NET 9.0.14 every one of the portable rows is `302` / `302`.

Three further things the suite records:

- The 401 and the 403 still carry the `Location` header they would have
  redirected to. Only the status line differs between the two sides.
- `X-Requested-With: XMLHttpRequest` still turns every shape into 401 and 403 on
  .NET 10, including a shape carrying an explicit allow mark, so that check runs
  before the metadata is consulted. The same value in the query string counts
  too.
- Overriding `OnRedirectToLogin` and `OnRedirectToAccessDenied` the way the
  breaking-change notice recommends puts every shape back on 302 — and takes the
  `X-Requested-With` exception away with it, because both live in the same place.

## The mark

`MetadataTests` asserts the type names as they shipped in 10.0, because the
breaking-change notice still names `IApiEndpointMetadata`, which is a preview
spelling that is not in the assembly:

- `Microsoft.AspNetCore.Http.Metadata.IDisableCookieRedirectMetadata` — public
- `Microsoft.AspNetCore.Http.Metadata.IAllowCookieRedirectMetadata` — public
- `Microsoft.AspNetCore.Http.Metadata.DisableCookieRedirectMetadata` — exists,
  but is **not** public
- `Microsoft.AspNetCore.Http.AllowCookieRedirectAttribute` — public; there is no
  shipped attribute for the disable side, which is why
  `src/CookieRedirectApp/DisableCookieRedirectAttribute.cs` writes one in a line
- `Microsoft.AspNetCore.Mvc.ApiControllerAttribute` implements
  `IDisableCookieRedirectMetadata` itself, so the attribute you already have on
  the controller *is* the metadata

`GET /__endpoints` lists every routed endpoint, whether it carries either mark,
and which metadata object supplies the disable mark. That is the check to run
against your own app.

## Running it

```
./check.sh          # or ./check.ps1 on Windows
```

That runs the suite on both frameworks: 71 tests on `net9.0` and 92 on
`net10.0`, 163 in total. Both runs must be green.

To see the tables above printed from a live run rather than copied from here:

```
dotnet test tests/CookieRedirectTests/CookieRedirectTests.csproj -f net10.0 \
    --filter "Print_the_table|Print_what_marks|Report_the_versions" \
    --logger "console;verbosity=detailed"
```

Requires the .NET 10 and .NET 9 SDKs, and the matching ASP.NET Core shared
frameworks. Measured here on .NET 10.0.5 and .NET 9.0.14 on Windows 11;
`RuntimeVersionTests` prints the exact assembly versions and paths each run
used, so you can compare them with yours.

## Layout

```
src/CookieRedirectApp/     the app: one endpoint per shape, plus /__endpoints
tests/CookieRedirectTests/ the measurements
  Shapes.cs                the table, with expectations compiled per framework
  EndpointShapeTests.cs    status code and Location for each shape
  MetadataTests.cs         which endpoints carry the mark, and which type supplies it
  XhrTests.cs              the X-Requested-With exception
  RestoreRedirectsTests.cs the documented way back to redirects everywhere
  RuntimeVersionTests.cs   the versions these numbers came from
```

## Licence

MIT. See [LICENSE](LICENSE).
