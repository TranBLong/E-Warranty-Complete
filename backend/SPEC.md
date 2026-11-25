# SPEC.md — Đặc tả nghiệp vụ E-Warranty System (bản đầy đủ)

## Đề tài
Đề tài 13: Thiết kế Back-end cho hệ thống Quản lý bảo hành thiết bị điện tử (E-Warranty System).

## Vai trò và quyền hạn (Policies)

| Vai trò | Policy chính | Quyền |
|---|---|---|
| Admin | AdminOnly | Quản trị toàn hệ thống: CRUD User, catalog, service center |
| Manager | AdminOrManager | Báo cáo, quản lý trung tâm bảo hành, catalog |
| Receptionist | Staff | Tạo WarrantyCard, RepairRequest, gán Technician |
| Technician | InternalStaff | Xem / cập nhật RepairRequest được giao |
| Customer | (role) | Xem sản phẩm & phiếu BH của mình, theo dõi sửa chữa |

Policies đăng ký trong `Program.cs`: `AdminOnly`, `AdminOrManager`, `Staff`, `InternalStaff`, `CanManageUsers`, `CanManageCatalog`, `CanManageServiceCenters`, `CanAssignTechnician`.

Constants: `Warranty.Constants.Roles`, `Warranty.Constants.Policies`.

## Entity chính

### User
Id, FullName, Email (unique), PasswordHash, Phone, Role, IsActive, ServiceCenterId?, CreatedAt

### Product
Id, Name, Brand, Model, SerialNumber (unique), WarrantyMonths, CategoryId?, CreatedAt

### Category
Id, Name, Description?, IsActive, CreatedAt

### WarrantyCard
Id, ProductId, CustomerId, StartDate, EndDate, Status (Active / Expired / Voided)

### RepairRequest
Id, WarrantyCardId, ReceptionistId?, TechnicianId?, ServiceCenterId?, Description, Status, CreatedAt, UpdatedAt

### RepairRequestStatusHistory
Id, RepairRequestId, OldStatus, NewStatus, ChangedById, ChangedAt

### RepairRequestAttachment
Id, RepairRequestId, UploadedById, FileName, FilePath, ContentType, FileSize, UploadedAt

### ServiceCenter
Id, Name, Address, Phone?, IsActive, CreatedAt — quan hệ Staff (Users)

### Part / RepairRequestPart
Phụ tùng và liên kết với phiếu sửa chữa

### RefreshToken
Id, UserId, Token (hash), ExpiresAt, IsRevoked, CreatedAt

### Notification, Feedback, Invoice, Payment
Mở rộng nghiệp vụ (seed data có sẵn)

## Luồng nghiệp vụ

1. **Đăng ký bảo hành**: Receptionist tạo WarrantyCard gắn Product + Customer.
2. **Tra cứu theo serial**: `GET /api/warranty-cards/by-serial/{serial}` (AllowAnonymous) — kiểm tra còn hiệu lực.
3. **Yêu cầu sửa chữa**: Customer / Receptionist tạo RepairRequest từ WarrantyCard còn Active.
4. **Phân công**: Receptionist / Manager gán Technician; Manager gán staff vào ServiceCenter (`POST /api/service-centers/{id}/staff`).
5. **Cập nhật tiến độ**: Technician đổi Status → ghi history.
6. **Upload file**: Attachment qua `RepairRequestAttachmentService` (UploadOptions).
7. **Hết hạn tự động**: `WarrantyExpiryBackgroundService` chuyển Active → Expired khi EndDate < hôm nay.

## Phân trang & tìm kiếm (Buổi 11)

`QueryParameters`: PageNumber, PageSize, Keyword, SortBy, IsDescending.  
SortBy có **whitelist** trên Products, Users, Categories, ServiceCenters.  
Kết quả: `PagedResult<T>` (Items, PageNumber, PageSize, TotalCount, TotalPages).

## Xác thực & bảo mật

- JWT Access Token (mặc định 30 phút) + Refresh Token (7 ngày, rotate).
- `Jwt:SigningKey` **không** lưu trong appsettings.Development.json → User Secrets / env.
- Seed tài khoản mặc định **chỉ** khi `IsDevelopment()`.
- `ActiveUserMiddleware`: từ chối user bị khóa hoặc role token lệch DB.
- Global `ExceptionHandlingMiddleware`.

## Options Pattern (Buổi 10)

- `JwtOptions` → TokenService
- `UploadOptions` → RepairRequestAttachmentService

## Triển khai (Buổi 13)

- Dockerfile + .dockerignore
- `appsettings.Production.json`
- Health: `GET /health` (EF DbContext check)
- CORS cho frontend Vite

## Tài khoản seed (Development)

| Email | Password | Role |
|---|---|---|
| admin@warranty.local | Admin@123 | Admin |
| manager@warranty.local | Manager@123 | Manager |
| receptionist@warranty.local | Reception@123 | Receptionist |
| tech@warranty.local | Tech@123 | Technician |
| customer@warranty.local | Customer@123 | Customer |
