namespace ServiceApotheke.API.Models
{
    public class UpdatePharmacistProfileDto
    {
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
    }
}