using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Enums;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/service-centers")]
public sealed class ServiceCentersController(AppDbContext dbContext) : ControllerBase
{
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "address", "created", "createdat"
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceCenterResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.ServiceCenters.AsNoTracking().Where(c => c.IsActive).AsQueryable();

        if (query.NormalizedKeyword is { } term)
        {
            q = q.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Address.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.ToLower().Contains(term)));
        }

        var sortKey = SortWhitelist.Contains(query.SortBy ?? "") ? query.SortBy!.ToLowerInvariant() : "name";
        var desc = query.IsDescending;
        q = sortKey switch
        {
            "address" => desc ? q.OrderByDescending(c => c.Address) : q.OrderBy(c => c.Address),
            "created" or "createdat" => desc ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            _ => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(c => new ServiceCenterResponse(c.Id, c.Name, c.Address, c.Phone, c.IsActive, c.CreatedAt,
                c.Staff.Count(s => s.IsActive)))
            .ToListAsync(cancellationToken);

        return Ok(PagedResult<ServiceCenterResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceCenterDetailResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.AsNoTracking()
            .Include(c => c.Staff.Where(s => s.IsActive))
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();

        return Ok(new ServiceCenterDetailResponse(
            center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt,
            center.Staff.Select(s => new StaffMemberDto(s.Id, s.FullName, s.Email, s.Role.ToString(), s.Phone)).ToList()));
    }

    [Authorize(Policy = Policies.CanManageServiceCenters)]
    [HttpPost]
    public async Task<ActionResult<ServiceCenterResponse>> Create(
        [FromBody] ServiceCenterRequest request,
        CancellationToken cancellationToken)
    {
        var center = new ServiceCenter
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true
        };
        dbContext.ServiceCenters.Add(center);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = center.Id },
            new ServiceCenterResponse(center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt, 0));
    }

    [Authorize(Policy = Policies.CanManageServiceCenters)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceCenterResponse>> Update(
        int id,
        [FromBody] ServiceCenterRequest request,
        CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();
        center.Name = request.Name.Trim();
        center.Address = request.Address.Trim();
        center.Phone = request.Phone?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        var staffCount = await dbContext.Users.CountAsync(u => u.ServiceCenterId == id && u.IsActive, cancellationToken);
        return Ok(new ServiceCenterResponse(center.Id, center.Name, center.Address, center.Phone, center.IsActive, center.CreatedAt, staffCount));
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (center is null) return NotFound();
        center.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Assign staff member to a service center (Buổi business requirement).</summary>
    [Authorize(Policy = Policies.CanManageServiceCenters)]
    [HttpPost("{id:int}/staff")]
    public async Task<ActionResult<StaffMemberDto>> AssignStaff(
        int id,
        [FromBody] AssignStaffRequest request,
        CancellationToken cancellationToken)
    {
        var center = await dbContext.ServiceCenters
            .SingleOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);
        if (center is null) return NotFound(new ProblemDetails { Title = "Service center not found." });

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return NotFound(new ProblemDetails { Title = "User not found." });

        if (user.Role is not (UserRole.Technician or UserRole.Receptionist or UserRole.Manager))
            return BadRequest(new ProblemDetails { Title = "Only Technician, Receptionist or Manager can be assigned to a service center." });

        user.ServiceCenterId = id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new StaffMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Phone));
    }

    /// <summary>Remove staff from service center.</summary>
    [Authorize(Policy = Policies.CanManageServiceCenters)]
    [HttpDelete("{id:int}/staff/{userId:int}")]
    public async Task<IActionResult> UnassignStaff(int id, int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Id == userId && u.ServiceCenterId == id, cancellationToken);
        if (user is null) return NotFound();

        user.ServiceCenterId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class ServiceCenterRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(200)]
    public string Name { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(500)]
    public string Address { get; init; } = string.Empty;
    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string? Phone { get; init; }
}

public sealed class AssignStaffRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public int UserId { get; init; }
}

public sealed record ServiceCenterResponse(
    int Id, string Name, string Address, string? Phone, bool IsActive, DateTime CreatedAt, int StaffCount);

public sealed record ServiceCenterDetailResponse(
    int Id, string Name, string Address, string? Phone, bool IsActive, DateTime CreatedAt,
    IReadOnlyList<StaffMemberDto> Staff);

public sealed record StaffMemberDto(int Id, string FullName, string Email, string Role, string? Phone);
