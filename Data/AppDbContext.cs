using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Models; // TRÈS IMPORTANT : Doit pointer vers ton dossier Models

namespace OlfactiveParfum.Backend.Data 
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Parfum> Parfums { get; set; }
        public DbSet<Commande> Commandes { get; set; }
        public DbSet<ArticleCommande> ArticlesCommandes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AvisLivreur> AvisLivreurs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}