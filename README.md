# Al-Amal Clinic — Backend API

REST API for a clinic appointment management system, built with ASP.NET Core following **Clean Architecture**.

**Frontend repository:** https://github.com/Samerthalji/clinic-frontend **Live demo (frontend, sample data):** https://clinic-frontend-omega-lake.vercel.app

## Features

- Three roles: **Patient**, **Doctor**, **Admin**, with role-based authorization.
- JWT authentication with **refresh tokens** (issue, refresh, revoke).
- Appointment booking and status management (Pending, Confirmed, Completed, Cancelled).
- Doctors and patients management with search and pagination.
- Global exception handling with consistent `ProblemDetails` responses.
- Admin account seeded from configuration (no credentials in source code).
- Application service layer covered with unit tests using **xUnit** and **Moq**.

## Architecture

```text
MedicalClinic.API             Controllers, middleware, exception handling, DI setup
MedicalClinic.Application     DTOs, interfaces, services, custom exceptions
MedicalClinic.Domain          Entities and core models
MedicalClinic.Infrastructure  EF Core DbContext, migrations, repositories, auth
MedicalClinic.API.Tests   Unit tests for application services
