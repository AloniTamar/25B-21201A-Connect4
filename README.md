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
- [Client (WinForms)](#client-winforms)  
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
### 2) Run Server
```bash
cd Server
dotnet run

# Note the URL (usually http://localhost:5221)
```

### 3) Run Client (WinForms)
```bash
cd ../Client
dotnet build
dotnet run
```
Once the WinForm launches, close it right away.
> The client’s `ApiClient` uses the server base URL (default `http://localhost:5221`). Change if needed.

### 4) Launch the game at http://localhost:5221 (or the server URL you set).

---

## Configuration

- **Server URL**: Shown on startup (e.g., `http://localhost:5221`).  
- **Client base URL**: In the client code (`Services/ApiClient.cs`).  
- **Sessions**: Server uses ASP.NET Core Session for simple “logged-in” state on the website after register.  
- **Cookies**: Basic cookies (`PlayerId`, `PlayerName`, `Identifier`) set post-register to keep navbar/Welcome in sync.

---

## Database & Migrations

- **Server DB** (SQL Server / LocalDB by default)
  - Entities: `Player`, `Game`, `Move`
  - **No manual step needed** for a fresh run — the server calls `Database.Migrate()` at startup and creates/updates the schema automatically.
  - Use EF tools **only when you change the model**:
    - Add migration:  
      ```
      cd Server
      dotnet ef migrations add <Name>
      ```
    - Update DB manually (optional):  
      ```
      dotnet ef database update
      ```

- **Client Local DB** (SQLite)
  - Entities: `ReplayGame`, `ReplayMove`
  - Created on demand by the client for saving replays; no manual steps needed.
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

### Launch the game at http://localhost:5221 (or the server URL you set).

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

## Client (WinForms)

- **Play**: Start a new game (client UI), click a column to drop your disc; server responds automatically with a legal random move.  
- **Animations**: Disc drop via `Timer` (frame-based Y), subtle shine/gradient.  
- **Blocking**: Input disabled during server turn.  
- **Replay**:
  - **Save** after a match ends, stored in SQLite.
  - **Replay selector**: filter by your player; shows `GameId`, start time, moves count, duration, result.
  - **Playback**: equal time spacing (not original timings), identical visuals/colors.

---

## Credits

- **Author(s)**: Tamar Aloni  
- **Course**: Project  • Semester B • 2025  
- **Repo**: https://github.com/AloniTamar/25B-21201A-Connect4  
- **About page**: `/about` (project overview & links)
