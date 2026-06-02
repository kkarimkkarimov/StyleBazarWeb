# Stylebazar.Tests

Automated test suite for StyleBazar.

## Layout

| Folder | What | Needs a DB? |
|--------|------|-------------|
| `Unit/` | Validators (Register/Login/Review) + AutoMapper profile. Pure logic. | No |
| `Integration/` | Boots the real MVC app in-memory (`WebApplicationFactory`): `/health`, home page, security headers, localization. | **Yes** |

## Run

```bash
# Everything (needs SQL Server reachable for the integration tests)
dotnet test Stylebazar.Tests/Stylebazar.Tests.csproj

# Fast: unit tests only, no DB required
dotnet test Stylebazar.Tests/Stylebazar.Tests.csproj --filter "Category!=Integration"

# With coverage (CI uses this)
dotnet test Stylebazar.Tests/Stylebazar.Tests.csproj --collect:"XPlat Code Coverage"
```

## Integration test database

Integration tests run against a **dedicated** database (default
`StyleBazarDb_Test` on `localhost`) so they never touch dev data. The app's
startup `MigrateAsync` creates and migrates it automatically on first run.

Point them elsewhere (e.g. a CI service container) with:

```bash
# PowerShell
$env:TEST_DB_CONNECTION = "Server=localhost,1433;Database=StyleBazarTest;User Id=sa;Password=Test!Pass123;TrustServerCertificate=True"
```

If no SQL Server is reachable, the integration tests fail fast with a clear
message — run unit-only with the `Category!=Integration` filter above.
