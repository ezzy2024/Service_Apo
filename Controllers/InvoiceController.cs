using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly InvoiceService _invoiceService;

        public InvoiceController(DataContext context, InvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet("{jobId}/pharmacist/{pharmacistId}")]
        public async Task<IActionResult> DownloadInvoice(int jobId, int pharmacistId)
        {
            var job = await _context.JobPosts.FindAsync(jobId);
            var pharmacist = await _context.Pharmacists.FindAsync(pharmacistId);
            
            if (job == null || pharmacist == null) return NotFound();

            var pharmacy = await _context.Pharmacies.FindAsync(job.PharmacyId);
            if (pharmacy == null) return NotFound();

            var pdfBytes = _invoiceService.GenerateInvoice(pharmacist, pharmacy, job);

            return File(pdfBytes, "application/pdf", $"Rechnung_{jobId}.pdf");
        }
    }
}