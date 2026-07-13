using System;

namespace OlfactiveParfum.Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Client"; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}