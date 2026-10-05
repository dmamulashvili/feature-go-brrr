using Microsoft.EntityFrameworkCore;
using OrderShop.Api.Database.Entities;

namespace OrderShop.Api.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();

        // One invoice per order, enforced by the database as a last line of defense.
        modelBuilder.Entity<Invoice>().HasIndex(i => i.OrderId).IsUnique();
    }
}
