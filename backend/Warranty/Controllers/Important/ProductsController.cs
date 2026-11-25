using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warranty.Constants;
using Warranty.Data;
using Warranty.Dtos;
using Warranty.Models;

namespace Warranty.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(AppDbContext dbContext) : ControllerBase
{
    private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "brand", "model", "serial", "serialnumber", "warranty", "warrantymonths", "created", "createdat"
    };

    /// <summary>Paged list with keyword search and whitelist sort (Buổi 11).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (query.NormalizedKeyword is { } term)
        {
            q = q.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Brand.ToLower().Contains(term) ||
                p.Model.ToLower().Contains(term) ||
                p.SerialNumber.ToLower().Contains(term));
        }

        var sortKey = SortWhitelist.Contains(query.SortBy ?? "")
            ? query.SortBy!.ToLowerInvariant()
            : "name";
        var desc = query.IsDescending;

        q = sortKey switch
        {
            "brand" => desc ? q.OrderByDescending(p => p.Brand) : q.OrderBy(p => p.Brand),
            "model" => desc ? q.OrderByDescending(p => p.Model) : q.OrderBy(p => p.Model),
            "serial" or "serialnumber" => desc ? q.OrderByDescending(p => p.SerialNumber) : q.OrderBy(p => p.SerialNumber),
            "warranty" or "warrantymonths" => desc ? q.OrderByDescending(p => p.WarrantyMonths) : q.OrderBy(p => p.WarrantyMonths),
            "created" or "createdat" => desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            _ => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name)
        };

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip(query.Skip).Take(query.Take)
            .Select(p => ToResponse(p))
            .ToListAsync(cancellationToken);

        return Ok(PagedResult<ProductResponse>.Create(items, query.PageNumber, query.Take, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Include(c => c.Category)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    /// <summary>Lookup product by serial number (public for staff / warranty check).</summary>
    [HttpGet("by-serial/{serial}")]
    public async Task<ActionResult<ProductResponse>> GetBySerial(string serial, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSerialNumber(serial);
        var product = await dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .SingleOrDefaultAsync(p => p.SerialNumber == normalized, cancellationToken);
        return product is null ? NotFound() : Ok(ToResponse(product));
    }

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(p => p.SerialNumber == serialNumber, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });

        if (request.CategoryId is int catId)
        {
            if (!await dbContext.Categories.AnyAsync(c => c.Id == catId && c.IsActive, cancellationToken))
                return BadRequest(new ProblemDetails { Title = "Category was not found or inactive." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = serialNumber,
            WarrantyMonths = request.WarrantyMonths,
            CategoryId = request.CategoryId
        };
        dbContext.Products.Add(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [Authorize(Policy = Policies.CanManageCatalog)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (product is null) return NotFound();

        var serialNumber = NormalizeSerialNumber(request.SerialNumber);
        if (await dbContext.Products.AnyAsync(c => c.Id != id && c.SerialNumber == serialNumber, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });

        if (request.CategoryId is int catIdUpd)
        {
            if (!await dbContext.Categories.AnyAsync(c => c.Id == catIdUpd && c.IsActive, cancellationToken))
                return BadRequest(new ProblemDetails { Title = "Category was not found or inactive." });
        }

        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Model = request.Model.Trim();
        product.SerialNumber = serialNumber;
        product.WarrantyMonths = request.WarrantyMonths;
        product.CategoryId = request.CategoryId;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new ProblemDetails { Title = "Serial number is already registered." });
        }

        return Ok(ToResponse(product));
    }

    [Authorize(Policy = Policies.AdminOnly)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (product is null) return NotFound();

        if (await dbContext.WarrantyCards.AnyAsync(card => card.ProductId == id, cancellationToken))
            return Conflict(new ProblemDetails { Title = "Products referenced by warranty cards cannot be deleted." });

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string NormalizeSerialNumber(string serialNumber) => serialNumber.Trim().ToUpperInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static ProductResponse ToResponse(Product product) => new(
        product.Id,
        product.Name,
        product.Brand,
        product.Model,
        product.SerialNumber,
        product.WarrantyMonths,
        product.CategoryId,
        product.Category?.Name,
        product.CreatedAt);
}
