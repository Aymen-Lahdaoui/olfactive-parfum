public class User {
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty; // Initialiser avec = string.Empty corrige le warning CS8618
    public string Email { get; set; } = string.Empty; // Remis pour corriger l'erreur dans AuthController
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
    public string Telephone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}