namespace ServiceApotheke.API.Models
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "pharmacist";
    }
}