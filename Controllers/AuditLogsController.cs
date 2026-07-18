using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/auditlogs — Obtenir tous les logs triés par date décroissante
        [HttpGet]
        public async Task<IActionResult> GetLogs([FromQuery] string? search, [FromQuery] int limit = 200)
        {
            IQueryable<AuditLog> query = _context.AuditLogs;

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(l => 
                    l.UserEmail.ToLower().Contains(lowerSearch) || 
                    l.UserNom.ToLower().Contains(lowerSearch) || 
                    l.Action.ToLower().Contains(lowerSearch) || 
                    l.Description.ToLower().Contains(lowerSearch)
                );
            }

            var logs = await query
                .OrderByDescending(l => l.DateAction)
                .Take(limit)
                .ToListAsync();

            return Ok(logs);
        }

        // POST: api/auditlogs — Créer manuellement un log (utile si le frontend veut enregistrer une action)
        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] CreateLogRequest request)
        {
            var log = new AuditLog
            {
                DateAction = System.DateTime.UtcNow,
                UserEmail = request.UserEmail,
                UserNom = request.UserNom,
                UserRole = request.UserRole,
                Action = request.Action,
                Description = request.Description
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(log);
        }

        // DELETE: api/auditlogs/clear — Vider les logs (uniquement par l'admin)
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearLogs([FromQuery] string email)
        {
            // Vérifier si c'est bien l'admin
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null || user.Role != "Admin")
            {
                return StatusCode(403, new { message = "Accès refusé. Réservé aux administrateurs." });
            }

            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Journal d'activité vidé avec succès." });
        }
    }

    public class CreateLogRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserNom { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
