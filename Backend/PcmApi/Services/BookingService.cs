using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;
using PcmApi;

namespace PcmApi.Services
{
    public interface IBookingService
    {
        Task<BookingDto?> CreateBookingAsync(int memberId, CreateBookingRequest request);
        Task<BookingDto?> HoldSlotAsync(int memberId, HoldSlotRequest request);
        Task<List<BookingDto>> GetMemberBookingsAsync(int memberId);
        Task<List<BookingDto>> GetCourtBookingsAsync(int courtId, DateTime from, DateTime to);
        Task<bool> CancelBookingAsync(int bookingId, int memberId);
        Task<bool> CreateRecurringBookingsAsync(int memberId, CreateRecurringBookingRequest request);
        Task<bool> ReleaseExpiredHoldsAsync();
    }

    public class BookingService : IBookingService
    {
        private readonly PcmDbContext _context;
        private readonly IWalletService _walletService;
        private readonly IHubContext<PcmHub>? _hubContext;

        public BookingService(PcmDbContext context, IWalletService walletService, IHubContext<PcmHub>? hubContext = null)
        {
            _context = context;
            _walletService = walletService;
            _hubContext = hubContext;
        }

        public async Task<BookingDto?> CreateBookingAsync(int memberId, CreateBookingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate court exists
                var court = await _context.Courts.FindAsync(request.CourtId);
                if (court == null || !court.IsActive)
                    return null;

                if (request.EndTime <= request.StartTime)
                    return null;

                var now = DateTime.UtcNow;

                // Check for overlapping bookings
                var hasConflict = await ActiveBookingsForCourt(request.CourtId, memberId, now)
                    .Where(b => b.StartTime < request.EndTime && b.EndTime > request.StartTime)
                    .AnyAsync();

                if (hasConflict)
                    return null;

                // Calculate price
                var duration = request.EndTime - request.StartTime;
                var totalPrice = (decimal)duration.TotalHours * court.PricePerHour;
                if (totalPrice <= 0)
                    return null;

                // Check wallet balance
                var balance = await _walletService.GetWalletBalanceAsync(memberId);
                if (balance < totalPrice)
                    return null;

                var memberUserId = await _context.Members
                    .Where(m => m.Id == memberId)
                    .Select(m => m.UserId)
                    .FirstOrDefaultAsync();

                // Reuse existing hold by the same member if it matches the slot.
                var existingHold = await _context.Bookings.FirstOrDefaultAsync(b =>
                    b.CourtId == request.CourtId &&
                    b.MemberId == memberId &&
                    b.Status == BookingStatus.Holding &&
                    b.HoldExpiresAt != null &&
                    b.HoldExpiresAt > now &&
                    b.StartTime < request.EndTime &&
                    b.EndTime > request.StartTime);

                var booking = existingHold ?? new Booking
                {
                    CourtId = request.CourtId,
                    MemberId = memberId,
                    CreatedDate = now
                };

                booking.StartTime = request.StartTime;
                booking.EndTime = request.EndTime;
                booking.TotalPrice = totalPrice;
                booking.Status = BookingStatus.PendingPayment;
                booking.HoldExpiresAt = null;

                if (existingHold == null)
                    _context.Bookings.Add(booking);

                // Persist to get booking Id for transaction traceability.
                await _context.SaveChangesAsync();

                // Process payment and link to the booking.
                var walletTx = await _walletService.ProcessPaymentAsync(
                    memberId,
                    totalPrice,
                    TransactionType.Payment,
                    relatedBookingId: booking.Id,
                    relatedTournamentId: null,
                    description: $"Court booking - {court.Name}");

                if (walletTx == null)
                    return null;

                booking.TransactionId = walletTx.Id;
                booking.Status = BookingStatus.Confirmed;
                _context.Bookings.Update(booking);

                var notification = new Notification
                {
                    ReceiverId = memberId,
                    Message = $"Booking confirmed: {court.Name} ({booking.StartTime:g} - {booking.EndTime:t})",
                    Type = NotificationType.Success,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                    {
                        courtId = booking.CourtId,
                        bookingId = booking.Id,
                        status = booking.Status.ToString()
                    });
                    if (!string.IsNullOrEmpty(memberUserId))
                    {
                        await _hubContext.Clients.Group($"user-{memberUserId}").SendAsync("ReceiveNotification", new
                        {
                            id = notification.Id,
                            receiverId = notification.ReceiverId,
                            message = notification.Message,
                            type = notification.Type.ToString(),
                            isRead = notification.IsRead,
                            createdDate = notification.CreatedDate
                        });
                    }
                }

