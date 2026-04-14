using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/aggregate")]
public class AggregationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AggregationController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("order-details/{orderId}")]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var client = _httpClientFactory.CreateClient();

        // Step 1: 先拿订单
        var orderResponse = await client.GetAsync(
            $"http://order-service:8080/api/Orders/{orderId}");

        if (!orderResponse.IsSuccessStatusCode)
            return NotFound(new { message = $"Order {orderId} not found." });

        var orderJson = await orderResponse.Content.ReadAsStringAsync();

        using var orderDoc = JsonDocument.Parse(orderJson);
        int productId  = orderDoc.RootElement.GetProperty("productId").GetInt32();
        int customerId = orderDoc.RootElement.GetProperty("customerId").GetInt32();

        // Step 2: 并行拿商品和客户
        var productTask  = client.GetStringAsync(
            $"http://product-service:8080/api/products/{productId}");
        var customerTask = client.GetStringAsync(
            $"http://customer-service:8080/api/Customers/{customerId}");

        await Task.WhenAll(productTask, customerTask);

        // Step 3: 直接拼接 JSON 字符串返回，完全避开序列化器冲突
        var combined = $"{{\"order\":{orderJson},\"product\":{await productTask},\"customer\":{await customerTask}}}";

        return Content(combined, "application/json");
    }
}