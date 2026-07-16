namespace OlfactiveParfum.Backend.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // Destinataire (identifié par email pour simplifier sans JWT)
        public string UserEmail { get; set; } = string.Empty;

        // Contenu
        public string Titre    { get; set; } = string.Empty;
        public string Message  { get; set; } = string.Empty;
        public string Type     { get; set; } = "info"; // info | success | warning | error

        // Lien optionnel (ex: commande concernée)
        public int? CommandeId { get; set; }

        // Etat
        public bool IsRead  { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
