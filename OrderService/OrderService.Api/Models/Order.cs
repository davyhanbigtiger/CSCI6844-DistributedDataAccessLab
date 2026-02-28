namespace OrderService.Api.Models;


public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductId { get; set; }          // 新增：需要验证产品
    public int Quantity { get; set; }           // 新增：购买数量（用于扣库存）
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Created";   // 新增：订单状态
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 新增
}
