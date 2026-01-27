using Microsoft.AspNetCore.Identity;
using PcmApi.Models;

namespace PcmApi.Data
{
    public class DbSeeder
    {
        public static async Task SeedAsync(PcmDbContext context, IServiceProvider serviceProvider)
        {
            try
            {
                // Track whether core member data already exists.
                var hasMembers = context.Members.Any();

                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Create roles
                var roles = new[] { UserRoles.Admin, UserRoles.Treasurer, UserRoles.Referee, UserRoles.Member };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                // Seed transaction categories even if members already exist.
                if (!context.TransactionCategories.Any())
                {
                    var categories = new[]
                    {
                        new TransactionCategory { Name = "Membership Fees", Type = TransactionCategoryType.Income },
                        new TransactionCategory { Name = "Court Booking", Type = TransactionCategoryType.Income },
                        new TransactionCategory { Name = "Tournament Entry", Type = TransactionCategoryType.Income },
                        new TransactionCategory { Name = "Maintenance", Type = TransactionCategoryType.Expense },
                        new TransactionCategory { Name = "Rewards", Type = TransactionCategoryType.Expense }
                    };
                    context.TransactionCategories.AddRange(categories);
                    await context.SaveChangesAsync();
                }

                // If members are already present, skip the heavy demo seeding.
                if (hasMembers)
                    return;

                // Create Admin user
                var admin = new IdentityUser { Email = "admin@pcm.com", UserName = "admin@pcm.com", EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, UserRoles.Admin);

                // Create admin member profile
                var adminMember = new Member
                {
                    UserId = admin.Id,
                    FullName = "Admin User",
                    JoinDate = DateTime.UtcNow,
                    WalletBalance = 50000000,
                    Tier = MemberTier.Diamond,
                    TotalSpent = 20000000,
                    RankLevel = 4.8
                };
                context.Members.Add(adminMember);

                // Create Treasurer user
                var treasurer = new IdentityUser { Email = "treasurer@pcm.com", UserName = "treasurer@pcm.com", EmailConfirmed = true };
                await userManager.CreateAsync(treasurer, "Treasurer@123");
                await userManager.AddToRoleAsync(treasurer, UserRoles.Treasurer);

                var treasurerMember = new Member
                {
                    UserId = treasurer.Id,
                    FullName = "Treasurer User",
                    JoinDate = DateTime.UtcNow,
                    WalletBalance = 30000000,
                    Tier = MemberTier.Gold,
                    TotalSpent = 10000000,
                    RankLevel = 4.2
                };
                context.Members.Add(treasurerMember);

                // Create Referee user
                var referee = new IdentityUser { Email = "referee@pcm.com", UserName = "referee@pcm.com", EmailConfirmed = true };
                await userManager.CreateAsync(referee, "Referee@123");
                await userManager.AddToRoleAsync(referee, UserRoles.Referee);

                var refereeMember = new Member
                {
                    UserId = referee.Id,
                    FullName = "Referee User",
                    JoinDate = DateTime.UtcNow,
                    WalletBalance = 5000000,
                    Tier = MemberTier.Silver,
                    TotalSpent = 3000000,
                    RankLevel = 3.5
                };
                context.Members.Add(refereeMember);

                // Create sample members
                var memberCount = 20;
                for (int i = 1; i <= memberCount; i++)
                {
                    var user = new IdentityUser { Email = $"member{i}@pcm.com", UserName = $"member{i}@pcm.com", EmailConfirmed = true };
                    await userManager.CreateAsync(user, "Member@123");
                    await userManager.AddToRoleAsync(user, UserRoles.Member);

                    var member = new Member
                    {
                        UserId = user.Id,
                        FullName = $"Member {i}",
                        JoinDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365)),
                        WalletBalance = Random.Shared.Next(1000000, 10000000),
                        Tier = (MemberTier)(Random.Shared.Next(0, 4)),
                        TotalSpent = Random.Shared.Next(500000, 20000000),
                        RankLevel = Random.Shared.Next(20, 48) / 10.0
                    };
                    context.Members.Add(member);
                }

