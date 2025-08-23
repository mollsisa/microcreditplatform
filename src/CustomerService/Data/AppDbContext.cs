using Microsoft.EntityFrameworkCore;

namespace CustomerService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
    public DbSet<Customer> Customers => Set<Customer>();
}