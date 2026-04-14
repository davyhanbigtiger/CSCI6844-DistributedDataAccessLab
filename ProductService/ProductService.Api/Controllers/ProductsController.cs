using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.DTOs;        // ← 新增
using ProductService.Api.Models;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _context;

    public ProductsController(ProductDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _context.Products.ToListAsync(ct);
        // 返回 DTO 列表，不直接暴露 Entity
        var response = products.Select(p =>
            new ProductResponseDto(p.Id, p.Name, p.Description, p.Price, p.Stock));
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync([id], ct);
        if (product == null) return NotFound();
        return Ok(new ProductResponseDto(product.Id, product.Name, product.Description, product.Price, product.Stock));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        // DTO → Entity
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock
        };
        await _context.Products.AddAsync(product, ct);
        await _context.SaveChangesAsync(ct);
        var response = new ProductResponseDto(product.Id, product.Name, product.Description, product.Price, product.Stock);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync([id], ct);
        if (product == null) return NotFound();

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync([id], ct);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}