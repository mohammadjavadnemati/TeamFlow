TeamFlow API — فاز ۱: Authentication

تکنولوژی‌ها


ASP.NET Core 10 Web API
Entity Framework Core 10 + PostgreSQL
JWT Bearer + Refresh Token
ASP.NET Identity
Docker + Docker Compose
Swagger UI


ساختار پروژه

TeamFlow/
├── src/
│   ├── TeamFlow.API/                  # لایه ارائه
│   │   ├── Controllers/AuthController.cs
│   │   ├── Extensions/                # JWT, Swagger
│   │   ├── Middlewares/               # Exception Handler
│   │   └── Program.cs
│   ├── TeamFlow.Core/                 # لایه دامنه
│   │   ├── Entities/                  # User, RefreshToken
│   │   ├── DTOs/Auth/                 # Request/Response models
│   │   ├── Interfaces/IAuthService
│   │   └── Common/ApiResponse
│   └── TeamFlow.Infrastructure/       # لایه زیرساخت
│       ├── Data/ApplicationDbContext
│       └── Services/AuthService
├── Dockerfile
├── docker-compose.yml
└── TeamFlow.sln

راه‌اندازی سریع

با Docker (پیشنهادی)

bashdocker-compose up --build


API: http://localhost:5000
Swagger: http://localhost:5000


بدون Docker

bash# ۱. PostgreSQL local راه‌اندازی کن

# ۲. appsettings.Development.json را تنظیم کن

# ۳. Migration اجرا کن
cd src/TeamFlow.API
dotnet ef database update --project ../TeamFlow.Infrastructure

# ۴. اجرا
dotnet run

Migration ساختن (اولین بار)

bashcd src/TeamFlow.API
dotnet ef migrations add InitialCreate --project ../TeamFlow.Infrastructure --startup-project .
dotnet ef database update --project ../TeamFlow.Infrastructure --startup-project .

Endpoints فاز ۱

MethodRouteتوضیحAuthPOST/api/v1/auth/registerثبت‌نام-POST/api/v1/auth/loginورود-POST/api/v1/auth/refresh-tokenتمدید توکن-POST/api/v1/auth/logoutخروج✓POST/api/v1/auth/change-passwordتغییر رمز✓GET/api/v1/auth/meاطلاعات کاربر✓