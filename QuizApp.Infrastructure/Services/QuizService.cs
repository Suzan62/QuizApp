using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizApp.Core.DTOs;
using QuizApp.Core.Interfaces;
using QuizApp.Core.Models;
using QuizApp.Infrastructure.Data;

namespace QuizApp.Infrastructure.Services
{
    public class QuizService : IQuizService
    {
        private readonly QuizAppDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<QuizService> _logger;

        public QuizService(
            QuizAppDbContext context,
            IAIService aiService,
            ILogger<QuizService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<QuizResponse> GenerateQuizAsync(GenerateQuizRequest request, int userId)
        {
            try
            {
                var quiz = await _aiService.GenerateQuizAsync(request, userId);
                
                var response = new QuizResponse
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    Subject = quiz.Subject,
                    GradeLevel = quiz.GradeLevel,
                    Duration = quiz.Duration,
                    Questions = quiz.Questions.Select(q => new QuestionResponse
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Difficulty = q.Difficulty,
                        Points = q.Points,
                        Answers = q.Answers.Select(a => new AnswerResponse
                        {
                            Id = a.Id,
                            Text = a.Text
                            // Note: IsCorrect is intentionally omitted to prevent cheating
                        }).ToList()
                    }).ToList()
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz");
                throw;
            }
        }

        public async Task<QuizSubmissionResponse> SubmitQuizAsync(SubmitQuizRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == request.QuizId);

                if (quiz == null)
                    throw new ArgumentException("Quiz not found");

                var submission = new QuizSubmission
                {
                    UserId = userId,
                    QuizId = request.QuizId,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _context.QuizSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                var submissionAnswers = new List<SubmissionAnswer>();
                var questionResults = new List<QuestionResult>();
                int totalScore = 0;
                int totalPoints = quiz.Questions.Sum(q => q.Points);

                foreach (var questionAnswer in request.Answers)
                {
                    var question = quiz.Questions.FirstOrDefault(q => q.Id == questionAnswer.QuestionId);
                    if (question == null) continue;

                    var selectedAnswer = questionAnswer.SelectedAnswerId.HasValue
                        ? question.Answers.FirstOrDefault(a => a.Id == questionAnswer.SelectedAnswerId)
                        : null;

                    var isCorrect = selectedAnswer?.IsCorrect ?? false;
                    var pointsEarned = isCorrect ? question.Points : 0;
                    totalScore += pointsEarned;

                    var submissionAnswer = new SubmissionAnswer
                    {
                        QuizSubmissionId = submission.Id,
                        QuestionId = questionAnswer.QuestionId,
                        SelectedAnswerId = questionAnswer.SelectedAnswerId,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned
                    };

                    submissionAnswers.Add(submissionAnswer);

                    var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
                    questionResults.Add(new QuestionResult
                    {
                        QuestionId = question.Id,
                        QuestionText = question.Text,
                        IsCorrect = isCorrect,
                        PointsEarned = pointsEarned,
                        CorrectAnswer = correctAnswer?.Text,
                        YourAnswer = selectedAnswer?.Text
                    });
                }

                _context.SubmissionAnswers.AddRange(submissionAnswers);

                submission.Score = totalScore;
                submission.TotalPoints = totalPoints;
                submission.Percentage = totalPoints > 0 ? (double)totalScore / totalPoints * 100 : 0;

                _context.QuizSubmissions.Update(submission);
                await _context.SaveChangesAsync();

                // Generate AI improvement suggestions
                var improvementSuggestions = await _aiService.GenerateImprovementSuggestionsAsync(submission);

                await transaction.CommitAsync();

                return new QuizSubmissionResponse
                {
                    SubmissionId = submission.Id,
                    Score = submission.Score,
                    TotalPoints = submission.TotalPoints,
                    Percentage = submission.Percentage,
                    CompletedAt = submission.CompletedAt ?? DateTime.UtcNow,
                    ImprovementSuggestions = improvementSuggestions,
                    QuestionResults = questionResults
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting quiz");
                throw;
            }
        }

        public async Task<HintResponse> GetHintAsync(HintRequest request)
        {
            try
            {
                var question = await _context.Questions.FindAsync(request.QuestionId);
                if (question == null)
                    throw new ArgumentException("Question not found");

                var hint = await _aiService.GenerateHintAsync(question);

                return new HintResponse { Hint = hint };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hint for question {QuestionId}", request.QuestionId);
                throw;
            }
        }

