using Microsoft.EntityFrameworkCore;
using Trips.API.Models;

namespace Trips.API.Data;

public class TripsDbContext : DbContext
{
    public TripsDbContext(DbContextOptions<TripsDbContext> options) : base(options) { }

    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Индекс для поиска активных поездок (чтобы один пользователь не мог взять 2 велосипеда)
        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.UserId, t.Status });
            
        // Индекс для защиты от аренды одного велосипеда дважды
        modelBuilder.Entity<Trip>()
            .HasIndex(t => new { t.BikeId, t.Status });
    }
}
