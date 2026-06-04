using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;

        public AdminController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("pharmacists")]
        public async Task<IActionResult> GetPharmacists()
        {
            var users = await _context.Pharmacists
                .Select(p => new { p.Id, p.FullName, p.Email, p.IsVerified, p.ApprobationDocumentPath, p.CvDocumentPath })
                .ToListAsync();
            return Ok(users);
        }

        [HttpGet("pharmacies")]
        public async Task<IActionResult> GetPharmacies()
        {
            var users = await _context.Pharmacies
                .Select(p => new { p.Id, p.PharmacyName, p.Email, p.IsVerified })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("pharmacist/{id}/verify")]
        public async Task<IActionResult> VerifyPharmacist(int id)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();
            
            user.IsVerified = !user.IsVerified;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Apotheker-Status geändert auf: {(user.IsVerified ? "Verifiziert" : "Gesperrt")}" });
        }

        [HttpPut("pharmacy/{id}/verify")]
        public async Task<IActionResult> VerifyPharmacy(int id)
        {
            var user = await _context.Pharmacies.FindAsync(id);
            if (user == null) return NotFound();

            user.IsVerified = !user.IsVerified;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Apotheken-Status geändert auf: {(user.IsVerified ? "Verifiziert" : "Gesperrt")}" });
        }
    }
}