using Bikes.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Bikes.API.Data;

public class BikesDbContext : DbContext
{
    public BikesDbContext(DbContextOptions<BikesDbContext> options) : base(options)
    {
    }

    public DbSet<Bike> Bikes => Set<Bike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Индексы для быстрого поиска по локации и статусу
        modelBuilder.Entity<Bike>()
            .HasIndex(b => new { b.Status, b.Latitude, b.Longitude });
            
        modelBuilder.Entity<Bike>()
            .HasIndex(b => b.SerialNumber)
            .IsUnique();
    }
}
