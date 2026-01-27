using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Models;
using System.Security.Claims;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly PcmDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(PcmDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalMembers = await _context.Members.CountAsync();
            var totalRevenue = await _context.WalletTransactions
                .Where(t => t.Type == TransactionType.Payment && t.Status == TransactionStatus.Completed)
                .SumAsync(t => -t.Amount);

            var totalBookings = await _context.Bookings.CountAsync();
            var totalTournaments = await _context.Tournaments.CountAsync();

            var monthlyRevenue = await _context.WalletTransactions
                .Where(t => t.Type == TransactionType.Payment && 
                       t.Status == TransactionStatus.Completed &&
                       t.CreatedDate.Month == DateTime.UtcNow.Month)
                .SumAsync(t => -t.Amount);

            return Ok(new
            {
                totalMembers,
                totalRevenue,
                totalBookings,
                totalTournaments,
                monthlyRevenue
            });
        }

        [HttpGet("pending-deposits")]
        public async Task<IActionResult> GetPendingDeposits()
        {
            var pendingDeposits = await _context.WalletTransactions
                .Include(t => t.Member)
                .Where(t => t.Type == TransactionType.Deposit && t.Status == TransactionStatus.Pending)
                .Select(t => new
                {
                    id = t.Id,
                    memberId = t.Member!.Id,
                    memberName = t.Member.FullName,
                    amount = t.Amount,
                    createdDate = t.CreatedDate,
                    proofImageUrl = t.ProofImageUrl
                })
                .ToListAsync();

            return Ok(pendingDeposits);
        }

        [HttpPost("create-admin/{email}")]
        public async Task<IActionResult> CreateAdmin(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found");

            var isAdmin = await _userManager.IsInRoleAsync(user, UserRoles.Admin);
            if (isAdmin)
                return BadRequest("User is already admin");

            var result = await _userManager.AddToRoleAsync(user, UserRoles.Admin);
            if (!result.Succeeded)
                return BadRequest("Failed to add admin role");

            return Ok(new { message = "Admin role assigned" });
        }

        [HttpPost("create-treasurer/{email}")]
        public async Task<IActionResult> CreateTreasurer(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound("User not found");

            var isTreasurer = await _userManager.IsInRoleAsync(user, UserRoles.Treasurer);
            if (isTreasurer)
                return BadRequest("User is already treasurer");

            var result = await _userManager.AddToRoleAsync(user, UserRoles.Treasurer);
            if (!result.Succeeded)
                return BadRequest("Failed to add treasurer role");

            return Ok(new { message = "Treasurer role assigned" });
        }

        [HttpGet("fund-status")]
        public async Task<IActionResult> GetFundStatus()
        {
            var totalDeposited = await _context.WalletTransactions
                .Where(t => t.Type == TransactionType.Deposit && t.Status == TransactionStatus.Completed)
                .SumAsync(t => t.Amount);

            var totalPaid = await _context.WalletTransactions
                .Where(t => t.Type == TransactionType.Payment && t.Status == TransactionStatus.Completed)
                .SumAsync(t => -t.Amount);

            var balance = totalDeposited - totalPaid;

            return Ok(new
            {
                totalDeposited,
                totalPaid,
                balance,
                status = balance >= 0 ? "Positive" : "Negative"
            });
        }
    }
}
