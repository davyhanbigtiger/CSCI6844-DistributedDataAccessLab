using CustomerService.Api.Data;
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


[HttpPost]
public async Task<IActionResult> Create(Customer customer, CancellationToken ct)
{
    await _context.Customers.AddAsync(customer, ct);
    await _context.SaveChangesAsync(ct);
    return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
}


    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetById(int id, CancellationToken ct)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer == null) return NotFound();
        return Ok(customer);
    }
}
