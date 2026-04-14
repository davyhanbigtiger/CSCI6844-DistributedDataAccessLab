namespace ProductService.Api.DTOs;

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock
);

public record ProductResponseDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock
);