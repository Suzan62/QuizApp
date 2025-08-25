using Microsoft.EntityFrameworkCore;
using QuizApp.Core.Models;

namespace QuizApp.Infrastructure.Data
{
    public class QuizAppDbContext : DbContext
    {
        public QuizAppDbContext(DbContextOptions<QuizAppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuizSubmission> QuizSubmissions { get; set; }
        public DbSet<SubmissionAnswer> SubmissionAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255);
            });

            // Quiz configuration
            modelBuilder.Entity<Quiz>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Subject).HasMaxLength(100).IsRequired();
                entity.Property(e => e.GradeLevel).HasMaxLength(50).IsRequired();
            });

            // Question configuration
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Hint).HasMaxLength(500);
                entity.HasOne(e => e.Quiz)
                    .WithMany(e => e.Questions)
                    .HasForeignKey(e => e.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Answer configuration
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).HasMaxLength(500).IsRequired();
                entity.HasOne(e => e.Question)
                    .WithMany(e => e.Answers)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // QuizSubmission configuration
            modelBuilder.Entity<QuizSubmission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImprovementSuggestions).HasMaxLength(2000);
                entity.HasOne(e => e.User)
                    .WithMany(e => e.QuizSubmissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Quiz)
                    .WithMany(e => e.QuizSubmissions)
                    .HasForeignKey(e => e.QuizId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SubmissionAnswer configuration
            modelBuilder.Entity<SubmissionAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.QuizSubmission)
                    .WithMany(e => e.SubmissionAnswers)
                    .HasForeignKey(e => e.QuizSubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Question)
                    .WithMany(e => e.SubmissionAnswers)
                    .HasForeignKey(e => e.QuestionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.SelectedAnswer)
                    .WithMany()
                    .HasForeignKey(e => e.SelectedAnswerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed default users for testing
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "testuser",
                    Email = "test@example.com",
                    PasswordHash = "test123", // In real app, this would be hashed
                    CreatedAt = DateTime.SpecifyKind(DateTime.Parse("2025-01-01T00:00:00"), DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = "admin123", // In real app, this would be hashed
                    CreatedAt = DateTime.SpecifyKind(DateTime.Parse("2025-01-01T00:00:00"), DateTimeKind.Utc)
                }
            );
        }
    }
}
