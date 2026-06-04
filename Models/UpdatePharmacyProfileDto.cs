namespace ServiceApotheke.API.Models
{
    public class UpdatePharmacyProfileDto
    {
        public string? ContactPerson { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? FocusAreas { get; set; }
        public string? StaffSupport { get; set; }
        public bool InvoiceBillingPossible { get; set; }
        public string? TargetHourlyRate { get; set; }
        public bool ParkingAvailable { get; set; }
        public string? AccommodationProvided { get; set; }
    }
}