namespace CustomerService.Api.DTOs;

// Request DTO - used when creating a customer
public record CreateCustomerDto(
    string Name,
    string Email
);

// Response DTO - returned to clients (never expose the entity directly)
public record CustomerResponseDto(
    int Id,
    string Name,
    string Email
);