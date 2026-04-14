using CustomerService.Api.Data;
using CustomerService.Api.DTOs;
using CustomerService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _context;

    public CustomersController(CustomerDbContext context)
    {
        _context = context;
    }

    // GET /api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponseDto>>> GetAll(CancellationToken ct)
    {
        var customers = await _context.Customers.ToListAsync(ct);
        var response = customers.Select(c => new CustomerResponseDto(c.Id, c.Name, c.Email));
        return Ok(response);
    }

    // POST /api/customers
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerDto dto, CancellationToken ct)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email
        };

        await _context.Customers.AddAsync(customer, ct);
        await _context.SaveChangesAsync(ct);

        var response = new CustomerResponseDto(customer.Id, customer.Name, customer.Email);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    // GET /api/customers/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(int id, CancellationToken ct)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer == null)
            return NotFound();

        return Ok(new CustomerResponseDto(customer.Id, customer.Name, customer.Email));
    }
}