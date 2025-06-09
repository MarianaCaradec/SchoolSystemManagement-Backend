using Microsoft.EntityFrameworkCore;
using SchoolManagement.API.Data.Context;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;
using static SchoolManagement.API.Models.User;

namespace SchoolManagement.API.Services
{
    public class StudentService(SchoolSysDBContext context, IUserService userService) : IStudentService
    {
        private readonly SchoolSysDBContext _context = context;
        private readonly IUserService _userService = userService;

        public async Task<IEnumerable<StudentResponseDto>> GetStudentsAsync(int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            IQueryable<Student> query = _context.Students.Include(s => s.User);

            return await query.Select(st => new StudentResponseDto
            {
                Id = userRole != UserRole.Student ? st.Id : 0,
                Name = st.Name,
                Surname = st.Surname,
                BirthDate = st.BirthDate,
                Address = userRole != UserRole.Student ? st.Address : "-",
                MobileNumber = userRole != UserRole.Student ? st.MobileNumber : 0,
            }).ToListAsync();
        }

        public async Task<StudentResponseDto> GetStudentByIdAsync(int id, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            Student student = await _context.Students
                .Include(st => st.User)
                .Include(st => st.Class)
                .ThenInclude(c => c.Teachers)
                .Include(st => st.Attendances)
                .Include(st => st.Grades)
                .ThenInclude(g => g.Subject)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (student == null) throw new KeyNotFoundException($"Student with ID {id} not found.");

            if (userRole == UserRole.Teacher && !student.Class.Teachers.Any(t => t.UserId == userId))
            {
                throw new UnauthorizedAccessException("You are not authorized to access this student.");
            }
            
            if(userRole == UserRole.Student && student.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to access this student.");
            }

            return new StudentResponseDto
            {
                Id = userRole != UserRole.Student ? student.Id : 0,
                Name = student.Name,
                Surname = student.Surname,
                BirthDate = student.BirthDate,
                Address = student.Address,
                MobileNumber = student.MobileNumber,
                EmailRole = new AuthDto(student.User.Email, student.User.Role),
                Class = new ClassStudentDto 
                { 
                    Course = student.Class.Course, 
                    Divition = student.Class.Divition, 
                    Teachers = student.Class.Teachers.Select(t => new TeacherResponseDto
                    {
                        Name = t.Name,
                        Surname = t.Surname,
                        MobileNumber = userRole != UserRole.Student ? t.MobileNumber : 0,
                        Address = userRole != UserRole.Student ? t.Address : "-",
                        UserId = userRole != UserRole.Student ? t.UserId : 0,
                    }).ToList()
                    },
                Attendances = student.Attendances.Select(a => new AttendanceDto
                {
                    Date = a.Date,
                    Present = a.Present,
                })
                .ToList(),
                Grades = student.Grades.Select(g => new GradeDto
                {
                    Value = g.Value,
                    Date = g.Date,
                    SubjectId = g.SubjectId,
                    SubjectName = g.Subject.Title,
                })
                .ToList(),
            };
        }

        public async Task<StudentInputDto> CreateStudentAsync(StudentInputDto studentToBeCreated, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if(userRole != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            Student createdStudentToBeSaved = new Student
            {
                Name = studentToBeCreated.Name,
                Surname = studentToBeCreated.Surname,
                BirthDate = studentToBeCreated.BirthDate,
                Address = studentToBeCreated.Address,
                MobileNumber = studentToBeCreated.MobileNumber,
                UserId = studentToBeCreated.UserId,
                ClassId = studentToBeCreated.ClassId
            };

            _context.Students.Add(createdStudentToBeSaved);
            await _context.SaveChangesAsync();

            return new StudentInputDto
            {
                Id = createdStudentToBeSaved.Id,
                Name = createdStudentToBeSaved.Name,
                Surname = createdStudentToBeSaved.Surname,
                BirthDate = createdStudentToBeSaved.BirthDate,
                Address = createdStudentToBeSaved.Address,
                MobileNumber = createdStudentToBeSaved.MobileNumber,
                UserId = createdStudentToBeSaved.UserId,
                ClassId = createdStudentToBeSaved.ClassId
            };
        }

        public async Task<StudentInputDto> UpdateStudentAsync(int id, StudentInputDto studentToBeUpdated, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if(userRole == UserRole.Teacher)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            Student student = await _context.Students
                .FirstOrDefaultAsync(st => st.Id == id);

            if (student == null) throw new KeyNotFoundException($"Student with ID {id} not found.");

            if(userRole == UserRole.Student)
            {
                if(student.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");

                }

                student.Name = studentToBeUpdated.Name;
                student.Surname = studentToBeUpdated.Surname;
                student.BirthDate = studentToBeUpdated.BirthDate;
                student.Address = studentToBeUpdated.Address;
                student.MobileNumber = studentToBeUpdated.MobileNumber;
            } else
            {
                student.Name = studentToBeUpdated.Name;
                student.Surname = studentToBeUpdated.Surname;
                student.BirthDate = studentToBeUpdated.BirthDate;
                student.Address = studentToBeUpdated.Address;
                student.MobileNumber = studentToBeUpdated.MobileNumber;
                student.ClassId = studentToBeUpdated.ClassId;
                student.UserId = studentToBeUpdated.UserId;
            }

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            return new StudentInputDto
            {
                Name = student.Name,
                Surname = student.Surname,
                BirthDate = student.BirthDate,
                Address = student.Address,
                MobileNumber = student.MobileNumber,
                ClassId = student.ClassId,
                UserId = student.UserId,
            };
        }

        public async Task<bool> DeleteStudentAsync(int id, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if(userRole != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            Student studentToBeDeleted = await _context.Students.FindAsync(id);

            if (studentToBeDeleted == null) throw new KeyNotFoundException($"Student with ID {id} not found");

            _context.Students.Remove(studentToBeDeleted);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
