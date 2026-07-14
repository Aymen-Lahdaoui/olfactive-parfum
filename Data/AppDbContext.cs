using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Models; // TRÈS IMPORTANT : Doit pointer vers ton dossier Models

namespace OlfactiveParfum.Backend.Data 
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Parfum> Parfums { get; set; }
        public DbSet<Commande> Commandes { get; set; } // L'erreur disparaîtra si le "using" ci-dessus est correct
        public DbSet<ArticleCommande> ArticlesCommandes { get; set; }
    }
}