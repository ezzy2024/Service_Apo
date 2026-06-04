using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        [Required] public int JobPostId { get; set; }
        [Required] public int PharmacistId { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        
        public string? TimesheetPath { get; set; }

        [JsonIgnore]
        public virtual JobPost? JobPost { get; set; }
        [JsonIgnore]
        public virtual Pharmacist? Pharmacist { get; set; }
    }
}