using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PcmApi.Services
{
    /// <summary>
    /// Background service that releases expired 5-minute booking holds.
    /// </summary>
    public class BookingHoldCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingHoldCleanupService> _logger;

        public BookingHoldCleanupService(IServiceScopeFactory scopeFactory, ILogger<BookingHoldCleanupService> logger)
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
                    using var scope = _scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    await bookingService.ReleaseExpiredHoldsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while releasing expired booking holds.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

