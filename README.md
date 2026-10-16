# RaceDay Event Management System

[![RaceDay API CI](https://github.com/Kele5295/RaceDay-Event-Management-System/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Kele5295/RaceDay-Event-Management-System/actions/workflows/dotnet.yml)

## Project Description

RaceDay supports South African road running, walking and cycling events. Organisers manage events, categories, enrolments and results. Participants register, browse events, enrol and view their results.

This repository contains the Part 1 planning documents and the Part 2 ASP.NET Core Web API for the PROG6212 Programming 2B Portfolio of Evidence.

## Repository Contents

| Location | Contents |
| --- | --- |
| `docs/` | Part 1 ERD, SQL script and API endpoint plan |
| `Part2/RaceDay.API/` | ASP.NET Core Web API, models, controllers and EF Core migrations |
| `Part2/RaceDay.API.Tests/` | Automated API integration tests |
| `.github/workflows/part1-ci.yml` | Part 1 document checks |
| `.github/workflows/dotnet.yml` | Part 2 restore, build and automated tests |

## Technologies

- C# and .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- Microsoft SQL Server Express
- Swagger UI through Swashbuckle
- xUnit and ASP.NET Core MVC Testing
- EF Core InMemory provider for automated tests
- Git and GitHub Actions

## User Roles

### Organiser

- Create events and update or delete their own events.
- Manage categories belonging to their events.
- View enrolments for their events.
- Capture and update results for their events.

### Participant

- Register and log in.
- Browse events and categories.
- Enrol in an event category.
- View and cancel their own enrolments.
- View their own results and performance history.

Event browsing and event type/category listings are public. Protected operations require a login session. Ownership checks restrict access to the relevant organiser or participant.

## Prerequisites

To run the API locally:

1. Install the .NET 10 SDK.
2. Install SQL Server Express with an instance named `SQLEXPRESS`, or configure another SQL Server instance.
3. Use Visual Studio with ASP.NET and web development support, or the .NET CLI.
4. Trust the local HTTPS development certificate when prompted.

The automated tests do not require SQL Server.

## Get the Source Code

```powershell
git clone https://github.com/Kele5295/RaceDay-Event-Management-System.git
cd RaceDay-Event-Management-System
```

The commands below assume that the terminal is in the repository root.

## Part 2 Database Setup

Part 2 uses EF Core code-first migrations to create `RaceDayPart2DB`. It is separate from the Part 1 `RaceDayDB` database.

The connection string is configured in `Part2/RaceDay.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=RaceDayPart2DB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
  }
}
```

Update the server name if your SQL Server instance differs. The example uses Windows authentication. `TrustServerCertificate=True` is intended for local development.

Restore the solution:

```powershell
dotnet restore Part2/RaceDay.API/RaceDay.API.slnx
```

In Visual Studio, open `Part2/RaceDay.API/RaceDay.API.slnx`. Open **Tools → NuGet Package Manager → Package Manager Console**, select `RaceDay.API` as the default project, and run:

```powershell
Update-Database -Project RaceDay.API -StartupProject RaceDay.API
```

This applies the existing migrations and seeds the Road Running, Walking and Cycling event types. Do not create another initial migration.

User accounts are created through the registration endpoint; demo accounts are not seeded automatically.

## Run the API

Set `RaceDay.API` as the startup project and select the **https** launch profile in Visual Studio.

Alternatively, run from the repository root:

```powershell
dotnet run --project Part2/RaceDay.API --launch-profile https
```

Open:

https://localhost:7144/swagger

Swagger is enabled in the Development environment.

## Authentication and Sessions

Passwords are hashed using ASP.NET Core `PasswordHasher<User>`. Password hashes are not returned in API responses.

Successful login stores the user ID and role in a server-side session. The browser receives an HTTP-only, secure session cookie. Sessions have a 30-minute idle timeout.

Use HTTPS and the same browser when testing protected endpoints in Swagger. Log in again after restarting the application because session storage is held in memory.

Logout clears the session. Protected endpoints return:

- `401 Unauthorized` when no valid login session exists.
- `403 Forbidden` when the role or resource ownership does not permit the operation.

### Register an Organiser

Send this body to `POST /api/auth/register`:

```json
{
  "firstName": "Test",
  "lastName": "Organiser",
  "email": "organiser@example.com",
  "password": "RaceDayDemo!2026",
  "role": "Organiser",
  "phoneNumber": "0825550101"
}
```

For a participant, use a different email address and set `"role": "Participant"`.

### Log In

Send this body to `POST /api/auth/login`:

```json
{
  "email": "organiser@example.com",
  "password": "RaceDayDemo!2026"
}
```

These are example credentials for accounts created locally through registration.

## API Endpoints

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Clears current session |
| GET | `/api/profile` | Logged-in user |
| PUT | `/api/profile` | Logged-in user |
| GET | `/api/events` | Public |
| GET | `/api/events/{id}` | Public |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Event owner |
| DELETE | `/api/events/{id}` | Event owner |
| GET | `/api/eventtypes` | Public |
| GET | `/api/events/{eventId}/categories` | Public |
| POST | `/api/events/{eventId}/categories` | Event owner |
| PUT | `/api/categories/{id}` | Event owner |
| DELETE | `/api/categories/{id}` | Event owner |
| POST | `/api/enrolments` | Participant |
| GET | `/api/enrolments/me` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Event owner |
| DELETE | `/api/enrolments/{id}` | Enrolment owner |
| POST | `/api/enrolments/{enrolmentId}/result` | Event owner |
| PUT | `/api/results/{id}` | Event owner |
| GET | `/api/results/me` | Participant |
| GET | `/api/events/{eventId}/results` | Event owner |

Swagger provides request schemas and an interactive interface for executing these endpoints.

### Example Event Request

Use a future event date when testing enrolment.

```json
{
  "eventName": "Pretoria Community Run",
  "description": "A community running event.",
  "eventDate": "2026-12-05",
  "location": "Pretoria",
  "distance": 5,
  "eventTypeID": 1
}
```

### Example Category Request

Use the event ID returned when creating the event.

```json
{
  "categoryName": "5km Open",
  "description": "Open category for the 5km run.",
  "maximumParticipants": 100
}
```

The capacity property is named `maximumParticipants`.

### Example Enrolment Request

Log in as a Participant and use the actual event and category IDs:

```json
{
  "eventID": 1,
  "categoryID": 1
}
```

## Validation and Business Rules

- Email addresses must be unique.
- Roles must be `Organiser` or `Participant`.
- Registration passwords must contain at least eight characters.
- Event distance must be between 0.01 and 9999.99.
- Decimal distance validation uses culture-independent parsing.
- An enrolment category must belong to the selected event.
- Participants cannot enrol in past events.
- Duplicate enrolments return `409 Conflict`.
- Category capacity is checked before enrolment.
- Cancelling an enrolment preserves it with a `Cancelled` status.
- A cancelled enrolment currently prevents another enrolment for the same participant and event.
- Enrolments with recorded results cannot be cancelled.
- Results require a confirmed enrolment and an event date that is not in the future.
- Result times use `HH:mm:ss` and must be less than 24 hours.
- Deleting resources with related enrolment records is restricted.

## Automated Tests

Run from the repository root:

```powershell
dotnet test Part2/RaceDay.API/RaceDay.API.slnx
```

The test suite contains 20 test cases covering:

- Registration, password hashing and duplicate email rejection.
- Invalid passwords and roles during registration.
- Successful login, failed login and session-based profile access.
- Logout and anonymous access restrictions.
- Event creation and retrieval.
- Invalid distances and missing events.
- Participant role restrictions and organiser ownership checks.
- Viewing and cancelling enrolments.
- Protection of another participant's enrolment.
- Missing enrolments and organiser enrolment restrictions.

Tests use an isolated EF Core InMemory database and do not modify the local SQL Server database.

The automated suite does not verify SQL Server constraints, database-generated defaults or transaction behaviour. Enrolment creation and duplicate rejection were checked manually through Swagger against SQL Server.

## Continuous Integration

The Part 2 workflow restores dependencies, builds the solution in Release mode and runs the automated tests on every push and pull request.

Test result files are uploaded as a workflow artifact.

- [Part 2 workflow](.github/workflows/dotnet.yml)
- [GitHub Actions runs](https://github.com/Kele5295/RaceDay-Event-Management-System/actions)
- [Successful Part 2 CI run](https://github.com/Kele5295/RaceDay-Event-Management-System/actions/runs/38940285442)

## Part 1 Deliverables

The original planning and database documents are retained:

- [Entity Relationship Diagram](docs/RaceDay_ERD.pdf)
- [RESTful API Endpoint Plan](docs/RaceDay_API_Endpoint_Plan.md)
- [SQL Server Database Script](docs/RaceDay_Database.sql)

The database contains six main entities: User, EventType, Event, Category, Enrolment and Result.

For the Part 1 database, open `docs/RaceDay_Database.sql` in SQL Server Management Studio and execute it against a suitable local instance. This creates the separate `RaceDayDB` database and sample records.

For Part 2, use EF Core migrations as described above.

### Implementation Changes from the Part 1 Plan

Part 2 implements session-based authentication. Any token-based wording in the original plan is superseded by the session implementation.

Logout is included as an additional endpoint. Conflicting operations can return `409 Conflict`, and enrolment cancellation preserves the record rather than physically deleting it.

## AI Assistance Disclosure

ChatGPT was used to assist with implementation guidance, code drafts, troubleshooting, automated test drafts and documentation. Local build and test checks were performed during development.

## Module Information

**Module:** PROG6212 – Programming 2B  
**Assessment:** Portfolio of Evidence – Parts 1 and 2  
**System:** RaceDay Event Management System