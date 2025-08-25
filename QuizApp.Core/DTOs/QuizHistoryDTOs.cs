using System.ComponentModel.DataAnnotations;

namespace QuizApp.Core.DTOs
{
    public class QuizHistoryRequest
    {
        public string? Grade { get; set; }
        public string? Subject { get; set; }
        public int? MinMarks { get; set; }
        public int? MaxMarks { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class QuizHistoryResponse
    {
        public List<QuizHistoryItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class QuizHistoryItem
    {
        public int SubmissionId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public double Percentage { get; set; }
        public DateTime CompletedAt { get; set; }
        public bool CanRetry { get; set; }
    }

    public class RetryQuizRequest
    {
        [Required]
        public int QuizId { get; set; }
    }

    public class LeaderboardRequest
    {
        public string? Grade { get; set; }
        public string? Subject { get; set; }
        public int Top { get; set; } = 10;
    }

    public class LeaderboardResponse
    {
        public List<LeaderboardEntry> Entries { get; set; } = new();
        public string? Grade { get; set; }
        public string? Subject { get; set; }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public double AveragePercentage { get; set; }
        public int QuizzesCompleted { get; set; }
    }
}
