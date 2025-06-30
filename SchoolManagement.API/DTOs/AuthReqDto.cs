namespace SchoolManagement.API.DTOs
{
    public class AuthReqDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int UserId { get; set; }
    }
}