        public async Task<QuizHistoryResponse> GetQuizHistoryAsync(QuizHistoryRequest request, int userId)
        {
            try
            {
                var query = _context.QuizSubmissions
                    .Include(qs => qs.Quiz)
                    .Where(qs => qs.UserId == userId && qs.CompletedAt.HasValue);

                if (!string.IsNullOrEmpty(request.Grade))
                    query = query.Where(qs => qs.Quiz.GradeLevel == request.Grade);

                if (!string.IsNullOrEmpty(request.Subject))
                    query = query.Where(qs => qs.Quiz.Subject == request.Subject);

                if (request.MinMarks.HasValue)
                    query = query.Where(qs => qs.Score >= request.MinMarks);

                if (request.MaxMarks.HasValue)
                    query = query.Where(qs => qs.Score <= request.MaxMarks);

                if (request.FromDate.HasValue)
                    query = query.Where(qs => qs.CompletedAt >= request.FromDate);

                if (request.ToDate.HasValue)
                    query = query.Where(qs => qs.CompletedAt <= request.ToDate);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var submissions = await query
                    .OrderByDescending(qs => qs.CompletedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var items = submissions.Select(qs => new QuizHistoryItem
                {
                    SubmissionId = qs.Id,
                    QuizTitle = qs.Quiz.Title,
                    Subject = qs.Quiz.Subject,
                    GradeLevel = qs.Quiz.GradeLevel,
                    Score = qs.Score,
                    TotalPoints = qs.TotalPoints,
                    Percentage = qs.Percentage,
                    CompletedAt = qs.CompletedAt ?? DateTime.UtcNow,
                    CanRetry = true
                }).ToList();

                return new QuizHistoryResponse
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz history for user {UserId}", userId);
                throw;
            }
        }

        public async Task<QuizResponse> RetryQuizAsync(RetryQuizRequest request, int userId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == request.QuizId);

                if (quiz == null)
                    throw new ArgumentException("Quiz not found");

                // Apply adaptive difficulty based on user's past performance
                var adjustedQuestions = await _aiService.AdjustQuestionDifficultyAsync(
                    quiz.Questions.ToList(), userId, quiz.Subject, quiz.GradeLevel);

                return new QuizResponse
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    Subject = quiz.Subject,
                    GradeLevel = quiz.GradeLevel,
                    Duration = quiz.Duration,
                    Questions = adjustedQuestions.Select(q => new QuestionResponse
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Difficulty = q.Difficulty,
                        Points = q.Points,
                        Answers = q.Answers.Select(a => new AnswerResponse
                        {
                            Id = a.Id,
                            Text = a.Text
                        }).ToList()
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying quiz {QuizId} for user {UserId}", request.QuizId, userId);
                throw;
            }
        }

        public async Task<LeaderboardResponse> GetLeaderboardAsync(LeaderboardRequest request)
        {
            try
            {
                var query = _context.QuizSubmissions
                    .Include(qs => qs.User)
                    .Include(qs => qs.Quiz)
                    .Where(qs => qs.CompletedAt.HasValue);

                if (!string.IsNullOrEmpty(request.Grade))
                    query = query.Where(qs => qs.Quiz.GradeLevel == request.Grade);

                if (!string.IsNullOrEmpty(request.Subject))
                    query = query.Where(qs => qs.Quiz.Subject == request.Subject);

                var leaderboardData = await query
                    .GroupBy(qs => qs.User)
                    .Select(g => new
                    {
                        User = g.Key,
                        TotalScore = g.Sum(qs => qs.Score),
                        AveragePercentage = g.Average(qs => qs.Percentage),
                        QuizzesCompleted = g.Count()
                    })
                    .OrderByDescending(x => x.AveragePercentage)
                    .ThenByDescending(x => x.TotalScore)
                    .Take(request.Top)
                    .ToListAsync();

                var entries = leaderboardData.Select((data, index) => new LeaderboardEntry
                {
                    Rank = index + 1,
                    Username = data.User.Username,
                    TotalScore = data.TotalScore,
                    AveragePercentage = Math.Round(data.AveragePercentage, 2),
                    QuizzesCompleted = data.QuizzesCompleted
                }).ToList();

                return new LeaderboardResponse
                {
                    Entries = entries,
                    Grade = request.Grade,
                    Subject = request.Subject
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                throw;
            }
        }

        public async Task<Quiz?> GetQuizByIdAsync(int quizId)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == quizId);
        }

        public async Task<QuizSubmission?> GetQuizSubmissionAsync(int submissionId)
        {
            return await _context.QuizSubmissions
                .Include(qs => qs.Quiz)
                .Include(qs => qs.User)
                .Include(qs => qs.SubmissionAnswers)
                .ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(qs => qs.Id == submissionId);
        }
    }
}
