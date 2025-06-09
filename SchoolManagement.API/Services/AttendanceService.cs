using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using SchoolManagement.API.Data.Context;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;
using static SchoolManagement.API.Models.User;

namespace SchoolManagement.API.Services
{
    public class AttendanceService(SchoolSysDBContext context, IUserService userService) : IAttendanceService
    {
        private readonly SchoolSysDBContext _context = context;
        private readonly IUserService _userService = userService;

        public async Task<IEnumerable<AttendanceDto>> GetAttendancesAsync(int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            IQueryable<Attendance> query = _context.Attendances.Include(a => a.Student);

            if (userRole == UserRole.Student)
            {
               query = query.Where(a => a.StudentId == userId);

            }

            return await query.Select(a => new AttendanceDto
            {
                 Id = a.Id,
                 Date = a.Date,
                 Present = a.Present,
                 StudentId = a.Student != null ? a.StudentId : null,
                 TeacherId = userRole != UserRole.Student && a.Teacher != null ? a.TeacherId : null,
                 StudentUserId = a.Student != null ? a.Student.UserId : null,
                 TeacherUserId = a.Teacher != null ? a.Teacher.UserId : null
            }).ToListAsync();
        }

        public async Task<AttendanceResponseDto> GetAttendanceByIdAsync(int id, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            Attendance attendance = await _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                throw new KeyNotFoundException($"Attendance with ID {id} not found.");
            }

            if (userRole == UserRole.Student)
            {
                if (attendance.Student == null || attendance.Student.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");
                }
            }

            if (userRole == UserRole.Teacher)
            {
                if (attendance.Teacher != null && attendance.Teacher.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");
                }
            }

            return new AttendanceResponseDto
            {
                Id = attendance.Id,
                Date = attendance.Date,
                Present = attendance.Present,
                Student = attendance.Student != null ? new StudentInputDto
                {
                    Id = attendance.Student.Id,
                    Name = attendance.Student.Name,
                    Surname = attendance.Student.Surname
                } : null,
                Teacher = userRole != UserRole.Student && attendance.Teacher != null ? new TeacherInputDto
                {
                    Id = attendance.Teacher.Id,
                    Name = attendance.Teacher.Name,
                    Surname = attendance.Teacher.Surname
                } : null
            };
        }

        public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceDto attendanceToBeCreated, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if(userRole == UserRole.Student)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            bool hasStudent = attendanceToBeCreated.StudentId.HasValue && attendanceToBeCreated.StudentId != 0;
            bool hasTeacher = attendanceToBeCreated.TeacherId.HasValue && attendanceToBeCreated.TeacherId != 0;

            if (!hasStudent && !hasTeacher)
            {
                throw new ArgumentException("Either StudentId or TeacherId must be provided.");
            }

            if (hasStudent && hasTeacher)
            {
                throw new ArgumentException("Only one of StudentId or TeacherId must be provided.");
            }

            Attendance createdAttendanceToBeSaved = new Attendance
            {
                Date = attendanceToBeCreated.Date,
                Present = attendanceToBeCreated.Present,
                StudentId = hasStudent ? attendanceToBeCreated.StudentId : null,
                TeacherId = hasTeacher ? attendanceToBeCreated.TeacherId : null,
                Student = hasStudent ? await _context.Students.FindAsync(attendanceToBeCreated.StudentId) : null,
                Teacher = hasTeacher ? await _context.Teachers.FindAsync(attendanceToBeCreated.TeacherId) : null
            };

