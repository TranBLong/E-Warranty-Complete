using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Middlewares;
using Warranty.Options;
using Warranty.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options Pattern (Buổi 10) ----------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) &&
                   Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
        "Jwt:SigningKey must be at least 32 UTF-8 bytes")
    .ValidateOnStart();

builder.Services.AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing from configuration.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) ||
    Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be configured with at least 32 UTF-8 bytes. " +
        "In Development run: dotnet user-secrets set \"Jwt:SigningKey\" \"your-32-byte-or-longer-secret-key-here\" " +
        "Or set environment variable Jwt__SigningKey.");
}

// ---------- Database ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.")));

// ---------- Controllers & JSON ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();

// ---------- Swagger + JWT (Buổi 1 / Buổi 13) ----------
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste access token only (do not type 'Bearer ').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ---------- Services ----------
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<RepairRequestAttachmentService>();
builder.Services.AddHostedService<WarrantyExpiryBackgroundService>();

// ---------- Authentication & Authorization (Buổi 8 + Buổi 9) ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.AdminOrManager, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist));
    options.AddPolicy(Policies.InternalStaff, p =>
        p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist, Roles.Technician));
    options.AddPolicy(Policies.CanManageUsers, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanManageCatalog, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanManageServiceCenters, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.CanAssignTechnician, p =>
        p.RequireRole(Roles.Admin, Roles.Manager, Roles.Receptionist));
});

// ---------- Health checks (Buổi 13) ----------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ---------- Migrate + Seed (seed only in Development) ----------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedAsync(dbContext);
    }
}

// ---------- Middleware pipeline order (Buổi 12) ----------
// Exception handling first so it wraps everything downstream
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program visible to integration tests
public partial class Program;