                // Create courts
                var courts = new[]
                {
                    new Court { Name = "Sân 1", PricePerHour = 150000, Description = "Sân ngoài trời chất lượng cao", IsActive = true },
                    new Court { Name = "Sân 2", PricePerHour = 150000, Description = "Sân ngoài trời chất lượng cao", IsActive = true },
                    new Court { Name = "Sân 3", PricePerHour = 200000, Description = "Sân trong nhà có điều hòa", IsActive = true },
                    new Court { Name = "Sân 4", PricePerHour = 200000, Description = "Sân trong nhà có điều hòa", IsActive = true }
                };
                context.Courts.AddRange(courts);

                // Create news/announcements
                var news = new[]
                {
                    new News
                    {
                        Title = "Khai mạc Giải Summer Open 2026",
                        Content = "Giải đấu lớn nhất năm sẽ diễn ra từ tháng 6-8. Hãy đăng ký ngay!",
                        IsPinned = true,
                        CreatedDate = DateTime.UtcNow
                    },
                    new News
                    {
                        Title = "Thay đổi giờ hoạt động CLB",
                        Content = "Từ ngày 1/2, CLB sẽ hoạt động từ 6h sáng đến 22h tối.",
                        IsPinned = true,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new News
                    {
                        Title = "Khuyến mãi tháng 2",
                        Content = "Giảm 20% giá sân cho tất cả thành viên. Hơn nữa, nạp tiền trên 500k được thưởng thêm 50k.",
                        IsPinned = false,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    }
                };
                context.News.AddRange(news);

                // Create tournaments
                var tournaments = new[]
                {
                    new Tournament
                    {
                        Name = "Summer Open 2026",
                        StartDate = new DateTime(2026, 6, 1),
                        EndDate = new DateTime(2026, 8, 31),
                        Format = TournamentFormat.RoundRobin,
                        EntryFee = 500000,
                        PrizePool = 50000000,
                        Status = TournamentStatus.Finished,
                        CreatedDate = DateTime.UtcNow.AddMonths(-1)
                    },
                    new Tournament
                    {
                        Name = "Winter Cup 2026",
                        StartDate = new DateTime(2026, 12, 1),
                        EndDate = new DateTime(2026, 12, 31),
                        Format = TournamentFormat.Knockout,
                        EntryFee = 300000,
                        PrizePool = 30000000,
                        Status = TournamentStatus.Registering,
                        CreatedDate = DateTime.UtcNow
                    }
                };
                context.Tournaments.AddRange(tournaments);

                await context.SaveChangesAsync();

                // Add tournament participants
                var tournament = context.Tournaments.First();
                var members = context.Members.Where(m => m.FullName.StartsWith("Member")).Take(8).ToList();

                foreach (var member in members)
                {
                    var participant = new TournamentParticipant
                    {
                        TournamentId = tournament.Id,
                        MemberId = member.Id,
                        TeamName = $"Team {member.FullName}",
                        PaymentCompleted = true,
                        RegisteredDate = DateTime.UtcNow.AddDays(-10)
                    };
                    context.TournamentParticipants.Add(participant);
                }

                // Add matches for finished tournament
                if (members.Count >= 4)
                {
                    var match = new Match
                    {
                        TournamentId = tournament.Id,
                        RoundName = "Final",
                        Date = DateTime.UtcNow.AddDays(-5),
                        StartTime = DateTime.UtcNow.AddDays(-5).AddHours(15),
                        Team1_Player1Id = members[0].Id,
                        Team1_Player2Id = members[1].Id,
                        Team2_Player1Id = members[2].Id,
                        Team2_Player2Id = members[3].Id,
                        Score1 = 2,
                        Score2 = 1,
                        Details = "[\"11-9\", \"8-11\", \"11-7\"]",
                        WinningSide = 1,
                        IsRanked = true,
                        Status = MatchStatus.Finished
                    };
                    context.Matches.Add(match);
                }

                // Add wallet transactions
                foreach (var member in context.Members.Take(5))
                {
                    var transaction = new WalletTransaction
                    {
                        MemberId = member.Id,
                        Amount = 2000000,
                        Type = TransactionType.Deposit,
                        Status = TransactionStatus.Completed,
                        Description = "Nạp tiền vào ví",
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    };
                    context.WalletTransactions.Add(transaction);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeding error: {ex.Message}");
            }
        }
    }
}
