namespace NomadBike.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public int BikeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? Price { get; set; }
        public bool IsActive { get; set; }
    }
}
