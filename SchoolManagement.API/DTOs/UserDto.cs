using SchoolManagement.API.Models;

namespace SchoolManagement.API.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public User.UserRole Role { get; set; }
        public string RoleName { get; }
        public TeacherDto? Teacher { get; set; }
        public StudentDto? Student { get; set; }

        public UserDto() { }

        public UserDto(int id, string email, User.UserRole role)
        {
            Id = id;
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
