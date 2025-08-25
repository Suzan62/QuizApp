using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Core.DTOs;
using QuizApp.Core.Interfaces;

namespace QuizApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(IQuizService quizService, ILogger<QuizController> logger)
        {
            _quizService = quizService;
            _logger = logger;
        }

        /// <summary>
        /// Generate a new AI-powered quiz
        /// </summary>
        /// <param name="request">Quiz generation parameters</param>
        /// <returns>Generated quiz with questions and answers</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(QuizResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateQuiz([FromBody] GenerateQuizRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var quiz = await _quizService.GenerateQuizAsync(request, userId);
                
                _logger.LogInformation("Quiz generated for user {UserId}: Subject={Subject}, Grade={Grade}", 
                    userId, request.Subject, request.GradeLevel);
                
                return Ok(quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz");
                return StatusCode(500, new { message = "Error generating quiz" });
            }
        }

        /// <summary>
        /// Submit quiz answers for evaluation
        /// </summary>
        /// <param name="request">Quiz submission with answers</param>
        /// <returns>Quiz results with score and improvement suggestions</returns>
        [HttpPost("submit")]
        [ProducesResponseType(typeof(QuizSubmissionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var result = await _quizService.SubmitQuizAsync(request, userId);
                
                _logger.LogInformation("Quiz submitted by user {UserId}: QuizId={QuizId}, Score={Score}/{Total}",
                    userId, request.QuizId, result.Score, result.TotalPoints);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz");
                return StatusCode(500, new { message = "Error submitting quiz" });
            }
        }

        /// <summary>
        /// Get AI-generated hint for a specific question
        /// </summary>
        /// <param name="request">Question ID</param>
        /// <returns>Helpful hint for the question</returns>
        [HttpPost("hint")]
        [ProducesResponseType(typeof(HintResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHint([FromBody] HintRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var hint = await _quizService.GetHintAsync(request);
                
                _logger.LogInformation("Hint requested for question {QuestionId}", request.QuestionId);
                
                return Ok(hint);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hint");
                return StatusCode(500, new { message = "Error getting hint" });
            }
        }

        /// <summary>
        /// Retry a quiz with adaptive difficulty
        /// </summary>
        /// <param name="request">Quiz retry request</param>
        /// <returns>Quiz with adjusted difficulty based on past performance</returns>
        [HttpPost("retry")]
        [ProducesResponseType(typeof(QuizResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RetryQuiz([FromBody] RetryQuizRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                var quiz = await _quizService.RetryQuizAsync(request, userId);
                
                _logger.LogInformation("Quiz retry requested by user {UserId}: QuizId={QuizId}", 
                    userId, request.QuizId);
                
                return Ok(quiz);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying quiz");
                return StatusCode(500, new { message = "Error retrying quiz" });
            }
        }

        /// <summary>
        /// Get quiz history with filtering options
        /// </summary>
        /// <param name="request">History filter parameters</param>
        /// <returns>Paginated list of quiz attempts</returns>
        [HttpGet("history")]
        [ProducesResponseType(typeof(QuizHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetQuizHistory([FromQuery] QuizHistoryRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var history = await _quizService.GetQuizHistoryAsync(request, userId);
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz history");
                return StatusCode(500, new { message = "Error getting quiz history" });
            }
        }

        /// <summary>
        /// Get leaderboard for top performers
        /// </summary>
        /// <param name="request">Leaderboard filter parameters</param>
        /// <returns>Top performers ranking</returns>
        [HttpGet("leaderboard")]
        [ProducesResponseType(typeof(LeaderboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLeaderboard([FromQuery] LeaderboardRequest request)
        {
            try
            {
                var leaderboard = await _quizService.GetLeaderboardAsync(request);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                return StatusCode(500, new { message = "Error getting leaderboard" });
            }
        }

        /// <summary>
        /// Get quiz details by ID
        /// </summary>
        /// <param name="id">Quiz ID</param>
        /// <returns>Quiz details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetQuiz(int id)
        {
            try
            {
                var quiz = await _quizService.GetQuizByIdAsync(id);
                
                if (quiz == null)
                {
                    return NotFound(new { message = "Quiz not found" });
                }

                return Ok(new
                {
                    id = quiz.Id,
                    title = quiz.Title,
                    description = quiz.Description,
                    subject = quiz.Subject,
                    gradeLevel = quiz.GradeLevel,
                    duration = quiz.Duration,
                    questionCount = quiz.Questions.Count,
                    createdAt = quiz.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz {QuizId}", id);
                return StatusCode(500, new { message = "Error getting quiz" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            
            throw new UnauthorizedAccessException("Invalid user token");
        }
    }
}
