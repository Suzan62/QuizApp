namespace QuizApp.Core.Models
{
    public class SubmissionAnswer
    {
        public int Id { get; set; }
        
        public int QuizSubmissionId { get; set; }
        public virtual QuizSubmission QuizSubmission { get; set; } = null!;
        
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; } = null!;
        
        public int? SelectedAnswerId { get; set; }
        public virtual Answer? SelectedAnswer { get; set; }
        
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
    }
}
