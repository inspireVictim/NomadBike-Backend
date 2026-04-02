using Microsoft.AspNetCore.Mvc;
using NomadBike.Api.Data;
using NomadBike.Api.Models;
using NomadBike.Models;
using System.Linq;

namespace NomadBike.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(FakeDb.Trips);
        }

        [HttpPost("start/{bikeId}")]
        public IActionResult Start(int bikeId)
        {
            var bike = FakeDb.Bikes.FirstOrDefault(b => b.Id == bikeId);
            if (bike == null)
                return NotFound();

            var newId = FakeDb.Trips.Any() ? FakeDb.Trips.Max(t => t.Id) + 1 : 1;
            var trip = new Trip
            {
                Id = newId,
                BikeId = bikeId,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            bike.IsLocked = false;
            FakeDb.Trips.Add(trip);

            return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
        }

        [HttpPost("end/{id}")]
        public IActionResult End(int id)
        {
            var trip = FakeDb.Trips.FirstOrDefault(t => t.Id == id);
            if (trip == null)
                return NotFound();

            if (!trip.IsActive)
                return BadRequest("Trip already ended.");

            trip.EndTime = DateTime.UtcNow;
            trip.IsActive = false;

            var bike = FakeDb.Bikes.FirstOrDefault(b => b.Id == trip.BikeId);
            if (bike != null)
                bike.IsLocked = true;

            return Ok(trip);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var trip = FakeDb.Trips.FirstOrDefault(t => t.Id == id);
            if (trip == null)
                return NotFound();

            return Ok(trip);
        }
    }
}
