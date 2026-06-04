using System.ComponentModel.DataAnnotations;

namespace ServiceApotheke.API.Models
{
    public class PharmacyRegDto
    {
        [Required]
        public string PharmacyName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }
        
        public string? LicenseNumber { get; set; }
    }
}