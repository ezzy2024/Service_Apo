using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Services;
using System;
using System.Linq;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;

        public JobController(DataContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateJobPost([FromBody] JobPost jobPost)
        {
            jobPost.CreatedAt = DateTime.UtcNow;
            jobPost.Status = "Active";
            _context.JobPosts.Add(jobPost);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Schicht erfolgreich erstellt." });
        }

        [HttpGet("pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetJobsByPharmacy(int pharmacyId)
        {
            var jobs = await _context.JobPosts
                .Include(j => j.JobApplications) 
                .Where(j => j.PharmacyId == pharmacyId)
                .ToListAsync();
            return Ok(jobs);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableJobs(
            [FromQuery] string? date, 
            [FromQuery] decimal? minSalary, 
            [FromQuery] string? software)
        {
            var query = _context.JobPosts.AsQueryable();

            query = query.Where(j => j.Status == "Active");

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(j => j.StartDate == date);
            }

            if (minSalary.HasValue)
            {
                query = query.Where(j => j.Salary >= minSalary.Value);
            }

            if (!string.IsNullOrEmpty(software))
            {
                var matchingPharmacyIds = _context.Pharmacies
                    .Where(p => p.SoftwareSystem != null && p.SoftwareSystem.Contains(software))
                    .Select(p => p.Id);

                query = query.Where(j => matchingPharmacyIds.Contains(j.PharmacyId));
            }

            var result = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
            return Ok(result);
        }

        [HttpGet("JobApplication/pharmacist/{pharmacistId}")]
        public async Task<IActionResult> GetPharmacistApplications(int pharmacistId)
        {
            var applications = await _context.JobApplications
                .Include(a => a.JobPost)
                .Where(a => a.PharmacistId == pharmacistId)
                .OrderByDescending(a => a.AppliedAt)
                .Select(a => new 
                {
                    a.Id,
                    a.JobPostId,
                    a.PharmacistId,
                    a.Status,
                    a.AppliedAt,
                    JobPost = a.JobPost != null ? new 
                    {
                        a.JobPost.RequestType,
                        a.JobPost.StartDate,
                        a.JobPost.StartTime,
                        a.JobPost.EndTime,
                        a.JobPost.Salary,
                        a.JobPost.PharmacyId
                    } : null
                })
                .ToListAsync();

            return Ok(applications);
        }

        [HttpPost("JobApplication/apply")]
        public async Task<IActionResult> ApplyForJob([FromBody] JobApplicationDto request)
        {
            bool exists = await _context.JobApplications
                .AnyAsync(a => a.JobPostId == request.JobPostId && a.PharmacistId == request.PharmacistId);

            if (exists)
            {
                return BadRequest(new { message = "Sie haben sich bereits auf diese Schicht beworben." });
            }

            var application = new JobApplication
            {
                JobPostId = request.JobPostId,
                PharmacistId = request.PharmacistId,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bewerbung erfolgreich eingereicht." });
        }

        [HttpPut("JobApplication/{id}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var application = await _context.JobApplications.FindAsync(id);
            if (application == null) return NotFound(new { message = "Bewerbung nicht gefunden." });

            application.Status = request.Status;
            await _context.SaveChangesAsync();

            var pharmacist = await _context.Pharmacists.FindAsync(application.PharmacistId);
            var jobPost = await _context.JobPosts.FindAsync(application.JobPostId);
            
            if (pharmacist != null && jobPost != null)
            {
                var pharmacy = await _context.Pharmacies.FindAsync(jobPost.PharmacyId);
                string pharmacyName = pharmacy != null ? pharmacy.PharmacyName : "Apotheke";
                
                string subject = request.Status == "Accepted" 
                    ? "Zusage für Ihre Schichtbewerbung!" 
                    : "Absage für Ihre Schichtbewerbung";

                string statusText = request.Status == "Accepted" 
                    ? "hat Ihre Bewerbung für die Schicht <strong>akzeptiert</strong>! Sie sind fest eingeplant." 
                    : "hat sich bei dieser Schicht leider für eine andere Vertretung entschieden.";

                string dateString = DateTime.TryParse(jobPost.StartDate, out var parsedDate) 
                    ? parsedDate.ToString("dd.MM.yyyy") 
                    : jobPost.StartDate;

                string emailBody = $@"
                <div style='font-family: Arial, sans-serif; color: #333;'>
                    <h2>Schicht-Update: {pharmacyName}</h2>
                    <p>Hallo {pharmacist.FullName},</p>
                    <p>{pharmacyName} {statusText}</p>
                    <div style='background: #f5f9ff; padding: 15px; border-left: 4px solid #1a5fb4; margin: 20px 0;'>
                        <strong>Datum:</strong> {dateString}<br>
                        <strong>Zeit:</strong> {jobPost.StartTime} - {jobPost.EndTime}<br>
                        <strong>Gehalt:</strong> {jobPost.Salary}€
                    </div>
                    <p>Loggen Sie sich in Ihr Dashboard ein, um den aktuellen Status zu prüfen.</p>
                </div>";

                try { await _emailService.SendEmailAsync(pharmacist.Email, subject, emailBody); } 
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[SMTP-WARNUNG] Status-Update E-Mail Fehler: {ex.Message}");
                    Console.ResetColor();
                }
            }

            return Ok(new { message = $"Status erfolgreich auf {request.Status} aktualisiert." });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}