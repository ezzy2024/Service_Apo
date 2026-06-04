using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class Pharmacist
    {
        public int Id { get; set; }
        [Required] public string FullName { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public bool IsVerified { get; set; } = false;
        public string? PreferredContactMethod { get; set; }
        public bool HasApprobation { get; set; }
        public string? ApprobationCountry { get; set; }
        public string? ExperienceYears { get; set; }
        public string? Specialties { get; set; }
        public string? SoftwareExperience { get; set; }
        public string? RadiusKm { get; set; }
        public string? PreferredStates { get; set; }
        public string? TravelWillingness { get; set; }
        public string? Mobility { get; set; }
        public string? AvailabilityType { get; set; }
        public string? ShortNoticeAvailability { get; set; }
        public bool EmergencyServiceWillingness { get; set; }
        public bool WeekendWillingness { get; set; }
        public string? FeeModel { get; set; }
        public decimal HourlyRate { get; set; }
        public string? VatSubject { get; set; }
        public string? TravelExpenses { get; set; }

        // Dateipfade
        public string? ApprobationDocumentPath { get; set; }
        public string? CvDocumentPath { get; set; }

        [JsonIgnore]
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}