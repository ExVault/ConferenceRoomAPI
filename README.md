# Conference Room API

ASP.NET Core Minimal API for managing conference room bookings.

## Tech stack

- .NET 10 / ASP.NET Core Minimal APIs
- Entity Framework Core 10
- SQLite
- OpenAPI (Swagger UI and Scalar)
- xUnit

## API overview

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/rooms` | Create a room |
| `PATCH` | `/rooms/{id}` | Update selected room fields or services |
| `DELETE` | `/rooms/{id}` | Delete a room |
| `GET` | `/rooms/available` | Find free rooms for a date and time range |
| `POST` | `/bookings` | Create a booking and return its price |
| `GET` | `/reports/room-usage` | Show room use and revenue for a date range |
| `GET` | `/reports/extra-service-usage` | Show extra service use and revenue for a date range |

## Getting started

The project requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

1. Clone the repository.
2. Set `SQLiteDatabasePath` in `appsettings.json` (alternatively create `appsettings.Local.json` and set `SQLiteDatabasePath` there).
3. Restore, build, and run the API:

```bash
dotnet restore
dotnet build
cd ConferenceRoomAPI
dotnet run
```

In the Development environment, the application will automatically create the SQLite file, apply migrations, and seed the database.

## Additional business rules

- Bookings must be at least 30 minutes long.
- Bookings must use 10-minute time steps.

## Design choices

- Deleting a room only marks it as inactive instead of removing it from the database. This keeps its booking history. Inactive rooms cannot be updated, found in availability searches, or booked again.
- Time zones and UTC offsets are outside the scope of this project. Booking start and end times are treated as local to the room.
- Booking rules can be changed in appsettings.json. Pricing periods must be ordered and cannot contain gaps or overlaps, matching the continuous schedule in the original technical specification.

## Tests

Run all unit and integration tests from the repository root:

```bash
dotnet test
```

Unit tests cover the pricing calculations. Integration tests cover room, booking, and report endpoints using a separate test database and controlled time.
