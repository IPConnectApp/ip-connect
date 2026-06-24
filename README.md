# IP Connect

## Overview

IP Connect is a social-networking web app. Users register, build a profile, add friends, chat in real time, and share photo albums. Built with ASP.NET Core MVC (.NET 9) on top of SQL Server, with real-time features powered by SignalR.

## Main Functionalities

- **Authentication & Accounts** — registration, login, email confirmation, password reset (ASP.NET Core Identity).
- **User Profiles** — display name, bio, birth date, gender, profile picture, online/last-seen status.
- **Friendships** — send/accept/reject friend requests, friend search, friend list management.
- **Real-time Chat** — 1:1 and group conversations, message history, delivered via SignalR (`ChatHub`).
- **Notifications** — real-time in-app notifications (friend requests, messages, etc.) via SignalR (`NotificationHub`).
- **Photo Albums** — create albums, upload/reorder/delete photos, photos stored in Azure Blob Storage.

## Technologies

- **.NET 9 / ASP.NET Core MVC** — application framework
- **Entity Framework Core 9** (SQL Server provider) — ORM / migrations
- **ASP.NET Core Identity** — authentication, user/role management
- **SignalR** — real-time chat and notifications
- **Azure Blob Storage** (`Azure.Storage.Blobs`) — photo storage
- **SMTP (Gmail)** — outgoing email (verification, password reset)
- **xUnit** — unit/repository/service tests (`tests/ip-connect.Tests`)
- **GitHub Actions** — CI (PR test run) and CD (deploy to Azure on `develop`)

## Frontend

Server-rendered Razor views (`.cshtml`) with partial layouts, styled with custom CSS (`wwwroot/css`) and Bootstrap. Client-side interactivity is plain JavaScript + jQuery (no SPA framework):

- `wwwroot/js/profile/*` — per-feature scripts (albums, album photos, chat, friends, friends search, profile badge)
- `wwwroot/js/global-signalr.js` — shared SignalR connection setup
- `wwwroot/js/utils/emoji.js` — chat emoji helper
- `wwwroot/lib` — vendored Bootstrap, jQuery, jQuery Validation

## Architecture

Classic layered MVC architecture:

```
Controllers  →  Services  →  Repositories  →  EF Core (ApplicationDbContext)  →  SQL Server
                   ↓
              Hubs (SignalR)  →  connected clients
```

- **Controllers** handle HTTP requests/routing and return Views or JSON.
- **Services** contain business logic, one per domain area (Account, Album, Conversation, Friendship, Notification, Photo, UserProfile, User, Email, Files, BlobStorage), each behind an interface for testability.
- **Repositories** wrap EF Core data access per entity, behind interfaces (`IRepository<T>` base pattern).
- **Hubs** (`ChatHub`, `NotificationHub`) push real-time events to connected clients.
- **Middleware** (`GlobalExceptionHandlerMiddleware`) centralizes exception handling.
- Dependency injection wires repositories/services/hubs in `Program.cs`.

## User Roles

The app has no admin/role tiers — ASP.NET Identity's role tables exist but are unused. Two access levels apply:

- **Guest (unauthenticated)** — can view the landing/home page, register, and log in.
- **Authenticated User** — required (`[Authorize]`) for Account settings, Albums, Chat, Friendships, Notifications, and Profile pages. Every authenticated user has equal capabilities, scoped to their own data and relationships.

## Domain Model

| Entity | Purpose | Key relationships |
|---|---|---|
| `ApplicationUser` | Identity user (extends `IdentityUser`) | 1–1 `UserProfile`; owns Albums, Photos, Conversations, Messages, Friendships, Notifications |
| `UserProfile` | Display name, bio, birth date, gender, online status | belongs to `ApplicationUser` |
| `Friendship` | Friend request/relationship between two users | `User` ↔ `Friend`, status (`Pending`/`Accepted`/...) |
| `Conversation` | 1:1 or group chat | created by `ApplicationUser`; has many `ConversationMember`, `Message` |
| `ConversationMember` | Join table: user ↔ conversation, tracks read state | `Conversation`, `ApplicationUser` |
| `Message` | Single chat message | belongs to `Conversation`, sent by `ApplicationUser` |
| `Notification` | In-app notification event | belongs to `ApplicationUser`, optionally references another user/entity |
| `Album` | Photo album | belongs to `ApplicationUser`; has many `Photo` |
| `Photo` | Single photo (Azure Blob URL, display order) | belongs to `Album` and `ApplicationUser` |

## Database

- **SQL Server** via EF Core, schema managed through **migrations** (`src/ip-connect/Migrations`).
- Identity tables (`AspNetUsers`, `AspNetRoles`, etc.) plus app tables for the entities above.
- Per-entity `IEntityTypeConfiguration<T>` classes in `Data/Configurations` define relationships/constraints (cascade rules, indexes, required fields).
- Connection string: `ConnectionStrings:DefaultConnection` in `appsettings.json` / `appsettings.Development.json` / environment config.

## Project Structure

```
ip-connect/
├── src/ip-connect/
│   ├── Controllers/        MVC controllers (Account, Album, Chat, Friendship, Home, Notification, Profile)
│   ├── Services/           Business logic, one folder per domain (+ interfaces)
│   ├── Repositories/       EF Core data access, one folder per entity (+ interfaces)
│   ├── Models/             Domain entities + Enums (FriendshipStatus, NotificationType)
│   ├── Dtos/                Data transfer objects for controller/service boundaries
│   ├── Data/                ApplicationDbContext + EF entity configurations
│   ├── Migrations/          EF Core migrations
│   ├── Hubs/                SignalR hubs (ChatHub, NotificationHub)
│   ├── Middleware/          GlobalExceptionHandlerMiddleware
│   ├── Exceptions/          Custom exception types
│   ├── Common/              Shared helpers/constants
│   ├── Views/                Razor views, organized by controller + Shared layouts/partials
│   ├── wwwroot/              CSS, JS, fonts, images, vendored libs, email templates
│   └── Program.cs            App startup, DI registration, middleware pipeline, routing
├── tests/ip-connect.Tests/
│   ├── RepositoryTests/      Repository-level tests
│   ├── ServiceTests/         Service-level tests
│   └── Helpers/              Test DB context factory
└── .github/workflows/        CI (unit tests on PR) and CD (Azure deploy on push to develop)
```
