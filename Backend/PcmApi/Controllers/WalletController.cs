using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;
using PcmApi.Services;
using System.Security.Claims;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly PcmDbContext _context;
        private readonly IWalletService _walletService;

        public WalletController(PcmDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> RequestDeposit([FromBody] DepositRequest request)
        {
            if (!int.TryParse(User.FindFirst("MemberId")?.Value, out var memberId))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
                if (member == null)
                    return NotFound("Member not found");
                memberId = member.Id;
            }

            var success = await _walletService.ProcessDepositAsync(memberId, request.Amount, request.Description, request.ProofImageUrl);
            if (!success)
                return BadRequest("Failed to process deposit request");

            return Ok(new { message = "Deposit request submitted for approval" });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound();

            return Ok(new { balance = member.WalletBalance });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound();

            var transactions = await _walletService.GetTransactionHistoryAsync(member.Id, pageNumber, pageSize);
            return Ok(transactions);
        }

        [HttpPut("approve/{transactionId}")]
        [HttpPut("/api/admin/wallet/approve/{transactionId}")]
        [Authorize(Roles = "Admin,Treasurer")]
        public async Task<IActionResult> ApproveDeposit(int transactionId)
        {
            var success = await _walletService.ApproveDepositAsync(transactionId);
            if (!success)
                return BadRequest("Failed to approve deposit");

            return Ok(new { message = "Deposit approved" });
        }

        [HttpPut("reject/{transactionId}")]
        [HttpPut("/api/admin/wallet/reject/{transactionId}")]
        [Authorize(Roles = "Admin,Treasurer")]
        public async Task<IActionResult> RejectDeposit(int transactionId)
        {
            var success = await _walletService.RejectDepositAsync(transactionId);
            if (!success)
                return BadRequest("Failed to reject deposit");

            return Ok(new { message = "Deposit rejected" });
        }
    }
}
