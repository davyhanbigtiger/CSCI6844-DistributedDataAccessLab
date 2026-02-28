namespace OrderService.Api.Services;

public interface IProductClient
{
    Task<bool> ProductExistsAsync(int productId);
}
