using System.Net;

namespace OrderService.Api.Services;

public class CustomerClient : ICustomerClient
{
    private readonly HttpClient _httpClient;

    public CustomerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CustomerExistsAsync(int customerId)
    {
        var response = await _httpClient.GetAsync($"api/customers/{customerId}");
        return response.StatusCode == HttpStatusCode.OK;
    }
}
