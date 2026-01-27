using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PcmApi.Models;

namespace PcmApi.Data
{
    public class PcmDbContext : IdentityDbContext<IdentityUser>
    {
        public PcmDbContext(DbContextOptions<PcmDbContext> options) : base(options)
        {
        }

        // DbSets for business entities
        public DbSet<Member> Members { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<TransactionCategory> TransactionCategories { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names with 250_ prefix
            modelBuilder.Entity<Member>().ToTable("250_Members");
            modelBuilder.Entity<WalletTransaction>().ToTable("250_WalletTransactions");
            modelBuilder.Entity<News>().ToTable("250_News");
            modelBuilder.Entity<TransactionCategory>().ToTable("250_TransactionCategories");
            modelBuilder.Entity<Court>().ToTable("250_Courts");
            modelBuilder.Entity<Booking>().ToTable("250_Bookings");
            modelBuilder.Entity<Tournament>().ToTable("250_Tournaments");
            modelBuilder.Entity<TournamentParticipant>().ToTable("250_TournamentParticipants");
            modelBuilder.Entity<Match>().ToTable("250_Matches");
            modelBuilder.Entity<Notification>().ToTable("250_Notifications");

            // Member configurations
            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.FullName).IsRequired().HasMaxLength(255);
                entity.Property(m => m.WalletBalance).HasColumnType("decimal(18,2)");
                entity.Property(m => m.TotalSpent).HasColumnType("decimal(18,2)");
                entity.HasIndex(m => m.UserId).IsUnique();

                // Foreign key to IdentityUser
                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relations
                entity.HasMany(m => m.WalletTransactions)
                    .WithOne(t => t.Member)
                    .HasForeignKey(t => t.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(m => m.Bookings)
                    .WithOne(b => b.Member)
                    .HasForeignKey(b => b.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(m => m.Notifications)
                    .WithOne(n => n.Receiver)
                    .HasForeignKey(n => n.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WalletTransaction configurations
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
                entity.Property(t => t.Description).HasMaxLength(500);

                // Foreign key
                entity.HasOne(t => t.Member)
                    .WithMany(m => m.WalletTransactions)
                    .HasForeignKey(t => t.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(t => t.CreatedDate);
            });

            // News configurations
            modelBuilder.Entity<News>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title).IsRequired().HasMaxLength(255);
                entity.Property(n => n.Content).IsRequired();
            });

            // TransactionCategory configurations
            modelBuilder.Entity<TransactionCategory>(entity =>
            {
                entity.HasKey(tc => tc.Id);
                entity.Property(tc => tc.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(tc => new { tc.Name, tc.Type }).IsUnique();
            });

            // Court configurations
            modelBuilder.Entity<Court>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.PricePerHour).HasColumnType("decimal(18,2)");
            });

            // Booking configurations
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");

                // Foreign keys
                entity.HasOne(b => b.Court)
                    .WithMany(c => c.Bookings)
                    .HasForeignKey(b => b.CourtId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Member)
                    .WithMany(m => m.Bookings)
                    .HasForeignKey(b => b.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => new { b.CourtId, b.StartTime, b.EndTime });
                entity.HasIndex(b => b.Status);
            });

            // Tournament configurations
            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(255);
                entity.Property(t => t.EntryFee).HasColumnType("decimal(18,2)");
                entity.Property(t => t.PrizePool).HasColumnType("decimal(18,2)");
            });

            // TournamentParticipant configurations
            modelBuilder.Entity<TournamentParticipant>(entity =>
            {
                entity.HasKey(tp => tp.Id);
                entity.Property(tp => tp.TeamName).HasMaxLength(255);

                entity.HasOne(tp => tp.Tournament)
                    .WithMany(t => t.Participants)
                    .HasForeignKey(tp => tp.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tp => tp.Member)
                    .WithMany(m => m.TournamentParticipants)
                    .HasForeignKey(tp => tp.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(tp => new { tp.TournamentId, tp.MemberId }).IsUnique();
            });

            // Match configurations
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.RoundName).IsRequired().HasMaxLength(100);

                entity.HasOne(m => m.Tournament)
                    .WithMany(t => t.Matches)
                    .HasForeignKey(m => m.TournamentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Team 1 relations
                entity.HasOne(m => m.Team1_Player1)
                    .WithMany()
                    .HasForeignKey(m => m.Team1_Player1Id)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(m => m.Team1_Player2)
                    .WithMany()
                    .HasForeignKey(m => m.Team1_Player2Id)
                    .OnDelete(DeleteBehavior.NoAction);

                // Team 2 relations
                entity.HasOne(m => m.Team2_Player1)
                    .WithMany()
                    .HasForeignKey(m => m.Team2_Player1Id)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(m => m.Team2_Player2)
                    .WithMany()
                    .HasForeignKey(m => m.Team2_Player2Id)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(m => m.Date);
            });

            // Notification configurations
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Message).IsRequired();

                entity.HasOne(n => n.Receiver)
                    .WithMany(m => m.Notifications)
                    .HasForeignKey(n => n.ReceiverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(n => new { n.ReceiverId, n.IsRead });
            });
        }
    }
}
