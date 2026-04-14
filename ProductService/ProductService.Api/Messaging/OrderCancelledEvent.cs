namespace ProductService.Api.Messaging;

public class OrderCancelledEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CancelledAt { get; set; }
}