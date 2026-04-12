# StudyApp - Clean Architecture .NET Core API
## Hướng dẫn Setup từ đầu

---

## 1. Tạo Solution và Projects

```bash
# Tạo solution
dotnet new sln -n StudyApp

# Tạo 4 projects
dotnet new classlib -n StudyApp.Domain -f net9.0
dotnet new classlib -n StudyApp.Application -f net9.0
dotnet new classlib -n StudyApp.Infrastructure -f net9.0
dotnet new webapi -n StudyApp.API -f net9.0

# Thêm vào solution
dotnet sln add StudyApp.Domain/StudyApp.Domain.csproj
dotnet sln add StudyApp.Application/StudyApp.Application.csproj
dotnet sln add StudyApp.Infrastructure/StudyApp.Infrastructure.csproj
dotnet sln add StudyApp.API/StudyApp.API.csproj

# Project references (Clean Architecture dependency flow)
dotnet add StudyApp.Application/StudyApp.Application.csproj reference StudyApp.Domain/StudyApp.Domain.csproj
dotnet add StudyApp.Infrastructure/StudyApp.Infrastructure.csproj reference StudyApp.Application/StudyApp.Application.csproj
dotnet add StudyApp.Infrastructure/StudyApp.Infrastructure.csproj reference StudyApp.Domain/StudyApp.Domain.csproj
dotnet add StudyApp.API/StudyApp.API.csproj reference StudyApp.Application/StudyApp.Application.csproj
dotnet add StudyApp.API/StudyApp.API.csproj reference StudyApp.Infrastructure/StudyApp.Infrastructure.csproj
```

---

## 2. Cài NuGet Packages

```bash
# StudyApp.Domain
cd StudyApp.Domain
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 9.0.0

# StudyApp.Infrastructure
cd ../StudyApp.Infrastructure
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.0

# StudyApp.API
cd ../StudyApp.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add package Swashbuckle.AspNetCore --version 7.2.0
```

---

## 3. Cấu trúc File

Sau khi copy code từ bộ file này vào đúng thư mục:

```
StudyApp/
├── Domain/
│   └── Entities/
│       ├── AppUser.cs              ← Extend IdentityUser
│       └── AllEntities.cs          ← Subject, Heading, Content, Question...
│
├── Application/
│   ├── DTOs/Auth/
│   │   └── AuthDtos.cs             ← RegisterDto, LoginDto, AuthResultDto...
│   └── Interfaces/
│       ├── IAuthService.cs
│       └── IJwtService.cs
│
├── Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs          ← DbContext + Identity + tất cả entities
│   └── Services/
│       ├── JwtService.cs            ← Tạo/validate JWT
│       └── AuthService.cs           ← Register, Login, RefreshToken...
│
└── API/
    ├── Controllers/
    │   └── AuthController.cs        ← API endpoints
    ├── Program.cs                   ← DI, Middleware setup
    └── appsettings.json
```

---

## 4. Cấu hình appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StudyAppDb;Trusted_Connection=True"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_THIS_TO_STRONG_KEY_32_CHARS_MIN",
    "Issuer": "StudyApp",
    "Audience": "StudyAppUsers",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

> ⚠️ **Quan trọng**: Thay `SecretKey` bằng chuỗi random >= 32 ký tự.  
> Dùng lệnh: `openssl rand -base64 32`

---

## 5. Tạo Migration và Update Database

```bash
# Từ thư mục gốc StudyApp/
dotnet ef migrations add InitialCreate \
    --project Infrastructure \
    --startup-project API

dotnet ef database update \
    --project Infrastructure \
    --startup-project API
```

---

## 6. Chạy ứng dụng

```bash
dotnet run --project API
```

Swagger UI: `https://localhost:5001/swagger`

---

## 7. Auth Endpoints

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/auth/register` | ❌ | Đăng ký tài khoản |
| POST | `/api/auth/login` | ❌ | Đăng nhập → nhận JWT |
| POST | `/api/auth/refresh-token` | ❌ | Làm mới Access Token |
| POST | `/api/auth/revoke-token` | ✅ | Logout / thu hồi token |
| GET  | `/api/auth/me` | ✅ | Thông tin user hiện tại |

### Ví dụ Request/Response

**POST /api/auth/register**
```json
// Request
{
  "email": "user@example.com",
  "password": "Abc123!",
  "fullName": "Nguyen Van A"
}

// Response 200
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "abc123...",
  "accessTokenExpires": "2025-04-10T10:00:00Z",
  "user": {
    "id": "guid-...",
    "email": "user@example.com",
    "fullName": "Nguyen Van A",
    "roles": ["User"]
  }
}
```

**POST /api/auth/login**
```json
// Request
{
  "email": "user@example.com",
  "password": "Abc123!"
}
// Response: giống register
```

**POST /api/auth/refresh-token**
```json
// Request
{ "refreshToken": "abc123..." }
// Response: accessToken mới + refreshToken mới (token rotation)
```

---

## 8. Architecture Flow

```
Request → AuthController (API)
            ↓
         IAuthService (Application - interface)
            ↓
         AuthService (Infrastructure - implementation)
            ↓
         UserManager<AppUser> + AppDbContext (Identity + EF Core)
```

### Dependency Rule (Clean Architecture)
```
Domain ← Application ← Infrastructure ← API
```
- **Domain**: Entities thuần, không phụ thuộc gì
- **Application**: Business logic, interfaces (không biết EF/Identity)
- **Infrastructure**: Implement interfaces, EF Core, JWT, Identity
- **API**: Controllers, DI, Middleware

---

## 9. Thêm Feature mới (Ví dụ: QuizService)

```csharp
// 1. Application/Interfaces/IQuizService.cs
public interface IQuizService {
    Task<QuizDto> CreateAsync(CreateQuizDto dto);
}

// 2. Infrastructure/Services/QuizService.cs
public class QuizService : IQuizService { ... }

// 3. Program.cs - đăng ký DI
builder.Services.AddScoped<IQuizService, QuizService>();

// 4. API/Controllers/QuizController.cs
[ApiController, Route("api/[controller]"), Authorize]
public class QuizController : ControllerBase { ... }
```

---

## 10. Roles

Hệ thống tự động seed 3 roles khi khởi động:
- `Admin` - Quản trị viên
- `User` - Người dùng thường (mặc định khi register)
- `VIP` - Người dùng trả phí

Gán role trong code:
```csharp
await _userManager.AddToRoleAsync(user, "VIP");
```

Bảo vệ endpoint theo role:
```csharp
[Authorize(Roles = "Admin")]
[Authorize(Roles = "User,VIP")]
```
