using System;

namespace OlfactiveParfum.Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public DateTime DateAction { get; set; } = DateTime.UtcNow;
        public string UserEmail { get; set; } = string.Empty;
        public string UserNom { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
