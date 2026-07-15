public class ArticleCommande {
    public int Id { get; set; }
    public int CommandeId { get; set; } // Doit correspondre à l'ID de Commande
    [System.Text.Json.Serialization.JsonIgnore]
    public Commande? Commande { get; set; } // Propriété de navigation (nullable pour éviter la validation)
    
    public int ParfumId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}