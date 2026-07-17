namespace OlfactiveParfum.Backend.Models
{
    public class AvisLivreur
    {
        public int Id { get; set; }
        public int LivreurId { get; set; }
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientNom { get; set; } = string.Empty;
        public int Note { get; set; } // 1 à 5
        public string Commentaire { get; set; } = string.Empty;
        public int CommandeId { get; set; }
        public DateTime DateAvis { get; set; } = DateTime.UtcNow;
    }
}
