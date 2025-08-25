using QuizApp.Core.DTOs;
using QuizApp.Core.Models;

namespace QuizApp.Core.Interfaces
{
    public interface IQuizService
    {
        Task<QuizResponse> GenerateQuizAsync(GenerateQuizRequest request, int userId);
        Task<QuizSubmissionResponse> SubmitQuizAsync(SubmitQuizRequest request, int userId);
        Task<HintResponse> GetHintAsync(HintRequest request);
        Task<QuizHistoryResponse> GetQuizHistoryAsync(QuizHistoryRequest request, int userId);
        Task<QuizResponse> RetryQuizAsync(RetryQuizRequest request, int userId);
        Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardRequest request);
        Task<Quiz?> GetQuizByIdAsync(int quizId);
        Task<QuizSubmission?> GetQuizSubmissionAsync(int submissionId);
    }
}
