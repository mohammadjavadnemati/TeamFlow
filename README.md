# TeamFlow 🚀
> A professional project management and team collaboration platform built with ASP.NET Core 10

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791?style=for-the-badge&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=for-the-badge&logo=docker)
![xUnit](https://img.shields.io/badge/Tests-18%20passed-brightgreen?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)

A feature-rich project management system inspired by Trello, Jira, and ClickUp — built with modern .NET technologies.

---

## ✨ Features

### Core
- 🔐 **JWT Authentication** with Refresh Token rotation
- 🏢 **Multi-Workspace** support with role-based access
- 👥 **Team Members** with 5 role levels (Owner, Admin, PM, Developer, Viewer)
- 📁 **Projects** with health score tracking
- 🏃 **Sprints** with activate/complete lifecycle
- ✅ **Tasks** with subtasks, labels, checklists, watchers, bookmarks

### Collaboration
- 💬 **Comments** on tasks
- 📎 **File Attachments** via Azure Blob Storage (PDF, Image, ZIP, Document)
- 🔔 **Notifications** system
- 📋 **Activity Log** for full audit trail

### Analytics & Reporting
- 📊 **Dashboard** with project health scores
- 📈 **Analytics** — burndown chart, completion rate, tasks per user/status/priority
- 📅 **Calendar** view for deadlines
- 🗓️ **Weekly & Custom Reports**
- 🕐 **Project Timeline**

### Smart Features
- ⚠️ **Workload Analysis** — detects overloaded team members
- 🎯 **Deadline Risk Detector** — flags at-risk sprints
- 🏆 **Productivity Score** — per-member performance scoring
- 📊 **Team Statistics** — leaderboard and top performers
- 📢 **Daily Standup Summary** — auto-generated daily report

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 17 |
| Auth | ASP.NET Identity + JWT Bearer |
| File Storage | Azure Blob Storage |
| Documentation | Swagger (Swashbuckle) |
| Containerization | Docker + Docker Compose |
| Testing | xUnit + Moq |

---

## 📁 Project Structure

```
TeamFlow/
├── src/
│   ├── TeamFlow.API/              # Controllers, Middlewares, Extensions
│   ├── TeamFlow.Core/             # Entities, Interfaces, DTOs, Enums
│   └── TeamFlow.Infrastructure/   # EF Core, Services, Migrations
├── tests/
│   └── TeamFlow.Tests/            # Unit Tests (xUnit + Moq)
├── docker-compose.yml
├── Dockerfile
└── TeamFlow.sln
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Azure Storage Account](https://azure.microsoft.com/en-us/products/storage/blobs) (for file uploads)

### Run with Docker

```bash
git clone https://github.com/YOUR_USERNAME/TeamFlow.git
cd TeamFlow
docker-compose up --build
```

API will be available at `http://localhost:5000`
Swagger UI at `http://localhost:5000`

### Run without Docker

```bash
# 1. Set up PostgreSQL locally

# 2. Update appsettings.Development.json with your connection string

# 3. Run migrations
dotnet ef migrations add InitialCreate \
  --project src/TeamFlow.Infrastructure \
  --startup-project src/TeamFlow.API

dotnet ef database update \
  --project src/TeamFlow.Infrastructure \
  --startup-project src/TeamFlow.API

# 4. Run
cd src/TeamFlow.API
dotnet run
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `JwtSettings__SecretKey` | JWT secret (min 32 chars) |
| `JwtSettings__Issuer` | JWT issuer |
| `JwtSettings__Audience` | JWT audience |
| `AzureStorage__ConnectionString` | Azure Blob Storage connection string |
| `AzureStorage__ContainerName` | Blob container name |

---

## 🧪 Running Tests

```bash
dotnet test tests/TeamFlow.Tests
```

```
Test summary: total: 18, failed: 0, succeeded: 18
```

---

## 📡 API Overview

| Module | Endpoints |
|--------|-----------|
| Auth | Register, Login, Logout, Refresh Token, Change Password |
| Workspaces | CRUD + Member Management |
| Projects | CRUD + Health Score |
| Sprints | CRUD + Activate/Complete |
| Tasks | CRUD + Assign + Filter + Search |
| Subtasks | CRUD |
| Labels | CRUD |
| Checklists | CRUD + Items |
| Comments | CRUD |
| Files | Upload + Delete (Azure Blob) |
| Notifications | List + Mark as Read |
| Activity Log | Workspace + Task level |
| Dashboard | Workspace overview |
| Analytics | Burndown, Completion Rate, Per User/Status/Priority |
| Reports | Weekly + Custom date range |
| Calendar | Monthly deadlines view |
| Smart | Workload, Risk, Productivity, Standup |

Full API documentation available at `/swagger` when running.

---

## 🔐 Role Permissions

| Action | Owner | Admin | PM | Developer | Viewer |
|--------|-------|-------|----|-----------|--------|
| Manage Workspace | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete Workspace | ✅ | ❌ | ❌ | ❌ | ❌ |
| Invite Members | ✅ | ✅ | ❌ | ❌ | ❌ |
| Create Project | ✅ | ✅ | ✅ | ❌ | ❌ |
| Manage Sprints | ✅ | ✅ | ✅ | ❌ | ❌ |
| Create Tasks | ✅ | ✅ | ✅ | ✅ | ❌ |
| View Everything | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 📄 License

This project is licensed under the MIT License.