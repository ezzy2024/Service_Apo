using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using System.IO;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("pharmacist/{id}")]
        public async Task<IActionResult> UploadDocuments(int id, IFormFile? approbation, IFormFile? cv)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound(new { message = "Benutzer nicht gefunden." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRoot, "uploads", id.ToString());

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            if (approbation != null)
            {
                var appPath = Path.Combine(uploadPath, "approb_" + approbation.FileName);
                using (var stream = new FileStream(appPath, FileMode.Create)) await approbation.CopyToAsync(stream);
                user.ApprobationDocumentPath = $"/uploads/{id}/approb_{approbation.FileName}";
            }

            if (cv != null)
            {
                var cvPath = Path.Combine(uploadPath, "cv_" + cv.FileName);
                using (var stream = new FileStream(cvPath, FileMode.Create)) await cv.CopyToAsync(stream);
                user.CvDocumentPath = $"/uploads/{id}/cv_{cv.FileName}";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dokumente hochgeladen." });
        }
    }
}