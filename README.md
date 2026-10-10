# Al-Amal Clinic — Backend API

REST API for a clinic appointment management system, built with **ASP.NET Core** following **Clean Architecture**.

**Frontend repository:** https://github.com/Samerthalji/clinic-frontend
**Live demo (frontend, sample data):** https://clinic-frontend-omega-lake.vercel.app

## Features

- Three roles: **Patient**, **Doctor**, **Admin**, with role-based authorization.
- JWT authentication with **refresh tokens** (issue, refresh, revoke).
- Appointment booking and status management (Pending, Confirmed, Completed, Cancelled).
- Doctors and patients management with search and pagination.
- Global exception handling with consistent `ProblemDetails` responses.
- Admin account seeded from configuration (no credentials in source code).

## Architecture

```
MedicalClinic.API             Controllers, middleware, exception handling, DI setup
MedicalClinic.Application     DTOs, interfaces, custom exceptions
MedicalClinic.Domain          Entities and core models
MedicalClinic.Infrastructure  EF Core DbContext, migrations, repositories, services
```

Dependencies point inward: `API → Application → Domain`, and `Infrastructure → Application → Domain`.

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core (Code First, migrations)
- ASP.NET Core Identity + JWT Bearer
- Repository and Unit of Work patterns
- Swagger / OpenAPI

## Getting Started

### Prerequisites

- .NET SDK (version used by the solution)
- A database server supported by the configured EF Core provider

### Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/Samerthalji/clinic-backend.git
   cd clinic-backend
   ```

2. Copy `MedicalClinic.API/appsettings.example.json` to `appsettings.Development.json` and set your connection string.

3. Store secrets with .NET User Secrets (never commit them):

   ```bash
   dotnet user-secrets set "JwtOptions:Key" "<random key, 32+ characters>" --project MedicalClinic.API
   dotnet user-secrets set "SeedAdmin:Email" "<admin email>" --project MedicalClinic.API
   dotnet user-secrets set "SeedAdmin:Password" "<strong password>" --project MedicalClinic.API
   ```

   The key names match the sections in `appsettings.example.json`.

4. Apply migrations and run:

   ```bash
   dotnet ef database update --project MedicalClinic.Infrastructure --startup-project MedicalClinic.API
   dotnet run --project MedicalClinic.API
   ```

5. Open Swagger at the URL printed in the console (usually `/swagger`).

## Configuration and Security

- Secrets (JWT key, admin credentials, connection string) are read from User Secrets or environment variables.
- Environment variables use `__` as the separator, for example `JwtOptions__Key`.
- No credentials are stored in the repository.

## Error Handling

Errors are thrown as custom exceptions (`NotFoundException`, `BadRequestException`, `ForbiddenException`) from the service layer and converted by a global `IExceptionHandler` into proper HTTP status codes with a `ProblemDetails` response. Internal details are hidden outside Development.

## Roadmap

- Unit tests (xUnit + NSubstitute) for the service layer.
- Deploy the API with a hosted database.
