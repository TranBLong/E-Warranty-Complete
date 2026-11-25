# plan.md — Kế hoạch & mapping với chương trình môn học

| Buổi | Nội dung môn học | Hiện thực trong dự án |
|------|------------------|------------------------|
| 1 | DTO, Validation, Swagger, Postman | Dtos/*, DataAnnotations, Swagger + JWT scheme |
| 2 | Kiến trúc, Middleware, DI | Program.cs pipeline, ActiveUserMiddleware, ExceptionHandlingMiddleware, DI services |
| 3 | Controller, Routing, HTTP, RESTful, Model Binding, ActionResult | Controllers/*, route attributes, ActionResult\<T\> |
| 3b | Pagination, Filtering, Searching, Sorting | QueryParameters + whitelist sort trên Products/Users/Categories/ServiceCenters |
| 5 | EF Core: DbContext, DbSet, Code First, Migration | AppDbContext, Migrations/* |
| 6 | CRUD + LINQ + quan hệ | CRUD controllers, Include, quan hệ User–ServiceCenter, Warranty–Product… |
| 7 | Controller – Service – Repository, DI thực tế | TokenService, RepairRequestAttachmentService, BackgroundService (layered services) |
| 8 | Authentication JWT | AuthController, TokenService, RefreshToken rotate |
| 9 | Authorization, Role, Claims, Policies | Constants/Roles, Constants/Policies, AddPolicy, [Authorize(Policy=…)] |
| 10 | Upload File, Configuration, Environment, Options | UploadOptions, JwtOptions, IOptions\<T\>, App_Data/uploads |
| 12 | Global Exception, Logging, Custom Middleware | ExceptionHandlingMiddleware, ILogger, ActiveUserMiddleware |
| 13 | Swagger docs, CORS, Deploy, tối ưu config | Swagger UI, CORS Frontend, Dockerfile, /health, Production settings |

## Việc đã làm thêm (nghiệp vụ)

- BackgroundService hết hạn thẻ bảo hành
- API gán / gỡ nhân viên khỏi ServiceCenter
- Tra cứu bảo hành theo serial (AllowAnonymous)
- Seed chỉ trong Development
- Jwt SigningKey → User Secrets
- Gitignore App_Data, loại bin/obj khỏi index
