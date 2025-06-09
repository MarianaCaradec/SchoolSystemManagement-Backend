using SchoolManagement.API.Models;

namespace SchoolManagement.API.DTOs
{
    public class AttendanceDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public bool Present { get; set; }
        public int? StudentId { get; set; }
        public int? TeacherId { get; set; }
        public int? StudentUserId { get; set; }
        public int? TeacherUserId { get; set; }

    }
}
