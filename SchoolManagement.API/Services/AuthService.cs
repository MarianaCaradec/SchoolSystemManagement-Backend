using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.API.Data.Context;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SchoolManagement.API.Models.User;

namespace SchoolManagement.API.Services
{
    public class AuthService(IConfiguration configuration, SchoolSysDBContext context, 
        IPasswordHasher<User> passwordHasher, IUserService userService, IHttpContextAccessor httpContextAccessor) : IAuthService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly SchoolSysDBContext _context = context;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly IUserService _userService = userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public async Task<string> GenerateTokenAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null when generating token.");

            var secretKey = _configuration["JwtSecret:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSecret:ExpirationInMinutes"])),
                Issuer = _configuration["JwtSecret:Issuer"],
                Audience = _configuration["JwtSecret:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
       
        public async Task<UserDto> RegisterAsync (Auth authUser)
        {
            bool existingUser = await _context.Users.AnyAsync(u => u.Email == authUser.Email);

            if (existingUser)
            {
                throw new ArgumentException("User with this email already exists.");
            }

            var hashedPassword = _passwordHasher.HashPassword(new User(), authUser.Password);

            if (!Enum.TryParse(authUser.RoleName, true, out UserRole parsedRole))
            {
                throw new ArgumentException($"Invalid role '{authUser.RoleName}'." +
                    $" Allowed roles are: Admin and Teacher for an Admin user, or Student for anyone.");
            }

            int creatorId = 0;
            UserRole inputRole = UserRole.Student;

            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                var creatorIdClaim = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(creatorIdClaim, out creatorId) && creatorId > 0)
                {
                    var creatorRole = await _userService.GetUserRole(creatorId);
                    inputRole = creatorRole == UserRole.Admin ? parsedRole : UserRole.Student;
                }
            }
            else
            {
                inputRole = UserRole.Student;
            }

            User userToBeSaved = new User
            {
                Email = authUser.Email,
                Password = hashedPassword,
                Role = inputRole
            };

            _context.Users.Add(userToBeSaved);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = userToBeSaved.Id,
                Email = userToBeSaved.Email,
                Role = userToBeSaved.Role
            };
        } 

        public async Task<LoginResultDto> LoginAsync(string email, string password)
        {
            User registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email); ;

            if (registeredUser == null)
            {
                throw new UnauthorizedAccessException("Invalid email");
            }

            var verifiedPassword = _passwordHasher.VerifyHashedPassword(registeredUser, registeredUser.Password, password);

            if (verifiedPassword != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException("Invalid password.");
            }

            string token = await GenerateTokenAsync(registeredUser);

            UserRole userRole = await _userService.GetUserRole(registeredUser.Id);

            return new LoginResultDto
            {
                Id = registeredUser.Id,
                Email = registeredUser.Email,
                Role = userRole,
                Token = token
            };
        }

        public async Task<UserDto> GetCurrentUserAsync()    
        {
            
            var userIdClaim = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userIdClaim == null) throw new UnauthorizedAccessException("Invalid user id");

            if (!int.TryParse(userIdClaim, out int userId)) throw new UnauthorizedAccessException("Invalid user id");

            var user = await _context.Users.FindAsync(userId);

            if (user == null) throw new KeyNotFoundException("Authenticated user not found");

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