                return new BookingDto
                {
                    Id = booking.Id,
                    CourtId = booking.CourtId,
                    CourtName = court.Name,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status.ToString(),
                    IsRecurring = booking.IsRecurring,
                    TransactionId = booking.TransactionId
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<BookingDto?> HoldSlotAsync(int memberId, HoldSlotRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var court = await _context.Courts.FindAsync(request.CourtId);
                if (court == null || !court.IsActive)
                    return null;

                if (request.EndTime <= request.StartTime)
                    return null;

                var now = DateTime.UtcNow;
                var hasConflict = await ActiveBookingsForCourt(request.CourtId, memberId, now)
                    .Where(b => b.StartTime < request.EndTime && b.EndTime > request.StartTime)
                    .AnyAsync();

                if (hasConflict)
                    return null;

                var duration = request.EndTime - request.StartTime;
                var totalPrice = (decimal)duration.TotalHours * court.PricePerHour;
                if (totalPrice <= 0)
                    return null;

                var booking = new Booking
                {
                    CourtId = request.CourtId,
                    MemberId = memberId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TotalPrice = totalPrice,
                    Status = BookingStatus.Holding,
                    HoldExpiresAt = now.AddMinutes(5),
                    CreatedDate = now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                    {
                        courtId = booking.CourtId,
                        bookingId = booking.Id,
                        status = booking.Status.ToString(),
                        holdExpiresAt = booking.HoldExpiresAt
                    });
                }

                return new BookingDto
                {
                    Id = booking.Id,
                    CourtId = booking.CourtId,
                    CourtName = court.Name,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    TotalPrice = booking.TotalPrice,
                    Status = booking.Status.ToString(),
                    IsRecurring = booking.IsRecurring,
                    TransactionId = booking.TransactionId
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<List<BookingDto>> GetMemberBookingsAsync(int memberId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.MemberId == memberId && b.Status != BookingStatus.Cancelled)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            return bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                CourtId = b.CourtId,
                CourtName = b.Court?.Name ?? "Unknown",
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                IsRecurring = b.IsRecurring,
                TransactionId = b.TransactionId
            }).ToList();
        }

