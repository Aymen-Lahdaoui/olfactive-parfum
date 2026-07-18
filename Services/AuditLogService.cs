using System;
using System.Threading.Tasks;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Services
{
    public interface IAuditLogService
    {
        Task CreateLogAsync(string email, string nom, string role, string action, string description);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateLogAsync(string email, string nom, string role, string action, string description)
        {
            try
            {
                var log = new AuditLog
                {
                    DateAction = DateTime.UtcNow,
                    UserEmail = email ?? "système@olfactive.com",
                    UserNom = nom ?? "Système",
                    UserRole = role ?? "Système",
                    Action = action ?? "INCONNU",
                    Description = description ?? string.Empty
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Silently log/fail to console to prevent breaking core requests
                Console.WriteLine($"[AuditLog Error] {ex.Message}");
            }
        }
    }
}
