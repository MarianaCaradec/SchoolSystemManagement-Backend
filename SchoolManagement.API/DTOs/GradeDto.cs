namespace SchoolManagement.API.DTOs
{
    public class GradeDto
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public DateOnly Date { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
    }
}
