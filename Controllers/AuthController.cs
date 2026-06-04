using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Services;
using System;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;

        public AuthController(DataContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto req)
        {
            string token = new Random().Next(100000, 999999).ToString();
            
            if (req.Role.ToLower() == "pharmacist")
            {
                var user = await _context.Pharmacists.SingleOrDefaultAsync(u => u.Email == req.Email);
                if (user == null) return Ok(new { message = "Falls die E-Mail existiert, wurde ein Code gesendet." }); 
                user.EmailConfirmationToken = token; 
                await _context.SaveChangesAsync();
            }
            else
            {
                var user = await _context.Pharmacies.SingleOrDefaultAsync(u => u.Email == req.Email);
                if (user == null) return Ok(new { message = "Falls die E-Mail existiert, wurde ein Code gesendet." });
                user.EmailConfirmationToken = token;
                await _context.SaveChangesAsync();
            }

            try 
            { 
                await _emailService.SendEmailAsync(req.Email, "Ihr Passwort-Reset Code", $"Ihr 6-stelliger Code lautet: {token}"); 
            } 
            catch 
            { 
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[TEST-CODE] Reset-Code für {req.Email}: {token}"); 
                Console.ResetColor();
            }

            return Ok(new { message = "Falls die E-Mail existiert, wurde ein Code gesendet." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto req)
        {
            string newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

            if (req.Role.ToLower() == "pharmacist")
            {
                var user = await _context.Pharmacists.SingleOrDefaultAsync(u => u.Email == req.Email && u.EmailConfirmationToken == req.Token);
                if (user == null) return BadRequest(new { message = "Ungültiger oder abgelaufener Code." });
                user.PasswordHash = newHash;
                user.EmailConfirmationToken = null;
                await _context.SaveChangesAsync();
            }
            else
            {
                var user = await _context.Pharmacies.SingleOrDefaultAsync(u => u.Email == req.Email && u.EmailConfirmationToken == req.Token);
                if (user == null) return BadRequest(new { message = "Ungültiger oder abgelaufener Code." });
                user.PasswordHash = newHash;
                user.EmailConfirmationToken = null;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Passwort erfolgreich geändert." });
        }
    }
}