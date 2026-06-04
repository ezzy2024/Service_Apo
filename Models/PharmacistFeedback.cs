using System.ComponentModel.DataAnnotations;

namespace ServiceApotheke.API.Models
{
    public class PharmacistFeedback
    {
        public int Id { get; set; }
        [Required] public int JobPostId { get; set; }
        [Required] public int PharmacistId { get; set; }
        public int OnboardingScore { get; set; } 
        public int WorkspaceSetupScore { get; set; } 
        public string WorkloadHV { get; set; } = string.Empty;
        public string WorkloadRecipe { get; set; } = string.Empty;
        public int BtmProcessScore { get; set; } 
        public int DataProtectionScore { get; set; } 
        public int OverallScore { get; set; } 
        public string CriticalIncidents { get; set; } = string.Empty;
        public string PositiveAspects { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}