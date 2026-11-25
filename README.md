# E-Warranty System

Hệ thống quản lý bảo hành thiết bị điện tử — ASP.NET Core Web API + React.

## Tính năng chính

- JWT Authentication + Refresh Token + Role / Policy Authorization
- CRUD Products, Categories, Users, Service Centers, Warranty Cards, Repair Requests
- Phân trang / tìm kiếm / sắp xếp thống nhất (`QueryParameters`)
- Upload file đính kèm (Options Pattern)
- BackgroundService hết hạn thẻ bảo hành
- Tra cứu bảo hành theo serial
- Gán nhân viên vào trung tâm bảo hành
- Health check, Dockerfile, Swagger, CORS

## Chạy nhanh

Xem **[HUONG_DAN_CHAY.md](HUONG_DAN_CHAY.md)**.

```bash
cd backend
dotnet user-secrets set "Jwt:SigningKey" "development-signing-key-32-bytes-1234567890"
dotnet run

cd frontend && npm i && npm run dev
```

## Tài liệu

- [SPEC.md](backend/SPEC.md) — đặc tả nghiệp vụ
- [plan.md](backend/plan.md) — mapping buổi học
- [BAO_CAO_DU_AN.md](BAO_CAO_DU_AN.md) — báo cáo kết thúc môn + gợi ý slide

## Tài khoản Development

| Email | Password | Role |
|-------|----------|------|
| admin@warranty.local | Admin@123 | Admin |
| customer@warranty.local | Customer@123 | Customer |
