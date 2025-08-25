using QuizApp.Core.DTOs;
using QuizApp.Core.Models;

namespace QuizApp.Core.Interfaces
{
    public interface IAIService
    {
        Task<Quiz> GenerateQuizAsync(GenerateQuizRequest request, int userId);
        Task<string> GenerateHintAsync(Question question);
        Task<List<string>> GenerateImprovementSuggestionsAsync(QuizSubmission submission);
        Task<List<Question>> AdjustQuestionDifficultyAsync(List<Question> questions, int userId, string subject, string gradeLevel);
    }
}
