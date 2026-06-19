# Start the app cleanly: stop any existing instance first (so the build can't be
# blocked by a locked DLL), then run on the https profile.
& "$PSScriptRoot\stop-app.ps1" | Out-Null
dotnet run --project "$PSScriptRoot\..\RazorShop.Web" --launch-profile https
