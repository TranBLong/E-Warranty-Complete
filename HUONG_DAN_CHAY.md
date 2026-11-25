# Hướng dẫn chạy E-Warranty System

## Yêu cầu

- .NET SDK **10.x**
- SQL Server (LocalDB / SQL Express / Docker) hoặc đổi connection string
- Node.js 18+ (frontend React + Vite)
- (Tuỳ chọn) Docker

## 1. Backend

```bash
cd backend

# Cài User Secrets cho JWT SigningKey (BẮT BUỘC – không còn trong appsettings.Development.json)
dotnet user-secrets set "Jwt:SigningKey" "development-signing-key-32-bytes-1234567890"

# (Tuỳ chọn) Connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost\\SQLEXPRESS;Database=Warranty;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"

dotnet restore
dotnet ef database update   # hoặc app tự MigrateAsync khi start
dotnet run
```

API mặc định theo `launchSettings.json`.  
Swagger: `/swagger`  
Health: `/health`

### Tài khoản Development (chỉ seed khi môi trường Development)

| Email | Mật khẩu | Vai trò |
|-------|----------|---------|
| admin@warranty.local | Admin@123 | Admin |
| manager@warranty.local | Manager@123 | Manager |
| receptionist@warranty.local | Reception@123 | Receptionist |
| tech@warranty.local | Tech@123 | Technician |
| customer@warranty.local | Customer@123 | Customer |

## 2. Frontend

```bash
cd frontend
npm install
npm run dev
```

Mở `http://localhost:5173`.

## 3. Docker (API)

```bash
cd backend
docker build -t e-warranty-api .
docker run --rm -p 8080:8080 \
  -e Jwt__SigningKey="production-signing-key-at-least-32-bytes-long!!" \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=Warranty;User Id=sa;Password=Your_password123;TrustServerCertificate=True;" \
  e-warranty-api
```

`curl http://localhost:8080/health`

## 4. Postman nhanh

1. `POST /api/auth/login` `{ "email": "admin@warranty.local", "password": "Admin@123" }`
2. Bearer token → `GET /api/products?pageNumber=1&pageSize=10&keyword=iphone&sortBy=name&isDescending=false`
3. `GET /api/warranty-cards/by-serial/{SERIAL}` (không cần token)

## 5. Cấu trúc quan trọng

```
backend/
  Program.cs
  Warranty/Constants, Options, Middlewares, Services, Controllers
  Dockerfile, appsettings.Production.json
frontend/src/pages/
```
