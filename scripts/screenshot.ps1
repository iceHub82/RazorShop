# Screenshot a URL with agent-browser.
#
# Works around Chrome's auto-de-elevation: when the shell runs as Administrator,
# Chrome refuses to run elevated and spawns a non-admin child (the original exits
# 0 doing nothing), which breaks headless screenshots. --do-not-de-elevate keeps
# Chrome in-process so DevTools connects and the capture succeeds.
#
# Usage:
#   ./scripts/screenshot.ps1 https://localhost:7153/ shot.png
#   ./scripts/screenshot.ps1 https://localhost:7153/Product/1 product.png
#
# Requires the app to be running (./scripts/run.ps1) and agent-browser installed.

param(
    [Parameter(Mandatory = $true)][string]$Url,
    [Parameter(Mandatory = $true)][string]$Out
)

$prof = Join-Path $env:TEMP ("ab-" + [guid]::NewGuid().ToString("N"))
$abArgs = "--do-not-de-elevate,--no-sandbox,--disable-gpu"

agent-browser --profile $prof --args $abArgs batch --bail `
    "open $Url" `
    "screenshot --full $Out" `
    "close"
