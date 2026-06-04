using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly DataContext _context;

        public FeedbackController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("pharmacist")]
        public async Task<IActionResult> SubmitPharmacistFeedback([FromBody] PharmacistFeedback feedback)
        {
            feedback.CreatedAt = DateTime.UtcNow;
            _context.PharmacistFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Apotheker-Feedback erfolgreich gespeichert." });
        }

        [HttpPost("pharmacy")]
        public async Task<IActionResult> SubmitPharmacyFeedback([FromBody] PharmacyFeedback feedback)
        {
            feedback.CreatedAt = DateTime.UtcNow;
            _context.PharmacyFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Apotheken-Feedback erfolgreich gespeichert." });
        }
    }
}