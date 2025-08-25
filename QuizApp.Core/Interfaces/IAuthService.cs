using QuizApp.Core.DTOs;
using QuizApp.Core.Models;

namespace QuizApp.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        string GenerateJwtToken(User user);
        Task<User?> ValidateTokenAsync(string token);
        Task<User?> GetUserByUsernameAsync(string username);
    }
}
