using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Services;
using System.Security.Claims;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly PcmDbContext _context;
        private readonly IBookingService _bookingService;

        public BookingsController(PcmDbContext context, IBookingService bookingService)
        {
            _context = context;
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound("Member not found");

            var booking = await _bookingService.CreateBookingAsync(member.Id, request);
            if (booking == null)
                return BadRequest("Failed to create booking");

            return Ok(booking);
        }

        [HttpPost("hold")]
        public async Task<IActionResult> HoldSlot([FromBody] HoldSlotRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound("Member not found");

            var hold = await _bookingService.HoldSlotAsync(member.Id, request);
            if (hold == null)
                return BadRequest("Failed to hold slot");

            return Ok(hold);
        }

        [HttpPost("recurring")]
        public async Task<IActionResult> CreateRecurringBooking([FromBody] CreateRecurringBookingRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound();

            // Check if member is VIP (Gold or Diamond tier)
            var tier = member.Tier.ToString();
            if (tier != "Gold" && tier != "Diamond")
                return Forbid("Only VIP members can create recurring bookings");

            var success = await _bookingService.CreateRecurringBookingsAsync(member.Id, request);
            if (!success)
                return BadRequest("Failed to create recurring bookings");

            return Ok(new { message = "Recurring bookings created successfully" });
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound();

            var bookings = await _bookingService.GetMemberBookingsAsync(member.Id);
            return Ok(bookings);
        }

        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar([FromQuery] int? courtId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (courtId.HasValue)
            {
                var bookings = await _bookingService.GetCourtBookingsAsync(courtId.Value, from, to);
                return Ok(bookings);
            }

            var activeCourts = await _context.Courts
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToListAsync();

            var allBookings = new List<BookingDto>();
            foreach (var id in activeCourts)
            {
                var bookings = await _bookingService.GetCourtBookingsAsync(id, from, to);
                allBookings.AddRange(bookings);
            }

            return Ok(allBookings.OrderBy(b => b.StartTime));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> CancelBookingDelete(int id) => CancelBookingInternal(id);

        [HttpPost("cancel/{id}")]
        public Task<IActionResult> CancelBookingPost(int id) => CancelBookingInternal(id);

        private async Task<IActionResult> CancelBookingInternal(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound();

            var success = await _bookingService.CancelBookingAsync(id, member.Id);
            if (!success)
                return BadRequest("Failed to cancel booking");

            return Ok(new { message = "Booking cancelled successfully" });
        }
    }
}
