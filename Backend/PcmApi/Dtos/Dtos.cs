using PcmApi.Models;

namespace PcmApi.Dtos
{
    // Auth DTOs
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    // Member DTOs
    public class UserDto
    {
        public int MemberId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; }
        public string Tier { get; set; } = string.Empty;
        public double RankLevel { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class MemberDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime JoinDate { get; set; }
        public double RankLevel { get; set; }
        public bool IsActive { get; set; }
        public decimal WalletBalance { get; set; }
        public string Tier { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public string? AvatarUrl { get; set; }
    }

    // Wallet DTOs
    public class DepositRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ProofImageUrl { get; set; }
    }

    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    // Court DTOs
    public class CourtDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public decimal PricePerHour { get; set; }
    }

    // Booking DTOs
    public class CreateBookingRequest
    {
        public int CourtId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class HoldSlotRequest
    {
        public int CourtId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class CreateRecurringBookingRequest
    {
        public int CourtId { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string RecurrenceRule { get; set; } = string.Empty;  // e.g., "Weekly;Tue,Thu"
        public DateTime EndDate { get; set; }
    }

    public class BookingDto
    {
        public int Id { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public int? TransactionId { get; set; }
    }

    // Tournament DTOs
    public class CreateTournamentRequest
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Format { get; set; } = "RoundRobin";
        public decimal EntryFee { get; set; }
        public decimal PrizePool { get; set; }
    }

    public class TournamentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Format { get; set; } = string.Empty;
        public decimal EntryFee { get; set; }
        public decimal PrizePool { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
    }

    public class JoinTournamentRequest
    {
        public string? TeamName { get; set; }
    }

    // Match DTOs
    public class MatchResultRequest
    {
        public int Score1 { get; set; }
        public int Score2 { get; set; }
        public string? Details { get; set; }
        public int WinningSide { get; set; }
    }

    public class MatchDto
    {
        public int Id { get; set; }
        public int? TournamentId { get; set; }
        public string RoundName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Team1_Player1Name { get; set; } = string.Empty;
        public string? Team1_Player2Name { get; set; }
        public string Team2_Player1Name { get; set; } = string.Empty;
        public string? Team2_Player2Name { get; set; }
        public int Score1 { get; set; }
        public int Score2 { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Notification DTOs
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // Pagination
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
