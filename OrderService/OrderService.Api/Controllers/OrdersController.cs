using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.Messaging;
using OrderService.Api.Models;
using OrderService.Api.Services;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly ICustomerClient _customerClient;
    private readonly IProductClient _productClient;
    private readonly IEventPublisher _publisher;

    public OrdersController(
        OrderDbContext context,
        ICustomerClient customerClient,
        IProductClient productClient,
        IEventPublisher publisher)
    {
        _context = context;
        _customerClient = customerClient;
        _productClient = productClient;
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _context.Orders.ToListAsync(ct);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetById(int id, CancellationToken ct)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] Order order, CancellationToken ct)
    {
        var customerExists = await _customerClient.CustomerExistsAsync(order.CustomerId);
        if (!customerExists)
            return BadRequest(new { error = "Customer does not exist.", customerId = order.CustomerId });

        var productExists = await _productClient.ProductExistsAsync(order.ProductId);
        if (!productExists)
            return BadRequest(new { error = "Product does not exist.", productId = order.ProductId });

        order.Status = "Created";
        order.CreatedAt = DateTime.UtcNow;

        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);

        _publisher.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            CreatedAt = order.CreatedAt
        });

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
