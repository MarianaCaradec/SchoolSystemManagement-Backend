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
        IPasswordHasher<User> passwordHasher, IUserService userService) : IAuthService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly SchoolSysDBContext _context = context;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
        private readonly IUserService _userService = userService;

        public async Task<string> GenerateTokenAsync(User user)
        {
            var jwtSecret = _configuration.GetSection("JwtSecret");
            var key = Encoding.UTF8.GetBytes(jwtSecret["SecretKey"]);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSecret["ExpirationInMinutes"])),
                Issuer = jwtSecret["Issuer"],
                Audience = jwtSecret["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<string> AuthenticateAsync(string email, string password, int userId)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Id == userId);

            if (user == null ||_passwordHasher.VerifyHashedPassword(user, user.Password, password) != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            return await GenerateTokenAsync(user);
        }
       
        public async Task<UserDto> RegisterAsync (Auth authUser, int? userId)
        {
            bool existingUser = await _context.Users.AnyAsync(u => u.Email == authUser.Email);

            if(existingUser)
            {
                throw new ArgumentException("User with this email already exists.");
            }

            var hashedPassword = _passwordHasher.HashPassword(null, authUser.Password);

            if (!Enum.TryParse(authUser.Role.ToString(), true, out UserRole inputRole))
            {
                throw new ArgumentException($"Invalid role '{authUser.Role}'." +
                    $" Allowed roles are: Admin and Teacher for an Admin user, or Student for anyone.");
            }

            UserRole creatorRole = UserRole.Student;

            if (userId.HasValue && userId > 0)
            {
                creatorRole = await _userService.GetUserRole(userId.Value);
            }
            
            if (creatorRole == UserRole.Admin)
            {
                inputRole = authUser.Role;
            }
            else if (creatorRole == UserRole.Teacher || creatorRole == UserRole.Student)
            {
                inputRole = UserRole.Student;
            }
            else
            {
                throw new UnauthorizedAccessException($"Only Admin users can assign {authUser.Role} role.");
            }

            User userToBeSaved = new User
            {
                Id = authUser.Id,
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

        public async Task<AuthDto> LoginAsync(string email, string password)
        {
            User registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (registeredUser == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var verifiedPassword = _passwordHasher.VerifyHashedPassword(registeredUser, registeredUser.Password, password);

            if (verifiedPassword != PasswordVerificationResult.Success)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            string token = await GenerateTokenAsync(registeredUser);

            AuthDto authUser = new AuthDto(registeredUser.Email, registeredUser.Role);

            return authUser;
        }
    }
}
