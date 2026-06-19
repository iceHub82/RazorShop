# Stop any running RazorShop.Web instance and free port 7153.
# Run this if a build fails with:
#   "RazorShop.Data.dll ... is being used by another process"
# A running app locks its own DLLs, so the build can't overwrite them.

$hosts = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='RazorShop.Web.exe'" |
    Where-Object { $_.CommandLine -match 'RazorShop\.Web' -and $_.CommandLine -notmatch 'devenv' }

if ($hosts) {
    $hosts | ForEach-Object {
        Write-Host "Stopping RazorShop.Web host PID $($_.ProcessId)"
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "No RazorShop.Web instance running."
}

Start-Sleep -Milliseconds 500
$held = Get-NetTCPConnection -LocalPort 7153 -State Listen -ErrorAction SilentlyContinue
if ($held) { Write-Host "Port 7153 still held by PID $($held.OwningProcess -join ',')" }
else       { Write-Host "Port 7153 free." }