        public async Task<List<BookingDto>> GetCourtBookingsAsync(int courtId, DateTime from, DateTime to)
        {
            var now = DateTime.UtcNow;
            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.CourtId == courtId &&
                       b.Status != BookingStatus.Cancelled &&
                       (b.Status != BookingStatus.Holding || (b.HoldExpiresAt != null && b.HoldExpiresAt > now)) &&
                       b.StartTime >= from && b.EndTime <= to)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                CourtId = b.CourtId,
                CourtName = b.Court?.Name ?? "Unknown",
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                IsRecurring = b.IsRecurring,
                TransactionId = b.TransactionId
            }).ToList();
        }

        public async Task<bool> CancelBookingAsync(int bookingId, int memberId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null || booking.MemberId != memberId)
                    return false;

                if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                    return false;

                // Holding slots have no payment - just release them.
                if (booking.Status == BookingStatus.Holding)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.HoldExpiresAt = null;
                    _context.Bookings.Update(booking);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    if (_hubContext != null)
                    {
                        await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                        {
                            courtId = booking.CourtId,
                            bookingId = booking.Id,
                            status = booking.Status.ToString()
                        });
                    }

                    return true;
                }

                // Check if cancellation is before 24 hours
                var hoursUntilBooking = (booking.StartTime - DateTime.UtcNow).TotalHours;
                var refundPercentage = hoursUntilBooking > 24 ? 1.0m : 0.5m;
                var refundAmount = booking.TotalPrice * refundPercentage;

                // Process refund
                var member = await _context.Members.FindAsync(memberId);
                if (member != null)
                {
                    member.WalletBalance += refundAmount;
                    member.TotalSpent = Math.Max(0, member.TotalSpent - refundAmount);
                    UpdateMemberTier(member);
                    _context.Members.Update(member);

                    var refundTransaction = new WalletTransaction
                    {
                        MemberId = memberId,
                        Amount = refundAmount,
                        Type = TransactionType.Refund,
                        Status = TransactionStatus.Completed,
                        Description = $"Refund for booking cancellation",
                        RelatedBookingId = bookingId,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.WalletTransactions.Add(refundTransaction);

                    var notification = new Notification
                    {
                        ReceiverId = memberId,
                        Message = $"Booking cancelled. Refund: {refundAmount:N0} VND",
                        Type = NotificationType.Warning,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }

                booking.Status = BookingStatus.Cancelled;
                _context.Bookings.Update(booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                    {
                        courtId = booking.CourtId,
                        bookingId = booking.Id,
                        status = booking.Status.ToString()
                    });
                }
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CreateRecurringBookingsAsync(int memberId, CreateRecurringBookingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var court = await _context.Courts.FindAsync(request.CourtId);
                if (court == null || !court.IsActive)
                    return false;

                if (request.Duration <= TimeSpan.Zero)
                    return false;

                var startDate = request.StartTime.Date;
                var endDate = request.EndDate.Date;
                if (endDate < startDate)
                    return false;

                var recurrenceDays = ParseRecurrenceDays(request.RecurrenceRule, request.StartTime.DayOfWeek);
                var now = DateTime.UtcNow;

                var occurrences = new List<(DateTime start, DateTime end, decimal price)>();

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (!recurrenceDays.Contains(date.DayOfWeek))
                        continue;

                    var start = date.Add(request.StartTime.TimeOfDay);
                    var end = start.Add(request.Duration);
                    if (end <= start)
                        return false;

                    var hasConflict = await ActiveBookingsForCourt(request.CourtId, memberId, now)
                        .Where(b => b.StartTime < end && b.EndTime > start)
                        .AnyAsync();
                    if (hasConflict)
                        return false;

                    var price = (decimal)request.Duration.TotalHours * court.PricePerHour;
                    occurrences.Add((start, end, price));
                }

                if (!occurrences.Any())
                    return false;

                var totalPrice = occurrences.Sum(o => o.price);
                var balance = await _walletService.GetWalletBalanceAsync(memberId);
                if (balance < totalPrice)
                    return false;

                var bookings = occurrences.Select(o => new Booking
                {
                    CourtId = request.CourtId,
                    MemberId = memberId,
                    StartTime = o.start,
                    EndTime = o.end,
                    TotalPrice = o.price,
                    IsRecurring = true,
                    RecurrenceRule = request.RecurrenceRule,
                    Status = BookingStatus.PendingPayment,
                    CreatedDate = now
                }).ToList();

                _context.Bookings.AddRange(bookings);
                await _context.SaveChangesAsync();

                foreach (var booking in bookings)
                {
                    var walletTx = await _walletService.ProcessPaymentAsync(
                        memberId,
                        booking.TotalPrice,
                        TransactionType.Payment,
                        relatedBookingId: booking.Id,
                        relatedTournamentId: null,
                        description: $"Recurring booking - {court.Name} ({booking.StartTime:g})");

                    if (walletTx == null)
                        return false;

                    booking.TransactionId = walletTx.Id;
                    booking.Status = BookingStatus.Confirmed;
                }

                var notification = new Notification
                {
                    ReceiverId = memberId,
                    Message = $"Recurring bookings created: {bookings.Count} slots",
                    Type = NotificationType.Success,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                _context.Bookings.UpdateRange(bookings);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (_hubContext != null)
                {
                    foreach (var booking in bookings)
                    {
                        await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                        {
                            courtId = booking.CourtId,
                            bookingId = booking.Id,
                            status = booking.Status.ToString()
                        });
                    }
                }

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> ReleaseExpiredHoldsAsync()
        {
            var now = DateTime.UtcNow;
            var expiredHolds = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Holding && b.HoldExpiresAt != null && b.HoldExpiresAt < now)
                .ToListAsync();

            foreach (var booking in expiredHolds)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.HoldExpiresAt = null;
            }

            if (expiredHolds.Any())
            {
                _context.Bookings.UpdateRange(expiredHolds);
                await _context.SaveChangesAsync();

                if (_hubContext != null)
                {
                    foreach (var booking in expiredHolds)
                    {
                        await _hubContext.Clients.All.SendAsync("UpdateCalendar", new
                        {
                            courtId = booking.CourtId,
                            bookingId = booking.Id,
                            status = booking.Status.ToString()
                        });
                    }
                }
            }

            return true;
        }

        private IQueryable<Booking> ActiveBookingsForCourt(int courtId, int memberId, DateTime nowUtc)
        {
            return _context.Bookings.Where(b =>
                b.CourtId == courtId &&
                b.Status != BookingStatus.Cancelled &&
                (b.Status != BookingStatus.Holding || (b.HoldExpiresAt != null && b.HoldExpiresAt > nowUtc)) &&
                !(b.Status == BookingStatus.Holding && b.MemberId == memberId));
        }

        private static HashSet<DayOfWeek> ParseRecurrenceDays(string recurrenceRule, DayOfWeek fallbackDay)
        {
            var result = new HashSet<DayOfWeek>();
            if (string.IsNullOrWhiteSpace(recurrenceRule))
            {
                result.Add(fallbackDay);
                return result;
            }

            var parts = recurrenceRule.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var dayPart = parts.Length > 1 ? parts[1] : parts[0];
            var dayTokens = dayPart.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in dayTokens)
            {
                if (TryParseDayOfWeek(token, out var day))
                    result.Add(day);
            }

            if (!result.Any())
                result.Add(fallbackDay);

            return result;
        }

        private static bool TryParseDayOfWeek(string token, out DayOfWeek day)
        {
            switch (token.Trim().ToLowerInvariant())
            {
                case "mon":
                case "monday":
                    day = DayOfWeek.Monday;
                    return true;
                case "tue":
                case "tues":
                case "tuesday":
                    day = DayOfWeek.Tuesday;
                    return true;
                case "wed":
                case "wednesday":
                    day = DayOfWeek.Wednesday;
                    return true;
                case "thu":
                case "thur":
                case "thurs":
                case "thursday":
                    day = DayOfWeek.Thursday;
                    return true;
                case "fri":
                case "friday":
                    day = DayOfWeek.Friday;
                    return true;
                case "sat":
                case "saturday":
                    day = DayOfWeek.Saturday;
                    return true;
                case "sun":
                case "sunday":
                    day = DayOfWeek.Sunday;
                    return true;
                default:
                    day = fallback;
                    return false;
            }
        }

        private static readonly DayOfWeek fallback = DayOfWeek.Monday;

        private static void UpdateMemberTier(Member member)
        {
            if (member.TotalSpent >= 10000000)
                member.Tier = MemberTier.Diamond;
            else if (member.TotalSpent >= 5000000)
                member.Tier = MemberTier.Gold;
            else if (member.TotalSpent >= 2000000)
                member.Tier = MemberTier.Silver;
            else
                member.Tier = MemberTier.Standard;
        }
    }
}
