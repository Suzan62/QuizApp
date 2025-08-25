using System.ComponentModel.DataAnnotations;
using QuizApp.Core.Models;

namespace QuizApp.Core.DTOs
{
    public class GenerateQuizRequest
    {
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string GradeLevel { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 50, ErrorMessage = "Number of questions must be between 1 and 50")]
        public int NumberOfQuestions { get; set; }
        
        public string? SpecificTopics { get; set; }
    }

    public class QuizResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string GradeLevel { get; set; } = string.Empty;
        public int Duration { get; set; }
        public List<QuestionResponse> Questions { get; set; } = new();
    }

    public class QuestionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public QuestionDifficulty Difficulty { get; set; }
        public int Points { get; set; }
        public List<AnswerResponse> Answers { get; set; } = new();
    }

    public class AnswerResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        // Note: IsCorrect should not be exposed to prevent cheating
    }

    public class SubmitQuizRequest
    {
        [Required]
        public int QuizId { get; set; }
        
        [Required]
        public List<QuestionAnswerSubmission> Answers { get; set; } = new();
    }

    public class QuestionAnswerSubmission
    {
        [Required]
        public int QuestionId { get; set; }
        
        public int? SelectedAnswerId { get; set; }
    }

    public class QuizSubmissionResponse
    {
        public int SubmissionId { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public double Percentage { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> ImprovementSuggestions { get; set; } = new();
        public List<QuestionResult> QuestionResults { get; set; } = new();
    }

    public class QuestionResult
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? YourAnswer { get; set; }
    }

    public class HintRequest
    {
        [Required]
        public int QuestionId { get; set; }
    }

    public class HintResponse
    {
        public string Hint { get; set; } = string.Empty;
    }
}
