using System.ComponentModel.DataAnnotations;

namespace ServiceApotheke.API.Models
{
    public class PharmacyFeedback
    {
        public int Id { get; set; }
        [Required] public int JobPostId { get; set; }
        [Required] public int PharmacyId { get; set; }
        public string ActualStartTime { get; set; } = string.Empty;
        public string ActualEndTime { get; set; } = string.Empty;
        public int CompetenceScore { get; set; } 
        public int IndependenceScore { get; set; } 
        public int CarefulnessScore { get; set; } 
        public int StressHandlingScore { get; set; } 
        public int TeamworkScore { get; set; } 
        public int OverallScore { get; set; } 
        public bool WouldBookAgain { get; set; }
        public string PositiveAspects { get; set; } = string.Empty;
        public string ImprovementAspects { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}