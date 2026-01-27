using Microsoft.AspNetCore.Identity;

namespace PcmApi.Models
{
    /// <summary>
    /// Member entity - represents a club member with wallet and tier system
    /// </summary>
    public class Member
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public double RankLevel { get; set; } = 0; // DUPR Rank
        public bool IsActive { get; set; } = true;

        // Wallet System
        public decimal WalletBalance { get; set; } = 0;
        public MemberTier Tier { get; set; } = MemberTier.Standard;
        public decimal TotalSpent { get; set; } = 0;
        public string? AvatarUrl { get; set; }

        // Identity
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }

        // Relations
        public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = new List<TournamentParticipant>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }

    /// <summary>
    /// Wallet transaction entity - tracks all financial movements
    /// </summary>
    public class WalletTransaction
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        // For traceability (Booking ID or Tournament ID)
        public int? RelatedBookingId { get; set; }
        public int? RelatedTournamentId { get; set; }

        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // For deposit requests
        public string? ProofImageUrl { get; set; }
    }

    /// <summary>
    /// News entity - for announcements and pinned content
    /// </summary>
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPinned { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Internal transaction categories for income/expense tracking
    /// </summary>
    public class TransactionCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TransactionCategoryType Type { get; set; } = TransactionCategoryType.Income;
    }

    /// <summary>
    /// Court entity - represents a pickleball court
    /// </summary>
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;  // e.g., "Sân 1", "Sân 2"
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public decimal PricePerHour { get; set; }

        // Relations
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    /// <summary>
    /// Booking entity - represents a court reservation
    /// </summary>
    public class Booking
    {
        public int Id { get; set; }
        public int CourtId { get; set; }
        public Court? Court { get; set; }

        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }

        public int? TransactionId { get; set; }
        public WalletTransaction? Transaction { get; set; }

        // Recurring booking
        public bool IsRecurring { get; set; } = false;
        public string? RecurrenceRule { get; set; }  // e.g., "Weekly;Tue,Thu"
        public int? ParentBookingId { get; set; }
        public Booking? ParentBooking { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // For slot holding (5 minutes)
        public DateTime? HoldExpiresAt { get; set; }
    }

    /// <summary>
    /// Tournament entity - represents a tournament
    /// </summary>
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TournamentFormat Format { get; set; } = TournamentFormat.RoundRobin;
        public decimal EntryFee { get; set; }
        public decimal PrizePool { get; set; }
        public TournamentStatus Status { get; set; } = TournamentStatus.Open;

        // Advanced settings as JSON
        public string? Settings { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Relations
        public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

    /// <summary>
    /// Tournament participant entity
    /// </summary>
    public class TournamentParticipant
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public string? TeamName { get; set; }
        public bool PaymentCompleted { get; set; } = false;
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Match entity - highly detailed for tracking scores and progression
    /// </summary>
    public class Match
    {
        public int Id { get; set; }
        public int? TournamentId { get; set; }
        public Tournament? Tournament { get; set; }

        public string RoundName { get; set; } = string.Empty;  // e.g., "Group A", "Quarter Final"
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }

        // Team 1
        public int Team1_Player1Id { get; set; }
        public Member? Team1_Player1 { get; set; }
        public int? Team1_Player2Id { get; set; }
        public Member? Team1_Player2 { get; set; }

        // Team 2
        public int Team2_Player1Id { get; set; }
        public Member? Team2_Player1 { get; set; }
        public int? Team2_Player2Id { get; set; }
        public Member? Team2_Player2 { get; set; }

        // Results
        public int Score1 { get; set; } = 0;
        public int Score2 { get; set; } = 0;
        public string? Details { get; set; }  // JSON: {"sets": ["11-9", "5-11", "11-8"]}
        public int WinningSide { get; set; } = 0;  // 1 for Team1, 2 for Team2
        public bool IsRanked { get; set; } = false;

        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Background reminder tracking
        public DateTime? ReminderSentAt { get; set; }
    }

    /// <summary>
    /// Notification entity - for real-time notifications
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public int ReceiverId { get; set; }
        public Member? Receiver { get; set; }

        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
