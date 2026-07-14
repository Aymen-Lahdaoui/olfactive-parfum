using System;
using System.Collections.Generic;

namespace OlfactiveParfum.Backend.Models
{
    public class Commande
    {
        public string Id { get; set; }
        public string ClientEmail { get; set; }
        public DateTime DateCommande { get; set; }
        public string Statut { get; set; }
        public List<ArticleCommande> Articles { get; set; }
    }
}