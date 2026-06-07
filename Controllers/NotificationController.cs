using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly DataContext _context;

        public NotificationController(DataContext context)
        {
            _context = context;
        }

        // Wird vom Frontend aufgerufen: /api/Notification/user/{userId}
        [HttpGet("user/{userId}")]
        public IActionResult GetUserNotifications(int userId)
        {
            // Placeholder: Da wir das Benachrichtigungsmodell noch nicht kennen, 
            // geben wir eine leere Liste zurück, damit das Frontend (Status 200) erhält und keinen Fehler wirft.
            return Ok(new object[] { });
        }

        [HttpGet("user/{userId}/all")]
        public IActionResult GetAllUserNotifications(int userId)
        {
            return Ok(new object[] { });
        }

        [HttpGet("getByUser/{userId}")]
        public IActionResult GetNotificationsByUser(int userId)
        {
            return Ok(new object[] { });
        }

        // Wird vom Frontend aufgerufen: /api/Notification/mark-read/{id}
        [HttpPut("mark-read/{id}")]
        public IActionResult MarkAsRead(int id)
        {
            return Ok(new { message = "Markiert als gelesen" });
        }
    }
}