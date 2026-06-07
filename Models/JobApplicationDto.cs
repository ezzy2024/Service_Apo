namespace ServiceApotheke.API.Models
{
    public class JobDisplayDto
    {
        public int Id { get; set; }
        public string StartDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal Salary { get; set; }
        public string PharmacyName { get; set; }
        public string Address { get; set; }
    }
}