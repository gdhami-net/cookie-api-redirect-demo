#!/usr/bin/env bash
# Runs the suite that backs the post's claims, on both frameworks.
# Exits non-zero if any test fails.
set -euo pipefail
cd "$(dirname "$0")"

echo "cookie-api-redirect-demo — checking the post's claims"
echo "  EndpointShapeTests     the status code and Location for every endpoint shape"
echo "  MetadataTests          which endpoints carry the mark, and which type supplies it"
echo "  XhrTests               whether X-Requested-With still avoids the redirect"
echo "  RestoreRedirectsTests  the documented event override, putting every shape back on 302"
echo "  RuntimeVersionTests    the runtime and assembly versions these numbers came from"
echo

for tfm in net9.0 net10.0; do
  echo "=== $tfm ==="
  dotnet test tests/CookieRedirectTests/CookieRedirectTests.csproj -f "$tfm" \
    --logger "console;verbosity=normal"
  echo
done

echo "To see the two tables the post prints:"
echo "  dotnet test tests/CookieRedirectTests/CookieRedirectTests.csproj -f net10.0 \\"
echo "      --filter \"Print_the_table|Print_what_marks|Report_the_versions\" \\"
echo "      --logger \"console;verbosity=detailed\""
