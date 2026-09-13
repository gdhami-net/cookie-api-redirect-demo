# Runs the suite that backs the post's claims, on both frameworks.
# Exits non-zero if any test fails.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host 'cookie-api-redirect-demo - checking the post''s claims'
Write-Host '  EndpointShapeTests     the status code and Location for every endpoint shape'
Write-Host '  MetadataTests          which endpoints carry the mark, and which type supplies it'
Write-Host '  XhrTests               whether X-Requested-With still avoids the redirect'
Write-Host '  RestoreRedirectsTests  the documented event override, putting every shape back on 302'
Write-Host '  RuntimeVersionTests    the runtime and assembly versions these numbers came from'
Write-Host ''

foreach ($tfm in @('net9.0', 'net10.0')) {
    Write-Host "=== $tfm ==="
    dotnet test tests/CookieRedirectTests/CookieRedirectTests.csproj -f $tfm --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host ''
}

Write-Host 'To see the two tables the post prints:'
Write-Host '  dotnet test tests/CookieRedirectTests/CookieRedirectTests.csproj -f net10.0 `'
Write-Host '      --filter "Print_the_table|Print_what_marks|Report_the_versions" `'
Write-Host '      --logger "console;verbosity=detailed"'
exit 0