            if (hasStudent)
            {
                Student studentWithClassAndTeachers = await _context.Students
                    .Include(s => s.Class.Teachers)
                    .FirstOrDefaultAsync(s => s.Id == attendanceToBeCreated.StudentId);

                if (studentWithClassAndTeachers == null)
                {
                    throw new ArgumentException("Invalid student or class information.");
                }

                if (userRole == UserRole.Teacher &&
                !studentWithClassAndTeachers.Class.Teachers.Any(t => t.UserId == userId))
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");
                }
            }

            if (hasTeacher && userRole == UserRole.Teacher)
            {
                var existingTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == createdAttendanceToBeSaved.TeacherId);

                if (existingTeacher != null && existingTeacher.UserId != userId)
                {
                    throw new UnauthorizedAccessException("Teachers cannot create attendance records for other teachers.");
                }

                if (existingTeacher != null && existingTeacher.UserId == userId)
                {
                    throw new UnauthorizedAccessException("Teachers cannot create their own attendance records.");
                }
            }

            _context.Attendances.Add(createdAttendanceToBeSaved);
            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = createdAttendanceToBeSaved.Id,
                Date = createdAttendanceToBeSaved.Date,
                Present = createdAttendanceToBeSaved.Present,
                StudentId = createdAttendanceToBeSaved.StudentId,
                TeacherId = createdAttendanceToBeSaved.TeacherId,
                StudentUserId = hasStudent ? (await _context.Students.FindAsync(attendanceToBeCreated.StudentId)).UserId : null,
                TeacherUserId = hasTeacher ? (await _context.Teachers.FindAsync(attendanceToBeCreated.TeacherId)).UserId : null
            };
        }

        public async Task<AttendanceDto> UpdateAttendanceAsync(int id, AttendanceDto attendanceToBeUpdated, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if (userRole == UserRole.Student)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            bool hasStudent = attendanceToBeUpdated.StudentId.HasValue && attendanceToBeUpdated.StudentId != 0;
            bool hasTeacher = attendanceToBeUpdated.TeacherId.HasValue && attendanceToBeUpdated.TeacherId != 0;

            if (!hasStudent && !hasTeacher)
            {
                throw new ArgumentException("Either StudentId or TeacherId must be provided.");
            }

            if (hasStudent && hasTeacher)
            {
                throw new ArgumentException("Only one of StudentId or TeacherId must be provided.");
            }

            Attendance attendance = await _context.Attendances
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null) throw new KeyNotFoundException($"Attendance with ID {id} not found.");

            if (hasStudent)
            {
                Student studentWithClassAndTeachers = await _context.Students
                    .Include(s => s.Class.Teachers)
                    .FirstOrDefaultAsync(s => s.Id == attendanceToBeUpdated.StudentId);

                if (studentWithClassAndTeachers == null)
                {
                    throw new ArgumentException("Invalid student or class information.");
                }

                if (userRole == UserRole.Teacher &&
                !studentWithClassAndTeachers.Class.Teachers.Any(t => t.UserId == userId))
                {
                    throw new UnauthorizedAccessException("You are not authorized to do this action.");
                }

                DateTime currentDate = DateTime.Now;
                DateTime attendanceDate = attendance.Date.ToDateTime(TimeOnly.MinValue);

                if ((currentDate - attendanceDate).TotalDays >= 21)
                {
                    throw new InvalidOperationException("Attendance update is not allowed after three weeks.");
                }
            }

            if (hasTeacher && userRole == UserRole.Teacher)
            {
                var existingTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == attendance.TeacherId);

                if (existingTeacher != null && existingTeacher.UserId != userId)
                {
                    throw new UnauthorizedAccessException("Teachers cannot modify attendance records for other teachers.");
                }

                if (existingTeacher != null && existingTeacher.UserId == userId)
                {
                    throw new UnauthorizedAccessException("Teachers cannot modify their own attendance records.");
                }
            }

            if (userRole == UserRole.Admin)
            {
                attendance.TeacherId = hasTeacher ? attendanceToBeUpdated.TeacherId : null;
            }

            attendance.Date = attendanceToBeUpdated.Date;
            attendance.Present = attendanceToBeUpdated.Present;
            attendance.StudentId = hasStudent ? attendanceToBeUpdated.StudentId : null;

            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();

            return new AttendanceDto
            {
                Id = attendance.Id,
                Date = attendance.Date,
                Present = attendance.Present,
                StudentId = attendance.StudentId,
                TeacherId = attendance.TeacherId
            };
        }

        public async Task<bool> DeleteAttendanceAsync(int id, int userId)
        {
            UserRole userRole = await _userService.GetUserRole(userId);

            if(userRole != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("You are not authorized to do this action.");
            }

            Attendance attendanceToBeDeleted = await _context.Attendances.FindAsync(id);

            if (attendanceToBeDeleted == null) throw new KeyNotFoundException($"Attendance with ID {id} not found.");

            _context.Attendances.Remove(attendanceToBeDeleted);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
