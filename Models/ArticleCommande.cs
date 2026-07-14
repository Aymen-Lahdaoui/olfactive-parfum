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
        
        // Clé étrangère (peut être nullable si l'article est créé en même temps que la commande)
        public string? CommandeId { get; set; } 
        
        // Propriété de navigation rendue nullable avec le '?' pour éviter l'erreur de validation 400
        public Commande? Commande { get; set; } 
    }
}