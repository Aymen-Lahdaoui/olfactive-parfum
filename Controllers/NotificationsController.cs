using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications?email=... — Récupère toutes les notifs d'un utilisateur
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email requis." });

            var notifs = await _context.Notifications
                .Where(n => n.UserEmail == email.ToLower())
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new
                {
                    n.Id,
                    n.Titre,
                    n.Message,
                    n.Type,
                    n.CommandeId,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifs);
        }

        // GET: api/notifications/count?email=... — Nombre de notifs non lues
        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest();

            var count = await _context.Notifications
                .CountAsync(n => n.UserEmail == email.ToLower() && !n.IsRead);

            return Ok(new { count });
        }

        // PUT: api/notifications/{id}/read — Marquer une notif comme lue
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marquée comme lue." });
        }

        // PUT: api/notifications/read-all?email=... — Tout marquer comme lu
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest();

            var notifs = await _context.Notifications
                .Where(n => n.UserEmail == email.ToLower() && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifs)
                n.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{notifs.Count} notifications marquées comme lues." });
        }

        // DELETE: api/notifications/{id} — Supprimer une notif
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif == null) return NotFound();

            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification supprimée." });
        }

        // DELETE: api/notifications/clear?email=... — Vider toutes les notifs
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAll([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest();

            var notifs = await _context.Notifications
                .Where(n => n.UserEmail == email.ToLower())
                .ToListAsync();

            _context.Notifications.RemoveRange(notifs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Toutes les notifications supprimées." });
        }
    }
}
