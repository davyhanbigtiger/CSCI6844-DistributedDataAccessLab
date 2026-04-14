namespace OrderService.Api.DTOs;

// 客户端下单时传入的数据
public record CreateOrderDto(
    int CustomerId,
    int ProductId,
    int Quantity
);

// 返回给客户端的数据
public record OrderResponseDto(
    int Id,
    int CustomerId,
    int ProductId,
    int Quantity,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt
);