using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuizApp.Core.DTOs;
using QuizApp.Core.Interfaces;
using QuizApp.Core.Models;
using QuizApp.Infrastructure.Data;
using System.Net.Http;
using System.Text;

namespace QuizApp.Infrastructure.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;
        private readonly QuizAppDbContext _context;
        private readonly string _groqApiKey;
        private readonly string _groqBaseUrl = "https://api.groq.com/openai/v1/chat/completion";

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger, QuizAppDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _groqApiKey = _configuration["GroqApiKey"] ?? "gsk-placeholder-key"; // Fallback for demo
        }

        public async Task<Quiz> GenerateQuizAsync(GenerateQuizRequest request, int userId)
        {
            try
            {
                // Check user's past performance for adaptive difficulty
                var userPerformance = await GetUserPerformanceAsync(userId, request.Subject, request.GradeLevel);
                
                var prompt = CreateQuizGenerationPrompt(request, userPerformance);
                
                var aiResponse = await CallGroqAPIAsync(prompt);
                var quiz = ParseQuizFromResponse(aiResponse, request);
                
                // Save to database
                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();
                
                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz");
                return CreateFallbackQuiz(request);
            }
        }

        public async Task<string> GenerateHintAsync(Question question)
        {
            try
            {
                if (!string.IsNullOrEmpty(question.Hint))
                    return question.Hint;

                var prompt = $"Generate a helpful hint for this question without giving away the answer directly: '{question.Text}'. The hint should guide the student to think about the solution approach.";
                
                var hint = await CallGroqAPIAsync(prompt);
                
                // Update question with generated hint
                question.Hint = hint;
                _context.Questions.Update(question);
                await _context.SaveChangesAsync();
                
                return hint;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hint for question {QuestionId}", question.Id);
                return "Think about the key concepts related to this topic and consider the context of the question.";
            }
        }

        public async Task<List<string>> GenerateImprovementSuggestionsAsync(QuizSubmission submission)
        {
            try
            {
                var incorrectAnswers = await _context.SubmissionAnswers
                    .Include(sa => sa.Question)
                    .Where(sa => sa.QuizSubmissionId == submission.Id && !sa.IsCorrect)
                    .ToListAsync();

                if (!incorrectAnswers.Any())
                    return new List<string> { "Great job! You answered all questions correctly." };

                var topics = string.Join(", ", incorrectAnswers.Select(ia => ia.Question.Text).Take(3));
                var prompt = $"Based on these incorrect answers in a {submission.Quiz.Subject} quiz for grade {submission.Quiz.GradeLevel}, provide 2 specific study suggestions: {topics}";
                
                var response = await CallGroqAPIAsync(prompt);
                
                return response.Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating improvement suggestions");
                return new List<string>
                {
                    "Review the topics you missed and practice similar problems.",
                    "Focus on understanding the fundamental concepts before moving to advanced topics."
                };
            }
        }

        public async Task<List<Question>> AdjustQuestionDifficultyAsync(List<Question> questions, int userId, string subject, string gradeLevel)
        {
            var userPerformance = await GetUserPerformanceAsync(userId, subject, gradeLevel);
            
            foreach (var question in questions)
            {
                question.Difficulty = userPerformance.AveragePercentage switch
                {
                    >= 80 => QuestionDifficulty.Hard,
                    >= 60 => QuestionDifficulty.Medium,
                    _ => QuestionDifficulty.Easy
                };

                question.Points = (int)question.Difficulty;
            }

            return questions;
        }

        private async Task<string> CallGroqAPIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    model = "llama3-8b-8192",
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

                var response = await _httpClient.PostAsync($"{_groqBaseUrl}/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Groq API call failed with status {StatusCode}", response.StatusCode);
                    return GetFallbackResponse(prompt);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseJson);
                
                return result?.choices?[0]?.message?.content?.ToString() ?? GetFallbackResponse(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Groq API");
                return GetFallbackResponse(prompt);
            }
        }

        private string GetFallbackResponse(string prompt)
        {
            if (prompt.Contains("hint"))
                return "Consider the key concepts and think step by step.";
            
            if (prompt.Contains("improvement"))
                return "1. Review the fundamentals\n2. Practice more problems";
            
            return "Sample response for: " + prompt.Substring(0, Math.Min(50, prompt.Length));
        }

        private string CreateQuizGenerationPrompt(GenerateQuizRequest request, UserPerformance performance)
        {
            var difficultyLevel = performance.AveragePercentage >= 80 ? "challenging" : 
                                performance.AveragePercentage >= 60 ? "medium" : "easy";

            return $@"Generate a {difficultyLevel} {request.Subject} quiz for grade {request.GradeLevel} with {request.NumberOfQuestions} multiple choice questions.
            {(string.IsNullOrEmpty(request.SpecificTopics) ? "" : $"Focus on: {request.SpecificTopics}")}
            
            Format the response as JSON with this structure:
            {{
                ""title"": ""Quiz Title"",
                ""description"": ""Quiz Description"",
                ""questions"": [
                    {{
                        ""text"": ""Question text"",
                        ""difficulty"": ""Easy|Medium|Hard"",
                        ""points"": 1,
                        ""answers"": [
                            {{""text"": ""Answer 1"", ""isCorrect"": true}},
                            {{""text"": ""Answer 2"", ""isCorrect"": false}},
                            {{""text"": ""Answer 3"", ""isCorrect"": false}},
                            {{""text"": ""Answer 4"", ""isCorrect"": false}}
                        ]
                    }}
                ]
            }}";
        }

        private Quiz ParseQuizFromResponse(string aiResponse, GenerateQuizRequest request)
        {
            try
            {
                // Try to parse AI response as JSON
                var quizData = JsonConvert.DeserializeObject<dynamic>(aiResponse);
                
                var quiz = new Quiz
                {
                    Title = quizData?.title?.ToString() ?? $"{request.Subject} Quiz - Grade {request.GradeLevel}",
                    Description = quizData?.description?.ToString() ?? $"AI generated quiz covering {request.Subject} topics",
                    Subject = request.Subject,
                    GradeLevel = request.GradeLevel,
                    Duration = request.NumberOfQuestions * 2, // 2 minutes per question
                    Questions = new List<Question>()
                };

                if (quizData?.questions != null)
                {
                    foreach (var questionData in quizData.questions)
                    {
                        var question = new Question
                        {
                            Text = questionData?.text?.ToString() ?? "Sample question",
                            Difficulty = Enum.TryParse<QuestionDifficulty>(questionData?.difficulty?.ToString(), out QuestionDifficulty diff) ? diff : QuestionDifficulty.Medium,
                            Points = (int)(questionData?.points ?? 1),
                            Answers = new List<Answer>()
                        };

                        if (questionData?.answers != null)
                        {
                            foreach (var answerData in questionData.answers)
                            {
                                question.Answers.Add(new Answer
                                {
                                    Text = answerData?.text?.ToString() ?? "Sample answer",
                                    IsCorrect = bool.TryParse(answerData?.isCorrect?.ToString(), out bool isCorrect) && isCorrect
                                });
                            }
                        }

                        quiz.Questions.Add(question);
                    }
                }

                return quiz;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response, using fallback");
                return CreateFallbackQuiz(request);
            }
        }

        private Quiz CreateFallbackQuiz(GenerateQuizRequest request)
        {
            var quiz = new Quiz
            {
                Title = $"{request.Subject} Quiz - Grade {request.GradeLevel}",
                Description = $"Practice quiz covering {request.Subject} topics for grade {request.GradeLevel}",
                Subject = request.Subject,
                GradeLevel = request.GradeLevel,
                Duration = request.NumberOfQuestions * 2,
                Questions = new List<Question>()
            };

            for (int i = 1; i <= request.NumberOfQuestions; i++)
            {
                var question = new Question
                {
                    Text = $"Sample {request.Subject} question {i} for grade {request.GradeLevel}",
                    Difficulty = QuestionDifficulty.Medium,
                    Points = 1,
                    Answers = new List<Answer>
                    {
                        new Answer { Text = "Correct answer", IsCorrect = true },
                        new Answer { Text = "Incorrect option 1", IsCorrect = false },
                        new Answer { Text = "Incorrect option 2", IsCorrect = false },
                        new Answer { Text = "Incorrect option 3", IsCorrect = false }
                    }
                };

                quiz.Questions.Add(question);
            }

            return quiz;
        }

        private async Task<UserPerformance> GetUserPerformanceAsync(int userId, string subject, string gradeLevel)
        {
            var submissions = await _context.QuizSubmissions
                .Include(qs => qs.Quiz)
                .Where(qs => qs.UserId == userId && 
                           qs.Quiz.Subject == subject && 
                           qs.Quiz.GradeLevel == gradeLevel &&
                           qs.CompletedAt.HasValue)
                .OrderByDescending(qs => qs.CompletedAt)
                .Take(5) // Last 5 attempts
                .ToListAsync();

            if (!submissions.Any())
            {
                return new UserPerformance { AveragePercentage = 50, QuizzesCompleted = 0 };
            }

            return new UserPerformance
            {
                AveragePercentage = submissions.Average(s => s.Percentage),
                QuizzesCompleted = submissions.Count
            };
        }
    }

    public class UserPerformance
    {
        public double AveragePercentage { get; set; }
        public int QuizzesCompleted { get; set; }
    }
}
