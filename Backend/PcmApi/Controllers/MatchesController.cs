using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PcmApi;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;

namespace PcmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly PcmDbContext _context;
        private readonly IHubContext<PcmHub>? _hubContext;

        public MatchesController(PcmDbContext context, IHubContext<PcmHub>? hubContext = null)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatch(int id)
        {
            var match = await _context.Matches
                .Include(m => m.Team1_Player1)
                .Include(m => m.Team1_Player2)
                .Include(m => m.Team2_Player1)
                .Include(m => m.Team2_Player2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
                return NotFound();

            return Ok(new MatchDto
            {
                Id = match.Id,
                TournamentId = match.TournamentId,
                RoundName = match.RoundName,
                Date = match.Date,
                Team1_Player1Name = match.Team1_Player1?.FullName ?? "Unknown",
                Team1_Player2Name = match.Team1_Player2?.FullName,
                Team2_Player1Name = match.Team2_Player1?.FullName ?? "Unknown",
                Team2_Player2Name = match.Team2_Player2?.FullName,
                Score1 = match.Score1,
                Score2 = match.Score2,
                Status = match.Status.ToString()
            });
        }

        [HttpPost("{id}/result")]
        [Authorize(Roles = "Admin,Referee")]
        public async Task<IActionResult> UpdateMatchResult(int id, [FromBody] MatchResultRequest request)
        {
            var match = await _context.Matches
                .Include(m => m.Team1_Player1)
                .Include(m => m.Team1_Player2)
                .Include(m => m.Team2_Player1)
                .Include(m => m.Team2_Player2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
                return NotFound();

            match.Score1 = request.Score1;
            match.Score2 = request.Score2;
            match.Details = request.Details;
            match.WinningSide = request.WinningSide;
            match.Status = MatchStatus.Finished;

            // Update player ranks if needed
            if (match.IsRanked)
            {
                // Simple DUPR rank update logic (can be enhanced)
                var winner = request.WinningSide == 1 ? match.Team1_Player1 : match.Team2_Player1;
                var loser = request.WinningSide == 1 ? match.Team2_Player1 : match.Team1_Player1;

                if (winner != null)
                    winner.RankLevel += 1;
                if (loser != null)
                    loser.RankLevel = Math.Max(0, loser.RankLevel - 1);
            }

            _context.Matches.Update(match);
            await _context.SaveChangesAsync();

            var participantIds = new[]
            {
                match.Team1_Player1Id,
                match.Team1_Player2Id,
                match.Team2_Player1Id,
                match.Team2_Player2Id
            }
            .Where(idValue => idValue.HasValue)
            .Select(idValue => idValue!.Value)
            .Distinct()
            .ToList();

            var members = await _context.Members
                .Where(m => participantIds.Contains(m.Id))
                .ToListAsync();

            var notifications = members.Select(m => new Notification
            {
                ReceiverId = m.Id,
                Message = $"Match finished ({match.RoundName}): {match.Score1}-{match.Score2}",
                Type = NotificationType.Info,
                CreatedDate = DateTime.UtcNow
            }).ToList();

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }

            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"match-{match.Id}").SendAsync("UpdateMatchScore", new
                {
                    matchId = match.Id,
                    score1 = match.Score1,
                    score2 = match.Score2,
                    roundName = match.RoundName,
                    status = match.Status.ToString()
                });

                foreach (var member in members)
                {
                    await _hubContext.Clients.Group($"user-{member.UserId}").SendAsync("ReceiveNotification", new
                    {
                        receiverId = member.Id,
                        message = $"Match finished ({match.RoundName}): {match.Score1}-{match.Score2}",
                        type = NotificationType.Info.ToString(),
                        createdDate = DateTime.UtcNow
                    });
                }
            }

            return Ok(new { message = "Match result updated" });
        }

        [HttpGet("tournament/{tournamentId}")]
        public async Task<IActionResult> GetTournamentMatches(int tournamentId)
        {
            var matches = await _context.Matches
                .Include(m => m.Team1_Player1)
                .Include(m => m.Team1_Player2)
                .Include(m => m.Team2_Player1)
                .Include(m => m.Team2_Player2)
                .Where(m => m.TournamentId == tournamentId)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    TournamentId = m.TournamentId,
                    RoundName = m.RoundName,
                    Date = m.Date,
                    Team1_Player1Name = m.Team1_Player1!.FullName,
                    Team1_Player2Name = m.Team1_Player2 != null ? m.Team1_Player2.FullName : null,
                    Team2_Player1Name = m.Team2_Player1!.FullName,
                    Team2_Player2Name = m.Team2_Player2 != null ? m.Team2_Player2.FullName : null,
                    Score1 = m.Score1,
                    Score2 = m.Score2,
                    Status = m.Status.ToString()
                })
                .ToListAsync();

            return Ok(matches);
        }
    }
}
