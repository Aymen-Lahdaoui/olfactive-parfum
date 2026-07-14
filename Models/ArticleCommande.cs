namespace OlfactiveParfum.Backend.Models
{
    public class ArticleCommande
    {
        public int Id { get; set; }
        public int ParfumId { get; set; }
        public string Nom { get; set; }
        public int Quantite { get; set; }
        public double PrixUnitaire { get; set; }
        public string ImageUrl { get; set; }
        public string CommandeId { get; set; } // Clé étrangère
        public Commande Commande { get; set; } // Navigation
    }
}