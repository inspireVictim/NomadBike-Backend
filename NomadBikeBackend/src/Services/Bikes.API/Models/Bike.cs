using System.ComponentModel.DataAnnotations;

namespace Bikes.API.Models;

public enum BikeStatus
{
    Available = 0,
    InUse = 1,
    Maintenance = 2,
    Offline = 3
}

public class Bike
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string SerialNumber { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    [Range(0, 100)]
    public int BatteryLevel { get; set; }
    
    public BikeStatus Status { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
