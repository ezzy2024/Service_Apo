using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class Pharmacy
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string PharmacyName { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public bool IsVerified { get; set; } = false;
        // Neue Profilfelder
        public string? ContactPerson { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? FocusAreas { get; set; }
        public string? StaffSupport { get; set; }
        public bool InvoiceBillingPossible { get; set; }
        public string? TargetHourlyRate { get; set; }
        public bool ParkingAvailable { get; set; }
        public string? AccommodationProvided { get; set; }
        

        [JsonIgnore]
        public virtual ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}