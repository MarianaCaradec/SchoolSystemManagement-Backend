using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.API.Data.Context;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;
using static SchoolManagement.API.Models.User;

namespace SchoolManagement.API.Services
{
    public class UserService(SchoolSysDBContext context, IPasswordHasher<User> passwordHasher) : IUserService
    {
        private readonly SchoolSysDBContext _context = context;
        private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;

        public async Task<IEnumerable<UserDto>> GetUsersAsync(int userId)
        {
            UserRole userRole = await GetUserRole(userId);

            IQueryable<User> query = _context.Users
                .Include(u => u.Teacher)
                .Include(u => u.Student)
                .ThenInclude(s => s.Class)
                .ThenInclude(c => c.Teachers);

            return await query.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                Teacher = userRole == UserRole.Admin && u.Teacher != null ? new TeacherDto
                {
                    Id = u.Teacher.Id,
                    Name = u.Teacher.Name,
                    Surname = u.Teacher.Surname
                } : null,
                Student = userRole == UserRole.Admin && u.Student != null ? new StudentDto
                {
                    Id = u.Student.Id,
                    Name = u.Student.Name,
                    Surname = u.Student.Surname
                } : null,
            }).ToListAsync();
        }

        public async Task<UserDto> GetUserByEmailAsync(string email, int userId)
        {
            UserRole userRole = await GetUserRole(userId);

            IQueryable<User> query = _context.Users;

            UserDto user = await query.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                Teacher = userRole == UserRole.Admin && u.Teacher != null ? new TeacherDto
                {
                    Id = u.Teacher.Id,
                    Name = u.Teacher.Name,
                    Surname = u.Teacher.Surname
            } : null,
                Student = userRole == UserRole.Admin && u.Student != null ? new StudentDto
                {
                    Id = u.Student.Id,
                    Name = u.Student.Name,
                    Surname = u.Student.Surname
                } : null
            }).FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) throw new KeyNotFoundException($"User with email {email} not found.");

            if (userRole != UserRole.Admin && userId != user?.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to view this user.");
            }

            return user;
        }

        public async Task<UserRole> GetUserRole(int id)
        {
            if (id <= 0) throw new UnauthorizedAccessException("Invalid or missing user ID from authentication context.");
            
            User user = await _context.Users.FindAsync(id) ??
                throw new KeyNotFoundException($"User with ID {id} not found");

            return user.Role;
        }

        private string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(null, password);
        }

        public async Task<UserDto> CreateUserAsync(UserInputDto userToBeCreated, int? userId)
        { 
            string hashedPassword = HashPassword(userToBeCreated.Password);

            if (!Enum.TryParse(userToBeCreated.Role.ToString(), true, out UserRole inputRole))
            {
                throw new ArgumentException($"Invalid role '{userToBeCreated.Role}'. Allowed roles are: Admin and Teacher for an Admin user, or Student for anyone.");
            }

            UserRole creatorRole = UserRole.Student;

            if (userId.HasValue && userId > 0)
            {
                creatorRole = await GetUserRole(userId.Value);
            }

            if (creatorRole == UserRole.Admin)
            {
                inputRole = userToBeCreated.Role;
            }
            else if (creatorRole == UserRole.Teacher || creatorRole == UserRole.Student)
            {
                inputRole = UserRole.Student;
            }
            else
            {
                throw new UnauthorizedAccessException($"Only Admin users can assign {userToBeCreated.Role} role.");
            }


            User createdUser = new User
            {
                Email = userToBeCreated.Email,
                Password = hashedPassword,
                Role = inputRole
            };

            _context.Users.Add(createdUser);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = createdUser.Id,
                Email = createdUser.Email,
                Role = createdUser.Role,
            };
        }

        public async Task<UserDto> UpdateUserAsync(int id, UserInputDto userToBeUpdated, int userId)
        {
            UserRole userRole = await GetUserRole(userId);

            User user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) throw new KeyNotFoundException($"User with ID {id} not found");

            if (!Enum.TryParse(userToBeUpdated.Role.ToString(), true, out UserRole inputRole))
            {
                throw new ArgumentException($"Invalid role '{userToBeUpdated.Role}'. Allowed roles are: Admin and Teacher for an Admin user, or Student for anyone.");
            }

            string hashedPassword = HashPassword(userToBeUpdated.Password);

            if (userRole != UserRole.Admin)
            {
                if(id != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");
                } else
                {
                    user.Email = userToBeUpdated.Email;
                    user.Password = hashedPassword;
                }
            }
            else
            {
                user.Email = userToBeUpdated.Email;
                user.Password = userToBeUpdated.Password;
                user.Role = inputRole;
            }

            _context.Update(user);   
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role,
            };
        }

        public async Task<bool> DeleteUserAsync(int id, int userId)
        {
            UserRole userRole = await GetUserRole(userId);

            if (userRole != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            User userToBeDeleted = await _context.Users.FindAsync(id);

            if (userToBeDeleted == null) throw new KeyNotFoundException($"User with ID {id} not found");
            
            _context.Users.Remove(userToBeDeleted);
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}
