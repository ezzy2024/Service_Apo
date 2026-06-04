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

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;

        public PharmacyController(DataContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PharmacyRegDto registration)
        {
            if (await _context.Pharmacies.AnyAsync(p => p.Email == registration.Email))
            {
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
            string token = Guid.NewGuid().ToString();

            var pharmacy = new Pharmacy
            {
                PharmacyName = registration.PharmacyName,
                Email = registration.Email,
                PasswordHash = passwordHash,
                PhoneNumber = registration.PhoneNumber,
                Address = registration.Address ?? string.Empty,
                LicenseNumber = registration.LicenseNumber ?? string.Empty,
                EmailConfirmationToken = token,
                IsEmailConfirmed = true // Demo-Umgehung
            };

            _context.Pharmacies.Add(pharmacy);
            await _context.SaveChangesAsync();

            var confirmationLink = $"https://serviceapotheke.tech/verify.html?email={registration.Email}&token={token}";
            var emailBody = $"<h1>Apotheke E-Mail-Bestätigung</h1><p>Hallo {pharmacy.PharmacyName}, bitte klicken Sie auf den folgenden Link, um Ihr Apotheken-Konto zu bestätigen:</p><a href='{confirmationLink}'>Konto bestätigen</a>";
            
            try
            {
                await _emailService.SendEmailAsync(pharmacy.Email, "Bestätigen Sie Ihre Apotheke - Service Apotheke", emailBody);
                return Ok(new { message = "Registrierung erfolgreich. Bitte überprüfen Sie Ihre E-Mails." });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("==========================================================================");
                Console.WriteLine($"[SMTP-WARNUNG - APOTHEKE] E-Mail-Versand fehlgeschlagen: {ex.Message}");
                Console.WriteLine($"[TEST-LINK] Kopieren Sie diesen Link zur Aktivierung:");
                Console.WriteLine(confirmationLink);
                Console.WriteLine("==========================================================================");
                Console.ResetColor();

                return Ok(new { 
                    message = "Registrierung erfolgreich. (Prüfen Sie das Server-Terminal für den Bestätigungslink)",
                    debugLink = confirmationLink 
                });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            var pharmacy = await _context.Pharmacies.SingleOrDefaultAsync(u => u.Email == email);
            
            if (pharmacy == null || pharmacy.EmailConfirmationToken != token)
            {
                string errorHtml = "<!DOCTYPE html><html lang='de'><body style='font-family:sans-serif; background:#f5f9ff; display:flex; align-items:center; justify-content:center; height:100vh;'><div style='background:white; padding:40px; border-radius:8px; box-shadow:0 4px 10px rgba(0,0,0,0.1); text-align:center;'><h2 style='color:#c01c28;'>Ungültiger Bestätigungslink</h2><p>Der Link ist fehlerhaft oder wurde bereits verwendet.</p></div></body></html>";
                return Content(errorHtml, "text/html");
            }

            pharmacy.IsEmailConfirmed = true;
            pharmacy.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();

            string successHtml = @"
            <!DOCTYPE html>
            <html lang='de'>
            <head><meta charset='utf-8'><title>Konto bestätigt</title></head>
            <body style='font-family:sans-serif; background:#f5f9ff; display:flex; align-items:center; justify-content:center; height:100vh;'>
                <div style='background:white; padding:40px; border-radius:8px; box-shadow:0 4px 10px rgba(0,0,0,0.1); text-align:center;'>
                    <h2 style='color:#26a269;'>Apotheken-Konto erfolgreich bestätigt!</h2>
                    <p style='color:#333; margin-top:10px;'>Sie können diesen Tab schließen und sich nun anmelden.</p>
                </div>
            </body>
            </html>";

            return Content(successHtml, "text/html");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var pharmacy = await _context.Pharmacies.SingleOrDefaultAsync(p => p.Email == login.Email);
            
            if (pharmacy == null || !BCrypt.Net.BCrypt.Verify(login.Password, pharmacy.PasswordHash))
            {
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", pharmacy.Id.ToString()),
                    new Claim(ClaimTypes.Email, pharmacy.Email),
                    new Claim(ClaimTypes.Role, "Pharmacy")
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { 
                token = tokenHandler.WriteToken(token), 
                id = pharmacy.Id.ToString(), 
                fullName = pharmacy.PharmacyName 
            });
        }

        [HttpGet("{id}/full-details")]
        public async Task<IActionResult> GetFullDetails(int id)
        {
            var pharmacy = await _context.Pharmacies.FindAsync(id);
            if (pharmacy == null) return NotFound(new { message = "Apotheke nicht gefunden." });
            
            pharmacy.PasswordHash = string.Empty; 
            return Ok(pharmacy);
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdatePharmacyProfileDto dto)
        {
            var pharmacy = await _context.Pharmacies.FindAsync(id);
            if (pharmacy == null) return NotFound(new { message = "Apotheke nicht gefunden." });

            // Aktualisiere nur die Felder, die im neuen Pharmacy-Modell existieren
            pharmacy.ContactPerson = dto.ContactPerson;
            pharmacy.SoftwareSystem = dto.SoftwareSystem;
            pharmacy.FocusAreas = dto.FocusAreas;
            pharmacy.StaffSupport = dto.StaffSupport;
            pharmacy.InvoiceBillingPossible = dto.InvoiceBillingPossible;
            pharmacy.TargetHourlyRate = dto.TargetHourlyRate;
            pharmacy.ParkingAvailable = dto.ParkingAvailable;
            pharmacy.AccommodationProvided = dto.AccommodationProvided;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }
    }
}