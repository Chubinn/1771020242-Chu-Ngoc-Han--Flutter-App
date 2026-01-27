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
    public class MembersController : ControllerBase
    {
        private readonly PcmDbContext _context;

        public MembersController(PcmDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetMembers([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Members
                .Include(m => m.User)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m =>
                    m.FullName.Contains(search) ||
                    (m.User != null && m.User.Email != null && m.User.Email.Contains(search)));
            }

            var members = await query
                .OrderByDescending(m => m.JoinDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MemberDto
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Email = m.User!.Email,
                    JoinDate = m.JoinDate,
                    RankLevel = m.RankLevel,
                    IsActive = m.IsActive,
                    WalletBalance = m.WalletBalance,
                    Tier = m.Tier.ToString(),
                    TotalSpent = m.TotalSpent,
                    AvatarUrl = m.AvatarUrl
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMember(int id)
        {
            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Bookings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return NotFound();

            return Ok(new MemberDto
            {
                Id = member.Id,
                FullName = member.FullName,
                Email = member.User?.Email,
                JoinDate = member.JoinDate,
                RankLevel = member.RankLevel,
                IsActive = member.IsActive,
                WalletBalance = member.WalletBalance,
                Tier = member.Tier.ToString(),
                TotalSpent = member.TotalSpent,
                AvatarUrl = member.AvatarUrl
            });
        }

        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetMemberProfile(int id)
        {
            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Bookings)
                .Include(m => m.TournamentParticipants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return NotFound();

            var profile = new
            {
                member = new MemberDto
                {
                    Id = member.Id,
                    FullName = member.FullName,
                    Email = member.User?.Email,
                    JoinDate = member.JoinDate,
                    RankLevel = member.RankLevel,
                    IsActive = member.IsActive,
                    WalletBalance = member.WalletBalance,
                    Tier = member.Tier.ToString(),
                    TotalSpent = member.TotalSpent,
                    AvatarUrl = member.AvatarUrl
                },
                matchCount = member.Bookings.Count,
                tournamentCount = member.TournamentParticipants.Count
            };

            return Ok(profile);
        }
    }
}
