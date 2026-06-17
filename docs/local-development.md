# Local development

## Prerequisites

- .NET 10 SDK
- A trusted HTTPS dev certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

## Run the app

**Use the `https` launch profile.** The app hardens antiforgery cookies to
`Secure = Always`, so over plain HTTP every form/htmx request throws
`AntiforgeryOptions.Cookie.SecurePolicy = Always, but the current request is not
an SSL request`. The `http` profile will look broken (cart, checkout, newsletter).

```bash
dotnet run --project RazorShop.Web --launch-profile https
```

- App: https://localhost:7153
- The dev environment name is **`Local`** (not `Development`) — set by the launch profile.
- On startup the app runs `EnsureCreated()` and seeds the SQLite DB at
  `RazorShop.Data/Db/local-shop.db`.

> Product images live under `wwwroot/products/**` and are git-ignored, so product
> thumbnails show as broken locally. That's expected.

## Secrets (user-secrets)

Secrets are **not** committed to `appsettings*.json` (those keys are blank). They
live in .NET user-secrets and are loaded by `Program.cs` for any non-Production
environment (the framework only auto-loads them for `Development`, but this app
runs as `Local`).

Set them once per machine, from the `RazorShop.Web` folder:

```bash
cd RazorShop.Web

dotnet user-secrets set "AdminUser"             "admin@artform.dk"
dotnet user-secrets set "AdminHash"             "<pbkdf2-hash>"        # see below
dotnet user-secrets set "PaymentApiKey"         "<quickpay-test-key>"
dotnet user-secrets set "EmailProvider:Password" "<smtp-password>"    # ':' = nested key

dotnet user-secrets list            # verify
```

Stored at (Windows): `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`,
where `<UserSecretsId>` is in `RazorShop.Web.csproj`. You can edit that JSON
directly instead of using `set`.

### Admin login

Username is whatever you set as `AdminUser`. `AdminHash` is a one-way
`Microsoft.AspNetCore.Identity.PasswordHasher` hash — generate one for a password
of your choice:

```bash
# throwaway generator
mkdir -p /tmp/hashgen && cd /tmp/hashgen
cat > hashgen.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>
</Project>
EOF
cat > Program.cs <<'EOF'
using Microsoft.AspNetCore.Identity;
var pw = args.Length > 0 ? args[0] : "ChangeMe123!";
Console.WriteLine(new PasswordHasher<object>().HashPassword(default!, pw));
EOF
dotnet run -- "ChangeMe123!"     # prints the hash; paste into user-secrets AdminHash
```

Restart the app after changing `AdminHash` (config is read at startup).

## Tests

```bash
dotnet test RazorShop.Tests/RazorShop.Tests.csproj
```

## "DLL is being used by another process" on build

A running app locks its own DLLs, so you can't build while an instance is running:

```
Unable to copy "...\RazorShop.Data.dll" ... being used by another process: RazorShop.Web (PID …)
```

Only one instance can own port 7153 at a time, too. Fixes:

- In Visual Studio: **Stop Debugging (Shift+F5)** before Build/Rebuild.
- From a terminal, stop any stray instance and free the port:
  ```powershell
  ./scripts/stop-app.ps1
  ```
- Or always start clean (stops any existing instance, then runs):
  ```powershell
  ./scripts/run.ps1
  ```

## Production

The same keys are blank in `appsettings.Production.json`. Provide them via
environment variables (or a secret store) before deploying — note `__` for nesting:

```
AdminUser=...   AdminHash=...   PaymentApiKey=...   EmailProvider__Password=...
```
