using System.ComponentModel.DataAnnotations;

namespace Trips.API.Models;

public enum TripStatus
{
    Active = 0,
    Completed = 1
}

public class Trip
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid BikeId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    public decimal? FinalCost { get; set; }
    public TripStatus Status { get; set; }
}
