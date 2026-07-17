// Modèle mis à jour : Commande.cs
public class Commande {
    public int Id { get; set; }

    public string ClientNom { get; set; } = string.Empty;
    // Ajout du '?' pour rendre ces champs optionnels
    public string? ClientAdresse { get; set; }
    public string? ClientTelephone { get; set; }
    public string ClientEmail { get; set; } = string.Empty;

    public string Statut { get; set; } = "EN_ATTENTE";
    public DateTime DateCommande { get; set; } = DateTime.UtcNow;

    public int? LivreurId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public User? Livreur { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? LivreurNom { get; set; }

    public string? CommentaireAnnulation { get; set; }

    public List<ArticleCommande> Articles { get; set; } = new List<ArticleCommande>();
}