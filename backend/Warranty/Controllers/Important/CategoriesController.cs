using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoriesController(AppDbContext dbContext) : ControllerBase
{
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "created", "createdat"
    };

    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Categories.AsNoTracking().Where(c => c.IsActive).AsQueryable();

        if (query.NormalizedKeyword is { } term)
            q = q.Where(c => c.Name.ToLower().Contains(term) ||
                             (c.Description != null && c.Description.ToLower().Contains(term)));

        var sortKey = SortWhitelist.Contains(query.SortBy ?? "") ? query.SortBy!.ToLowerInvariant() : "name";
        var desc = query.IsDescending;
        q = sortKey switch
        {
            "created" or "createdat" => desc ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            _ => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(PagedResult<CategoryResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var cat = await dbContext.Categories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return cat is null
            ? NotFound()
            : Ok(new CategoryResponse(cat.Id, cat.Name, cat.Description, cat.IsActive, cat.CreatedAt));
    }

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var cat = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };
        dbContext.Categories.Add(cat);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = cat.Id },
            new CategoryResponse(cat.Id, cat.Name, cat.Description, cat.IsActive, cat.CreatedAt));
    }

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        [FromBody] CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var cat = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cat is null) return NotFound();
        cat.Name = request.Name.Trim();
        cat.Description = request.Description?.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new CategoryResponse(cat.Id, cat.Name, cat.Description, cat.IsActive, cat.CreatedAt));
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var cat = await dbContext.Categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cat is null) return NotFound();
        cat.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class CategoryRequest
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Description { get; init; }
}

public sealed record CategoryResponse(
    int Id, string Name, string? Description, bool IsActive, DateTime CreatedAt);
