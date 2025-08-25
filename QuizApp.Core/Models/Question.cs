using System.ComponentModel.DataAnnotations;

namespace QuizApp.Core.Models
{
    public enum QuestionDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public string? Hint { get; set; }
        
        public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;
        
        public int Points { get; set; } = 1;
        
        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; } = null!;
        
        // Navigation properties
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public virtual ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new List<SubmissionAnswer>();
    }
}
