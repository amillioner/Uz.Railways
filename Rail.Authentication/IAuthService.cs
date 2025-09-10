using Rail.Data.Models;
using System.Threading.Tasks;

namespace Rail.Authentication;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<User?> GetUserByUsernameAsync(string username);
    string GenerateJwtToken(User user);
    bool VerifyPassword(string password, string hash);
    string HashPassword(string password);
}