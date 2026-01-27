using Microsoft.AspNetCore.SignalR;

namespace PcmApi
{
    public class PcmHub : Hub
    {
        public async Task ReceiveNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        public async Task SendNotificationToUser(string userId, object payload)
        {
            await Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", payload);
        }

        public async Task UpdateCalendar(int courtId, int bookingId, string status)
        {
            // Notify all clients about calendar update
            await Clients.All.SendAsync("UpdateCalendar", new { courtId, bookingId, status });
        }

        public async Task UpdateMatchScore(int matchId, int score1, int score2, string roundName)
        {
            // Notify clients watching this match
            await Clients.Group($"match-{matchId}").SendAsync("UpdateMatchScore", new { matchId, score1, score2, roundName });
        }

        public async Task JoinMatchGroup(int matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");
        }

        public async Task LeaveMatchGroup(int matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match-{matchId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnConnectedAsync();
        }
    }
}
