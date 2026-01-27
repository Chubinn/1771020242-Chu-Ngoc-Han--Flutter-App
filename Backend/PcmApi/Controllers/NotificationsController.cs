using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using System.Security.Claims;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly PcmDbContext _context;

        public NotificationsController(PcmDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
                return NotFound("Member not found");

            var notifications = await _context.Notifications
                .Where(n => n.ReceiverId == member.Id)
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate
                })
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
                return NotFound("Member not found");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.ReceiverId == member.Id);

            if (notification == null)
                return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Notification marked as read" });
        }

        private async Task<Models.Member?> GetCurrentMemberAsync()
        {
            var memberIdClaim = User.FindFirst("MemberId")?.Value;
            if (int.TryParse(memberIdClaim, out var memberId))
            {
                return await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        }
    }
}

