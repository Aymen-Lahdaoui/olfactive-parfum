using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userEmail, string titre, string message, string type = "info", int? commandeId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(string userEmail, string titre, string message, string type = "info", int? commandeId = null)
        {
            var notif = new Notification
            {
                UserEmail  = userEmail.ToLower(),
                Titre      = titre,
                Message    = message,
                Type       = type,
                CommandeId = commandeId,
                IsRead     = false,
                CreatedAt  = DateTime.UtcNow
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();
        }
    }
}
