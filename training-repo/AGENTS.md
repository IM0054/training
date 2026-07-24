# OrderHub — project guidance

## Project

OrderHub is a small internal order-management training application. Keep solutions
proportional to a single ASP.NET Core application backed by one SQL Server
database; do not introduce distributed-system or multi-tenant architecture.

## Stack

- .NET 8, ASP.NET Core MVC, Razor Views, Bootstrap 5
- EF Core 8 and SQL Server
- xUnit with EF Core InMemory for tests

## Architecture and conventions

- `OrderHub.Web` owns controllers, view models, and Razor views.
- `OrderHub.Core` owns domain models, service contracts, and business rules.
- `OrderHub.Infrastructure` owns EF Core, repositories, migrations, and seed data.
- Keep controllers thin. Put business rules in Core services.
- Only repositories may access `OrderHubDbContext`; controllers and services must
  not use EF Core directly.
- Views bind to view models, never directly to domain models.
- Represent expected failures with `ServiceResult<T>` instead of exceptions.
- Validate user input with DataAnnotations and ModelState; invalid input must not
  become an HTTP 500 response.
- Use `decimal` for money. Apply membership discounts once in
  `OrderService.CalculateTotal`.
- Use `TempData["Success"]` and `TempData["Error"]` for operation feedback.
- Follow `ProductsController.cs` and `ProductService.cs` for naming and structure.

## Commands

- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project src/OrderHub.Web`

## Review and verification

- Reproduce a reported bug before changing code and record concrete observations.
- Fix the smallest relevant surface; do not mix unrelated refactors into a fix.
- Add a regression test for every bug.
- After code changes, use the project `code-reviewer` agent when delegation is
  available, then run the full test suite with `test-runner`.
- Report changed files and verification results.
- Do not commit until the user has completed any required browser verification.

## Sensitive and generated files

- Do not manually edit `src/OrderHub.Infrastructure/Migrations/**`.
- Ask before changing connection strings or `appsettings*.json`.
- Do not read or write `*.pfx`, `appsettings.Production.json`, or user secrets.

## Do not

- Do not add NuGet packages without explicit approval.
- Do not use `git reset --hard`, force-push, or destructive recursive deletion.
- Do not drop databases or run destructive SQL without explicit approval.
- Do not modify tests merely to hide a production-code defect.
- Do not refactor code unrelated to the current task.
