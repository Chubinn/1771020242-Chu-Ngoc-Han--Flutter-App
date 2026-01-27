using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourtsController : ControllerBase
    {
        private readonly PcmDbContext _context;

        public CourtsController(PcmDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCourts()
        {
            var courts = await _context.Courts
                .Where(c => c.IsActive)
                .Select(c => new CourtDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive,
                    Description = c.Description,
                    PricePerHour = c.PricePerHour
                })
                .ToListAsync();

            return Ok(courts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourt(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court == null)
                return NotFound();

            return Ok(new CourtDto
            {
                Id = court.Id,
                Name = court.Name,
                IsActive = court.IsActive,
                Description = court.Description,
                PricePerHour = court.PricePerHour
            });
        }

        [HttpGet("{id}/bookings")]
        public async Task<IActionResult> GetCourtBookings(int id, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var now = DateTime.UtcNow;
            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.CourtId == id && 
                       b.Status != BookingStatus.Cancelled &&
                       (b.Status != BookingStatus.Holding || (b.HoldExpiresAt != null && b.HoldExpiresAt > now)) &&
                       b.StartTime >= from && b.EndTime <= to)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    CourtId = b.CourtId,
                    CourtName = b.Court!.Name,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status.ToString()
                })
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return Ok(bookings);
        }
    }
}
