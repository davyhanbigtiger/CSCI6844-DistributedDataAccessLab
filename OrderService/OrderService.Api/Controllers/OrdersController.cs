using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Data;
using OrderService.Api.DTOs;           // ← 新增
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
        // 返回 DTO 列表
        var response = orders.Select(o => new OrderResponseDto(
            o.Id, o.CustomerId, o.ProductId,
            o.Quantity, o.TotalAmount, o.Status, o.CreatedAt));
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null) return NotFound();
        return Ok(new OrderResponseDto(
            order.Id, order.CustomerId, order.ProductId,
            order.Quantity, order.TotalAmount, order.Status, order.CreatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        // 验证 customer 和 product 是否存在（同步 HTTP 调用，和原来一样）
        var customerExists = await _customerClient.CustomerExistsAsync(dto.CustomerId);
        if (!customerExists)
            return BadRequest(new { error = "Customer does not exist.", customerId = dto.CustomerId });

        var productExists = await _productClient.ProductExistsAsync(dto.ProductId);
        if (!productExists)
            return BadRequest(new { error = "Product does not exist.", productId = dto.ProductId });

        // DTO → Entity
        var order = new Order
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);

        // 发布 OrderCreated 事件（和原来一样）
        _publisher.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            CreatedAt = order.CreatedAt
        });

        var response = new OrderResponseDto(
            order.Id, order.CustomerId, order.ProductId,
            order.Quantity, order.TotalAmount, order.Status, order.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

        [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order == null) return NotFound();
        if (order.Status == "Cancelled")
            return BadRequest(new { error = "Order is already cancelled." });

        order.Status = "Cancelled";
        await _context.SaveChangesAsync(ct);

        // 发布 OrderCancelled 事件 → ProductService 会恢复库存
        _publisher.Publish(new OrderCancelledEvent
        {
            OrderId = order.Id,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            CancelledAt = DateTime.UtcNow
        });

        return Ok(new OrderResponseDto(
            order.Id, order.CustomerId, order.ProductId,
            order.Quantity, order.TotalAmount, order.Status, order.CreatedAt));
    }
}