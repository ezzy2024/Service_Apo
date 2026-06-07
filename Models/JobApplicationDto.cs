namespace ServiceApotheke.API.Models
{
    public class JobDisplayDto
    {
        public int Id { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string PharmacyName { get; set; } = "Apotheke";
        public string Address { get; set; } = "Nicht hinterlegt";
    }
}