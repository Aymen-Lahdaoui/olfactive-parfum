using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Models; // Assure-toi que le fichier User.cs est bien dans ce dossier Models

namespace OlfactiveParfum.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Parfum> Parfums { get; set; }
        
        // La table des utilisateurs
        public DbSet<User> Users { get; set; }
    }
}