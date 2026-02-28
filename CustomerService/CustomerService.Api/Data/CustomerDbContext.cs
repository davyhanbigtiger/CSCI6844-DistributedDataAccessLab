using CustomerService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Api.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
}
