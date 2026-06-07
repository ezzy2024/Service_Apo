using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacistController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;

        public PharmacistController(DataContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PharmacistRegDto registration)
        {
            if (await _context.Pharmacists.AnyAsync(p => p.Email == registration.Email))
            {
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
            string token = Guid.NewGuid().ToString();

            var pharmacist = new Pharmacist
            {
                FullName = registration.FullName,
                Email = registration.Email,
                PasswordHash = passwordHash,
                PhoneNumber = registration.PhoneNumber,
                EmailConfirmationToken = token,
                IsEmailConfirmed = true
            };

            _context.Pharmacists.Add(pharmacist);
            await _context.SaveChangesAsync();
             var confirmationLink = $"https://serviceapotheke.tech/verify.html?email={registration.Email}&token={token}";
             var emailBody = $"<h1>E-Mail-Bestätigung</h1><p>Hallo {pharmacist.FullName}, bitte klicken Sie auf den folgenden Link, um Ihr Konto zu bestätigen:</p><a href='{confirmationLink}'>Konto bestätigen</a>";
            
            try
            {
                await _emailService.SendEmailAsync(pharmacist.Email, "Bestätigen Sie Ihr Konto - Service Apotheke", emailBody);
                return Ok(new { message = "Registrierung erfolgreich. Bitte überprüfen Sie Ihre E-Mails, um Ihr Konto zu bestätigen." });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("==========================================================================");
                Console.WriteLine($"[SMTP-WARNUNG] E-Mail-Versand fehlgeschlagen: {ex.Message}");
                Console.WriteLine($"[TEST-LINK] Kopieren Sie diesen Link zur Aktivierung:");
                Console.WriteLine(confirmationLink);
                Console.WriteLine("==========================================================================");
                Console.ResetColor();

                return Ok(new { 
                    message = "Registrierung in Datenbank erfolgreich. SMTP-Fehler lokal umgangen. Aktivierungslink wurde im Server-Terminal ausgegeben.",
                    debugLink = confirmationLink 
                });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(u => u.Email == email);
            
            if (user == null || user.EmailConfirmationToken != token)
            {
                string errorHtml = "<!DOCTYPE html><html lang='de'><body style='font-family:sans-serif; background:#f5f9ff; display:flex; align-items:center; justify-content:center; height:100vh;'><div style='background:white; padding:40px; border-radius:8px; box-shadow:0 4px 10px rgba(0,0,0,0.1); text-align:center;'><h2 style='color:#c01c28;'>Ungültiger Bestätigungslink</h2><p>Der Link ist fehlerhaft oder wurde bereits verwendet.</p></div></body></html>";
                return Content(errorHtml, "text/html");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();

            string successHtml = @"
            <!DOCTYPE html>
            <html lang='de'>
            <head><meta charset='utf-8'><title>Konto bestätigt</title></head>
            <body style='font-family:sans-serif; background:#f5f9ff; display:flex; align-items:center; justify-content:center; height:100vh;'>
                <div style='background:white; padding:40px; border-radius:8px; box-shadow:0 4px 10px rgba(0,0,0,0.1); text-align:center;'>
                    <h2 style='color:#26a269;'>E-Mail erfolgreich bestätigt!</h2>
                    <p style='color:#333; margin-top:10px;'>Ihr Konto ist jetzt aktiv. Sie können diesen Tab schließen und sich anmelden.</p>
                </div>
            </body>
            </html>";

            return Content(successHtml, "text/html");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == login.Email);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });
            }
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { token = tokenHandler.WriteToken(token), id = user.Id.ToString(), fullName = user.FullName });
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdatePharmacistProfileDto dto)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound(new { message = "Benutzer nicht gefunden." });

            user.PreferredContactMethod = dto.PreferredContactMethod;
            user.HasApprobation = dto.HasApprobation;
            user.ApprobationCountry = dto.ApprobationCountry;
            user.ExperienceYears = dto.ExperienceYears;
            user.Specialties = dto.Specialties;
            user.SoftwareExperience = dto.SoftwareExperience;
            user.RadiusKm = dto.RadiusKm.ToString(); // Fix CS0029 (assuming RadiusKm is string)
            user.PreferredStates = dto.PreferredStates;
            user.TravelWillingness = dto.TravelWillingness;
            user.Mobility = dto.Mobility;
            user.AvailabilityType = dto.AvailabilityType;
            user.ShortNoticeAvailability = dto.ShortNoticeAvailability;
            user.EmergencyServiceWillingness = dto.EmergencyServiceWillingness;
            user.WeekendWillingness = dto.WeekendWillingness;
            user.FeeModel = dto.FeeModel;
            user.HourlyRate = dto.HourlyRate;
            user.VatSubject = dto.VatSubject;
            user.TravelExpenses = dto.TravelExpenses;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == request.Email);
            
            if (user == null) 
            {
                return Ok(new { message = "Code gesendet (Simulation)." }); 
            }

            string resetCode = new Random().Next(100000, 999999).ToString();
            
            try
            {
                await _emailService.SendEmailAsync(user.Email, "Passwort-Reset", $"Ihr Code: {resetCode}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("==========================================================================");
                Console.WriteLine($"[SMTP-WARNUNG] E-Mail-Versand fehlgeschlagen: {ex.Message}");
                Console.WriteLine($"[TEST-CODE] Passwort-Reset-Code für {user.Email}: {resetCode}");
                Console.WriteLine("==========================================================================");
                Console.ResetColor();
            }

            return Ok(new { message = "Code gesendet." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacist(int id)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) 
            {
                return NotFound(new { message = $"Pharmacist mit ID {id} nicht in DB gefunden." });
            }
            user.PasswordHash = string.Empty; // Fix CS8625
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePharmacistGeneral(int id, [FromBody] PharmacistUpdateGeneralDto dto)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound(new { message = "Benutzer nicht gefunden." });

            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.Phone ?? user.PhoneNumber;
            user.Address = dto.Address ?? user.Address;
            user.RadiusKm = dto.MaxDistanceKm > 0 ? dto.MaxDistanceKm.ToString() : user.RadiusKm; // Fix CS0029
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }

        [HttpGet("{id}/upcoming-shifts")]
public async Task<IActionResult> GetUpcomingShifts(int id)
{
    var shifts = await _context.JobApplications
        .Include(a => a.JobPost)
        .Where(a => a.PharmacistId == id && a.Status == "Accepted")
        .Select(a => new {
            Id = a.Id,
            JobPostId = a.JobPostId,
            StartTime = a.JobPost.StartTime,
            EndTime = a.JobPost.EndTime,
            Salary = a.JobPost.Salary,
            StartDate = a.JobPost.StartDate, // Wichtig für das Frontend-Mapping
            PharmacyName = a.JobPost.Pharmacy != null ? a.JobPost.Pharmacy.PharmacyName : "Apotheke"
        })
        .ToListAsync();
    
    return Ok(shifts);
}

        [HttpGet("{id}/all-shifts")]
        public async Task<IActionResult> GetAllShifts(int id)
        {
            var shifts = await _context.JobApplications
                .Include(a => a.JobPost)
                .Where(a => a.PharmacistId == id)
                .ToListAsync();
                
            return Ok(shifts);
        }

        [HttpGet("{id}/completed-shifts")]
        public async Task<IActionResult> GetCompletedShifts(int id)
        {
            var shifts = await _context.JobApplications
                .Include(a => a.JobPost)
                // Removed invalid ThenInclude
                .Where(a => a.PharmacistId == id && a.Status == "Completed")
                .ToListAsync();
                
            return Ok(shifts);
        }

        [HttpPost("{id}/upload-cv")]
        public async Task<IActionResult> UploadCv(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Keine Datei hochgeladen." });
            
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound(new { message = "Benutzer nicht gefunden." });

            user.CvDocumentPath = file.FileName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lebenslauf erfolgreich hochgeladen." });
        }
    }

    public class PharmacistUpdateGeneralDto 
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? Bio { get; set; }
        public bool HasTransportation { get; set; }
        public string? TransportationType { get; set; }
        public bool HasDrivingLicense { get; set; }
        public string? TaxNumber { get; set; }
        public int MaxDistanceKm { get; set; }
        public int AvailableDaysPerWeek { get; set; }
    }
}