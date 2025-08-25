using System.ComponentModel.DataAnnotations;

namespace QuizApp.Core.Models
{
    public class QuizSubmission
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; } = null!;
        
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public double Percentage { get; set; }
        
        public string? ImprovementSuggestions { get; set; }
        
        // Navigation properties
        public virtual ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new List<SubmissionAnswer>();
    }
}
