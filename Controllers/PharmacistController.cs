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
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });

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
            return Ok(new { message = "Registrierung erfolgreich." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null || user.EmailConfirmationToken != token) return Content("Fehler", "text/html");
            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();
            return Content("Bestätigt", "text/html");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });
            
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email) }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), id = user.Id.ToString(), fullName = user.FullName });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacist(int id)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();
            user.PasswordHash = string.Empty;
            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePharmacistGeneral(int id, [FromBody] PharmacistUpdateGeneralDto dto)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.Phone ?? user.PhoneNumber;
            user.Address = dto.Address ?? user.Address;
            user.RadiusKm = dto.MaxDistanceKm;
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil aktualisiert." });
        }

        [HttpGet("{id}/upcoming-shifts")]
        public async Task<IActionResult> GetUpcomingShifts(int id)
        {
            var shifts = await _context.JobApplications
                .Include(a => a.JobPost).ThenInclude(j => j.Pharmacy)
                .Where(a => a.PharmacistId == id && a.Status == "Accepted")
                .Select(a => new {
                    Id = a.Id,
                    StartTime = a.JobPost.StartTime,
                    EndTime = a.JobPost.EndTime,
                    Salary = a.JobPost.Salary,
                    StartDate = a.JobPost.StartDate,
                    PharmacyName = a.JobPost.Pharmacy != null ? a.JobPost.Pharmacy.PharmacyName : "Apotheke"
                }).ToListAsync();
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
                .Where(a => a.PharmacistId == id && a.Status == "Completed")
                .ToListAsync();
            return Ok(shifts);
        }

        [HttpPost("{id}/upload-cv")]
        public async Task<IActionResult> UploadCv(int id, IFormFile file)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();
            user.CvDocumentPath = file.FileName;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public class PharmacistUpdateGeneralDto 
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int MaxDistanceKm { get; set; }
    }
}