using System.Text.Json.Serialization;

namespace SchoolManagement.API.Models
{
    public class Auth
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        [JsonPropertyName("roleName")]
        public string RoleName { get; set; }
    }
}
