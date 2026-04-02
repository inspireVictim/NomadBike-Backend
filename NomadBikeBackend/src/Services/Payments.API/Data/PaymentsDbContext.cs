using Microsoft.EntityFrameworkCore;
using Payments.API.Models;

namespace Payments.API.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // UserId выступает в роли Primary Key
        modelBuilder.Entity<CustomerProfile>().HasKey(c => c.UserId);
        
        // Быстрый поиск транзакции по ID из Stripe для Webhook
        modelBuilder.Entity<PaymentTransaction>().HasIndex(t => t.StripePaymentIntentId).IsUnique();
    }
}
