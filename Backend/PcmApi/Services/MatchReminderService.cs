using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PcmApi.Data;
using PcmApi.Models;
using PcmApi;

namespace PcmApi.Services
{
    /// <summary>
    /// Background service that reminds participants about scheduled matches roughly 1 day ahead.
    /// </summary>
    public class MatchReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MatchReminderService> _logger;

        public MatchReminderService(IServiceScopeFactory scopeFactory, ILogger<MatchReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while sending match reminders.");
                }

                // Check reminders every 30 minutes to reduce duplicate work.
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task SendRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PcmDbContext>();
            var hubContext = scope.ServiceProvider.GetService<IHubContext<PcmHub>>();

            var nowUtc = DateTime.UtcNow;
            var fromUtc = nowUtc.AddHours(23);
            var toUtc = nowUtc.AddHours(25);

            var matches = await context.Matches
                .Where(m =>
                    m.Status == MatchStatus.Scheduled &&
                    m.ReminderSentAt == null &&
                    m.StartTime >= fromUtc &&
                    m.StartTime <= toUtc)
                .ToListAsync(stoppingToken);

            if (!matches.Any())
                return;

            foreach (var match in matches)
            {
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

                var members = await context.Members
                    .Where(m => participantIds.Contains(m.Id))
                    .ToListAsync(stoppingToken);

                var reminderMessage = $"Reminder: match tomorrow ({match.RoundName}) at {match.StartTime:HH:mm}";
                var notifications = members.Select(m => new Notification
                {
                    ReceiverId = m.Id,
                    Message = reminderMessage,
                    Type = NotificationType.Info,
                    CreatedDate = DateTime.UtcNow
                }).ToList();

                if (notifications.Any())
                    context.Notifications.AddRange(notifications);

                match.ReminderSentAt = DateTime.UtcNow;
                context.Matches.Update(match);

                await context.SaveChangesAsync(stoppingToken);

                if (hubContext != null)
                {
                    foreach (var member in members)
                    {
                        await hubContext.Clients.Group($"user-{member.UserId}").SendAsync("ReceiveNotification", new
                        {
                            receiverId = member.Id,
                            message = reminderMessage,
                            type = NotificationType.Info.ToString(),
                            createdDate = DateTime.UtcNow
                        }, stoppingToken);
                    }
                }
            }
        }
    }
}

