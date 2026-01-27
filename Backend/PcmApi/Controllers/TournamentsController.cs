using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi;
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
    public class TournamentsController : ControllerBase
    {
        private readonly PcmDbContext _context;
        private readonly IWalletService _walletService;
        private readonly IHubContext<PcmHub>? _hubContext;

        public TournamentsController(PcmDbContext context, IWalletService walletService, IHubContext<PcmHub>? hubContext = null)
        {
            _context = context;
            _walletService = walletService;
            _hubContext = hubContext;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
        {
            var format = Enum.Parse<TournamentFormat>(request.Format);

            var tournament = new Tournament
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Format = format,
                EntryFee = request.EntryFee,
                PrizePool = request.PrizePool,
                Status = TournamentStatus.Open,
                CreatedDate = DateTime.UtcNow
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return Ok(new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                Format = tournament.Format.ToString(),
                EntryFee = tournament.EntryFee,
                PrizePool = tournament.PrizePool,
                Status = tournament.Status.ToString(),
                ParticipantCount = 0
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetTournaments()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.Participants)
                .Select(t => new TournamentDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Format = t.Format.ToString(),
                    EntryFee = t.EntryFee,
                    PrizePool = t.PrizePool,
                    Status = t.Status.ToString(),
                    ParticipantCount = t.Participants.Count
                })
                .ToListAsync();

            return Ok(tournaments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTournament(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            return Ok(new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                Format = tournament.Format.ToString(),
                EntryFee = tournament.EntryFee,
                PrizePool = tournament.PrizePool,
                Status = tournament.Status.ToString(),
                ParticipantCount = tournament.Participants.Count
            });
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> JoinTournament(int id, [FromBody] JoinTournamentRequest request)
        {
            request ??= new JoinTournamentRequest();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return NotFound("Member not found");

            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
                return NotFound("Tournament not found");

            // Check if already registered
            var existing = await _context.TournamentParticipants
                .FirstOrDefaultAsync(p => p.TournamentId == id && p.MemberId == member.Id);
            if (existing != null)
                return BadRequest("Already registered in this tournament");

            // Check wallet balance
            if (member.WalletBalance < tournament.EntryFee)
                return BadRequest("Insufficient wallet balance");

            // Deduct entry fee
            var walletTx = await _walletService.ProcessPaymentAsync(
                member.Id,
                tournament.EntryFee,
                TransactionType.Payment,
                relatedBookingId: null,
                relatedTournamentId: id,
                description: $"Tournament entry fee - {tournament.Name}");

            if (walletTx == null)
                return BadRequest("Failed to process payment");

            // Register participant
            var participant = new TournamentParticipant
            {
                TournamentId = id,
                MemberId = member.Id,
                TeamName = request.TeamName,
                PaymentCompleted = true,
                RegisteredDate = DateTime.UtcNow
            };

            _context.TournamentParticipants.Add(participant);

            var notification = new Notification
            {
                ReceiverId = member.Id,
                Message = $"Registered for tournament: {tournament.Name}",
                Type = NotificationType.Success,
                CreatedDate = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"user-{member.UserId}").SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    receiverId = notification.ReceiverId,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    isRead = notification.IsRead,
                    createdDate = notification.CreatedDate
                });
            }

            return Ok(new { message = "Successfully registered for tournament" });
        }

        [HttpGet("{id}/standings")]
        public async Task<IActionResult> GetStandings(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .ThenInclude(p => p.Member)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            var standings = tournament.Participants
                .Select((p, index) => new
                {
                    position = index + 1,
                    memberId = p.Member!.Id,
                    memberName = p.Member.FullName,
                    teamName = p.TeamName,
                    rankLevel = p.Member.RankLevel
                })
                .ToList();

            return Ok(standings);
        }

        [HttpPost("{id}/generate-schedule")]
        [Authorize(Roles = "Admin,Referee")]
        public async Task<IActionResult> GenerateSchedule(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound();

            // Simple round-robin scheduler
            var participants = tournament.Participants.ToList();
            if (participants.Count < 2)
                return BadRequest("Need at least 2 participants to generate schedule");

            var matches = new List<Match>();

            for (int i = 0; i < participants.Count; i++)
            {
                for (int j = i + 1; j < participants.Count; j++)
                {
                    var match = new Match
                    {
                        TournamentId = id,
                        RoundName = "Group Stage",
                        Date = tournament.StartDate.AddDays(Random.Shared.Next((int)(tournament.EndDate - tournament.StartDate).TotalDays)),
                        Team1_Player1Id = participants[i].MemberId,
                        Team2_Player1Id = participants[j].MemberId,
                        Status = MatchStatus.Scheduled,
                        IsRanked = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    matches.Add(match);
                }
            }

            _context.Matches.AddRange(matches);
            tournament.Status = TournamentStatus.DrawCompleted;
            _context.Tournaments.Update(tournament);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Schedule generated", matchCount = matches.Count });
        }
    }
}
