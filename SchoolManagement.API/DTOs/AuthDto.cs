using SchoolManagement.API.Models;

namespace SchoolManagement.API.DTOs
{
    public class AuthDto
    {
        public string Email { get; set; }
        public User.UserRole Role { get; set; }
        public string RoleName { get; }

        public AuthDto(string email, User.UserRole role)
        {
            Email = email;
            Role = role;
            RoleName = role switch
            {
                User.UserRole.Admin => "Admin",
                User.UserRole.Teacher => "Teacher",
                User.UserRole.Student => "Student",
                _ => "Unknown"
            };
        }

    }
}
