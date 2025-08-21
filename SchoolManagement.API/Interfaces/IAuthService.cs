using SchoolManagement.API.DTOs;
using SchoolManagement.API.Models;

namespace SchoolManagement.API.Interfaces
{
    public interface IAuthService
    {
        Task<string> GenerateTokenAsync(User user);

        //public Task<string> AuthenticateAsync(string email, string password, int userId);
        Task<UserDto> RegisterAsync(Auth authUser);
        Task<LoginResultDto> LoginAsync(string email, string password);
        Task<UserDto> GetCurrentUserAsync();
    }
}
