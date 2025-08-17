# Connect4 — Client–Server (.NET)

A complete client–server **Connect Four** system built for the course project (Course 10212 • Semester B • 2025).

- **Server** — ASP.NET Core Razor Pages + Web API, EF Core, SQL Server  
- **Client** — WinForms (GDI+ animations), local **SQLite** replay store  
- **Gameplay** — Human vs. server; **server plays random legal moves**  
- **Replay** — Equal-time playback, identical visuals/colors  
- **Polish** — Friendly HTML error page, JSON `ProblemDetails` for API errors, site-wide alerts, robust validation, structured logs

---

## Table of Contents

- [Tech Stack](#tech-stack)  
- [Folder Structure](#folder-structure)  
- [Prerequisites](#prerequisites)  
- [Quick Start](#quick-start)  
- [Configuration](#configuration)  
- [Database & Migrations](#database--migrations)  
- [Running the Projects](#running-the-projects)  
- [Features](#features)  
- [Data Model](#data-model)  
- [Client (WinForms)](#client-winforms)  
- [Validation & Error Handling](#validation--error-handling)  
- [Logging](#logging)  
- [Troubleshooting](#troubleshooting)  
- [Credits](#credits)

---

## Tech Stack

- **Server**: .NET **9.0**, ASP.NET Core Razor Pages + Web API, EF Core, SQL Server, Bootstrap 5  
- **Client**: .NET **8.0-windows**, WinForms, GDI+ (`Graphics`, `Bitmap`, `Timer`), EF Core SQLite  
- **Error Handling**: HTML Error page (pages), JSON `ProblemDetails` (API)  
- **Validation**: Data annotations, jQuery Validate + Bootstrap adapters, friendly messages  
- **Logging**: `ILogger` in controllers, consistent breadcrumbs for key actions  

---

## Folder Structure

```
Connect4/
├─ Server/                # ASP.NET Core (Razor Pages + Web API)
│  ├─ Controllers/        # GamesController, MovesController (API)
│  ├─ Data/               # EF DbContext & entities
│  ├─ Pages/              # Razor Pages (Auth, Admin, Queries, About, Error)
│  ├─ Queries/            # ApiExceptionFilter (API 500 -> ProblemDetails)
│  ├─ wwwroot/            # static assets (validation adapters, css)
│  └─ appsettings.*.json
└─ Client/                # WinForms client (.NET 8.0-windows)
   ├─ Data/               # ReplayDbContext (SQLite)
   ├─ Services/           # ApiClient (HTTP to server)
   ├─ Models/             # DTOs + Replay models
   └─ *.cs                # Forms, animations, UI logic
```

---

## Prerequisites

- **OS**: Windows (for the WinForms client)  
- **.NET SDKs**:
  - Server: **.NET 9.0 SDK**
  - Client: **.NET 8.0 SDK (Windows)**  
- **SQL Server**: LocalDB or any SQL Server instance  
- (Optional) **PowerShell** for the packaging script

> If `dotnet-ef` isn’t installed:  
> `dotnet tool install -g dotnet-ef`

---

## Quick Start

### 1) Configure Server DB
Edit `Server/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\MSSQLLocalDB;Database=Connect4Db;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 2) Apply Migrations & Run (Server)
```bash
cd Server
dotnet restore
dotnet ef database update
dotnet run

# Note the URL (usually http://localhost:5221)
```

### 3) Run Client (WinForms)
```bash
cd ../Client
dotnet build
dotnet run
```
> The client’s `ApiClient` uses the server base URL (default `http://localhost:5221`). Change if needed.

---

## Configuration

- **Server URL**: Shown on startup (e.g., `http://localhost:5221`).  
- **Client base URL**: In the client code (`Services/ApiClient.cs`).  
- **Sessions**: Server uses ASP.NET Core Session for simple “logged-in” state on the website after register.  
- **Cookies**: Basic cookies (`PlayerId`, `PlayerName`, `Identifier`) set post-register to keep navbar/Welcome in sync.

---

## Database & Migrations

- **Server DB** (SQL Server):
  - Entities: `Player`, `Game`, `Move`
  - Use `dotnet ef database update` to create/update schema.

- **Client Local DB** (SQLite):
  - Entities: `ReplayGame`, `ReplayMove`
  - Created on-demand by the client for saving replays.

> If you change entities, add migrations in the right project:
> - Server: run commands **from `Server/`**
> - Client: run commands **from `Client/`**

---

## Running the Projects

### Server (ASP.NET Core)
```bash
cd Server
dotnet run
```
- **Pages**: `/`, `/Auth/Register`, `/Players/...`, `/Games/...`, `/About`, `/Queries/...`
- **API**: `/api/games`, `/api/moves` (see [API Reference](#api-reference))

### Client (WinForms)
```bash
cd Client
dotnet run
```
- Create a new game, play turns (animation), server replies automatically with a random legal move.
- Save & select replays; playback with equal time spacing and identical visuals/colors.

---

## Features

- **Registration (website)**:  
  UniqueNumber (1–1000), FirstName, Phone, Country (combo).  
  Client-side: Register button disabled until form is valid.  
  Server-side: Model validation, unique UniqueNumber, error banners.

- **Game lifecycle**:  
  Create game → play human move → server random move → detect win/draw → persist moves and finalize game.

- **Replay (client)**:  
  Stores completed games locally (SQLite).  
  Dedicated UI: list replays by date/GameId, play back with equal timing and same visuals/colors.

- **Queries & Admin (website)**:
  - List players (full details; case-insensitive sort)
  - Players by names descending (two columns: Name + Last Game Date)
  - List all games (full details)
  - Distinct games (duplicates defined as “same player played them”)
  - Combo choose player → player’s games
  - Count games per player (desc)
  - Group players by games played (3+, 2, 1, 0)
  - Group players by country
  - Admin: Update Player; Delete Player / Delete Game (with confirmation)

- **Polish & Reliability (M6)**:
  - **HTML Error page** (Razor Pages) with request ID & friendly text
  - **ProblemDetails JSON** for API errors (400 via `[ApiController]`, 404/405 via middleware, 500 via filter)
  - **Validation**: helpful messages; Bootstrap styling; no default framework text leaking
  - **Alerts**: site-wide `TempData` success/error banners
  - **Logging**: structured logs for create/delete/move pipeline

---

## Data Model

### Server DB (SQL Server)

- **Player**  
  `Id (PK)`, `UniqueNumber (1..1000, unique)`, `FirstName`, `Phone`, `Country`, `CreatedAt`

- **Game**  
  `Id (PK)`, `PlayerId (FK)`, `StartTime`, `EndTime?`, `DurationSec?`, `Result (enum: Unknown/Win/Loss/Draw)`, `Notes?`

- **Move**  
  `Id (PK)`, `GameId (FK)`, `TurnIndex`, `PlayerKind (enum: Human/Server)`, `Column (0..6)`, `Row (0..5)`

### Client Local DB (SQLite)

- **ReplayGame**  
  `Id (PK)`, `GameId`, `PlayerId`, `StartedAt`, navigation `Moves[]`

- **ReplayMove**  
  `Id (PK)`, `ReplayGameId (FK)`, `TurnIndex`, `PlayerKind`, `Column`, `Row`

---

## Client (WinForms)

- **Play**: Start a new game (client UI), click a column to drop your disc; server responds automatically with a legal random move.  
- **Animations**: Disc drop via `Timer` (frame-based Y), subtle shine/gradient.  
- **Blocking**: Input disabled during server turn.  
- **Replay**:
  - **Save** after a match ends, stored in SQLite.
  - **Replay selector**: filter by your player; shows `GameId`, start time, moves count, duration, result.
  - **Playback**: equal time spacing (not original timings), identical visuals/colors.

---

## Validation & Error Handling

- **Client-side** (Razor Register page):  
  - Button disabled until all fields valid  
  - jQuery Validate + Bootstrap 5 styles (no default/unfriendly text)

- **Server-side**:
  - Model binding messages customized in `Program.cs`
  - Friendly HTML **Error** page for pages (`/Error`)
  - API:
    - **400**: automatic via `[ApiController]`  
    - **404/405**: JSON `ProblemDetails` via middleware  
    - **500**: JSON `ProblemDetails` via `ApiExceptionFilter` (with `traceId`)

---

## Logging

Key actions are logged with structured messages:
- `GamesController`: create, list by player, delete  
- `MovesController`: human move, illegal move, server reply, game end  

Use these logs to trace demo issues quickly.

---

## Troubleshooting

- **EF Tools missing**:  
  `dotnet tool install -g dotnet-ef`

- **SQL Server connection**:  
  Ensure `(localdb)\MSSQLLocalDB` exists or update the connection string in `appsettings.Development.json`.

- **Port already in use**:  
  Edit `Properties/launchSettings.json` or stop the conflicting process.

- **Client can’t reach server**:  
  - Verify server URL and port.  
  - Update client’s `ApiClient` base URL.  
  - Make sure server is running before launching client.

- **API returns HTML**:  
  Confirm the request path starts with `/api/...`. Non-API routes intentionally return the HTML Error page.

---

## Credits

- **Author(s)**: Tamar Aloni  
- **Course**: Project  • Semester B • 2025  
- **Repo**: https://github.com/AloniTamar/25B-21201A-Connect4  
- **About page**: `/about` (project overview & links)
