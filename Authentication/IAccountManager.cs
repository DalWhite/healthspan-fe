using Healthspan.Authentication.Models;

namespace Healthspan.Authentication;
public interface IAccountManagement
{
    Task<FormResult> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<FormResult> RegisterAsync(string email, string password);
    Task<bool> CheckAuthenticatedAsync();
    Task<FormResult> RefreshToken();
}