namespace PcmApi.Models
{
    /// <summary>
    /// Tier levels for club members
    /// </summary>
    public enum MemberTier
    {
        Standard = 0,
        Silver = 1,
        Gold = 2,
        Diamond = 3
    }

    /// <summary>
    /// Status of wallet transactions
    /// </summary>
    public enum TransactionStatus
    {
        Pending = 0,
        Completed = 1,
        Rejected = 2,
        Failed = 3
    }

    /// <summary>
    /// Type of wallet transactions
    /// </summary>
    public enum TransactionType
    {
        Deposit = 0,
        Withdraw = 1,
        Payment = 2,
        Refund = 3,
        Reward = 4
    }

    /// <summary>
    /// Status of bookings
    /// </summary>
    public enum BookingStatus
    {
        PendingPayment = 0,
        Confirmed = 1,
        Cancelled = 2,
        Completed = 3,
        Holding = 4  // Hold for 5 minutes
    }

    /// <summary>
    /// Tournament format types
    /// </summary>
    public enum TournamentFormat
    {
        RoundRobin = 0,
        Knockout = 1,
        Hybrid = 2
    }

    /// <summary>
    /// Tournament status
    /// </summary>
    public enum TournamentStatus
    {
        Open = 0,
        Registering = 1,
        DrawCompleted = 2,
        Ongoing = 3,
        Finished = 4
    }

    /// <summary>
    /// Match status
    /// </summary>
    public enum MatchStatus
    {
        Scheduled = 0,
        InProgress = 1,
        Finished = 2
    }

    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Internal transaction categories (income/expense)
    /// </summary>
    public enum TransactionCategoryType
    {
        Income = 0,
        Expense = 1
    }

    /// <summary>
    /// User roles
    /// </summary>
    public class UserRoles
    {
        public const string Admin = "Admin";
        public const string Treasurer = "Treasurer";
        public const string Referee = "Referee";
        public const string Member = "Member";
    }
}
