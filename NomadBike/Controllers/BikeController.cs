using Microsoft.AspNetCore.Mvc;
using NomadBike.Api.Data;
using NomadBike.Api.Models;

namespace NomadBike.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BikeController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(FakeDb.Bikes);
        }

        [HttpPost("unlock/{id}")]
        public IActionResult Unlock(int id)
        {
            var bike = FakeDb.Bikes.FirstOrDefault(x => x.Id == id);

            if (bike == null)
                return NotFound();

            bike.IsLocked = false;

            return Ok(bike);
        }

        [HttpPost("lock/{id}")]
        public IActionResult Lock(int id)
        {
            var bike = FakeDb.Bikes.FirstOrDefault(x => x.Id == id);

            if (bike == null)
                return NotFound();

            bike.IsLocked = true;

            return Ok(bike);
        }
    }
}