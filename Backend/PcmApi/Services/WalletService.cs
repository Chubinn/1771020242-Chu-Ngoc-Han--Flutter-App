using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;
using PcmApi;

namespace PcmApi.Services
{
    public interface IWalletService
    {
        Task<bool> ProcessDepositAsync(int memberId, decimal amount, string description, string? proofImageUrl);
        Task<WalletTransaction?> ProcessPaymentAsync(
            int memberId,
            decimal amount,
            TransactionType type,
            int? relatedBookingId,
            int? relatedTournamentId,
            string description);
        Task<bool> ApproveDepositAsync(int transactionId);
        Task<bool> RejectDepositAsync(int transactionId);
        Task<decimal> GetWalletBalanceAsync(int memberId);
        Task<List<WalletTransactionDto>> GetTransactionHistoryAsync(int memberId, int pageNumber = 1, int pageSize = 20);
    }

    public class WalletService : IWalletService
    {
        private readonly PcmDbContext _context;
        private readonly IHubContext<PcmHub>? _hubContext;

        public WalletService(PcmDbContext context, IHubContext<PcmHub>? hubContext = null)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<bool> ProcessDepositAsync(int memberId, decimal amount, string description, string? proofImageUrl)
        {
            if (amount <= 0)
                return false;

            var startedTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = startedTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null)
                    return false;

                var walletTransaction = new WalletTransaction
                {
                    MemberId = memberId,
                    Amount = amount,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Pending,
                    Description = description,
                    ProofImageUrl = proofImageUrl,
                    CreatedDate = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(walletTransaction);
                await _context.SaveChangesAsync();
                if (startedTransaction && transaction != null)
                    await transaction.CommitAsync();
                return true;
            }
            catch
            {
                if (startedTransaction && transaction != null)
                    await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<WalletTransaction?> ProcessPaymentAsync(
            int memberId,
            decimal amount,
            TransactionType type,
            int? relatedBookingId,
            int? relatedTournamentId,
            string description)
        {
            if (amount <= 0)
                return null;

            var startedTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = startedTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var member = await _context.Members.FindAsync(memberId);
                if (member == null || member.WalletBalance < amount)
                    return null;

                // Deduct from wallet
                member.WalletBalance -= amount;
                member.TotalSpent += amount;

                // Create transaction record
                var walletTransaction = new WalletTransaction
                {
                    MemberId = memberId,
                    Amount = -amount,
                    Type = type,
                    Status = TransactionStatus.Completed,
                    Description = description,
                    RelatedBookingId = relatedBookingId,
                    RelatedTournamentId = relatedTournamentId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(walletTransaction);
                _context.Members.Update(member);

                // Update member tier based on total spent
                UpdateMemberTier(member);

                await _context.SaveChangesAsync();
                if (startedTransaction && transaction != null)
                    await transaction.CommitAsync();
                return walletTransaction;
            }
            catch
            {
                if (startedTransaction && transaction != null)
                    await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<bool> ApproveDepositAsync(int transactionId)
        {
            var startedTransaction = _context.Database.CurrentTransaction == null;
            using var transaction = startedTransaction ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                var walletTransaction = await _context.WalletTransactions
                    .Include(t => t.Member)
                    .FirstOrDefaultAsync(t => t.Id == transactionId);

                if (walletTransaction == null ||
                    walletTransaction.Type != TransactionType.Deposit ||
                    walletTransaction.Status != TransactionStatus.Pending)
                    return false;

                walletTransaction.Status = TransactionStatus.Completed;
                walletTransaction.Member!.WalletBalance += walletTransaction.Amount;

                var notification = new Notification
                {
                    ReceiverId = walletTransaction.Member.Id,
                    Message = $"Deposit approved: {walletTransaction.Amount:N0} VND",
                    Type = NotificationType.Success,
                    CreatedDate = DateTime.UtcNow
                };

                _context.WalletTransactions.Update(walletTransaction);
                _context.Members.Update(walletTransaction.Member);
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                if (startedTransaction && transaction != null)
                    await transaction.CommitAsync();

                if (_hubContext != null)
                {
                    await _hubContext.Clients
                        .Group($"user-{walletTransaction.Member.UserId}")
                        .SendAsync("ReceiveNotification", new
                        {
                            id = notification.Id,
                            receiverId = notification.ReceiverId,
                            message = notification.Message,
                            type = notification.Type.ToString(),
                            isRead = notification.IsRead,
                            createdDate = notification.CreatedDate
                        });
                }

                return true;
            }
            catch
            {
                if (startedTransaction && transaction != null)
                    await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> RejectDepositAsync(int transactionId)
        {
            var walletTransaction = await _context.WalletTransactions.FindAsync(transactionId);
            if (walletTransaction == null ||
                walletTransaction.Type != TransactionType.Deposit ||
                walletTransaction.Status != TransactionStatus.Pending)
                return false;

            walletTransaction.Status = TransactionStatus.Rejected;
            _context.WalletTransactions.Update(walletTransaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetWalletBalanceAsync(int memberId)
        {
            var member = await _context.Members.FindAsync(memberId);
            return member?.WalletBalance ?? 0;
        }

        public async Task<List<WalletTransactionDto>> GetTransactionHistoryAsync(int memberId, int pageNumber = 1, int pageSize = 20)
        {
            var transactions = await _context.WalletTransactions
                .Where(t => t.MemberId == memberId)
                .OrderByDescending(t => t.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return transactions.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Description = t.Description,
                CreatedDate = t.CreatedDate
            }).ToList();
        }

        private void UpdateMemberTier(Member member)
        {
            // Auto-update tier based on total spent
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
