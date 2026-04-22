# Copilot Instructions

## Project
This is a .NET 10 Blazor + API solution called "biblioteket" (a library app).

## Code style
- Use C# 14 features where appropriate
- Use Swedish names for domain models and variables (e.g. `HämtaPoster`, `LegimusHistoriePost`)
- Use `record` types for immutable data
- Prefer `async`/`await` throughout

## Architecture
- `biblioteket.Web` – Blazor frontend
- `biblioteket.ApiService` – ASP.NET Core API backend
- `biblioteket.ChromeImporter` – Chrome history import logic
- Services communicate via HTTP; URLs resolved through `services__apiservice__http__0`
- Redis used for caching (`ConnectionStrings__cache`)
- SQLite database mounted at `/data/Biblioteket2.db`

## Testing
- Test project: `biblioteket.External.Services.Test`
- Use xUnit