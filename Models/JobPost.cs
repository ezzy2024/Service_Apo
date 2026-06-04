using System;

namespace ServiceApotheke.API.Models
{
    public class JobPost
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string Urgency { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string SoftwareSystem { get; set; } = string.Empty;
        public string FocusAreas { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string Accommodation { get; set; } = string.Empty;
        public bool BillingByInvoice { get; set; }
        public bool ParkingAvailable { get; set; }
        public string? Notes { get; set; }
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}