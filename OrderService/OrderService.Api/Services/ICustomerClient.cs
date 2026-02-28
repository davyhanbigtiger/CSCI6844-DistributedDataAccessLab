namespace OrderService.Api.Services;

public interface ICustomerClient
{
    Task<bool> CustomerExistsAsync(int customerId);
}
