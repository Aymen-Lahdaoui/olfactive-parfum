using System;
using System.Collections.Generic;

namespace OlfactiveParfum.Backend.Models
{
    public class Commande
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Génère automatiquement un ID unique si absent
        public string ClientEmail { get; set; }
        public string ClientNom { get; set; } // <-- Ajouté pour correspondre au frontend
        public decimal Total { get; set; }     // <-- Ajouté pour correspondre au frontend
        public DateTime DateCommande { get; set; }
        public string Statut { get; set; } = "En cours";
        public List<ArticleCommande> Articles { get; set; } = new List<ArticleCommande>();
    }
}