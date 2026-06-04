namespace ServiceApotheke.API.Models
{
    public class PharmacyProfileDto
    {
        public string? PharmacyName { get; set; }
        public string? Phone { get; set; }
        public string? ContactPerson { get; set; }
        public string? ZipAndCity { get; set; }
        public string? StreetAndNumber { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? FocusAreas { get; set; }
        public bool BillingByInvoicePossible { get; set; }
        public bool ParkingAvailable { get; set; }
        public string? AccommodationProvided { get; set; }
        public string? LicenseNumber { get; set; }
        public string? SupportOnSite { get; set; }
        public string? WorkingHoursFrom { get; set; }
        public string? WorkingHoursTo { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? UrgencyLevel { get; set; }
        public decimal DesiredRate { get; set; }
        public string? AdditionalNotes { get; set; }
    }
}