using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Parfum> Parfums { get; set; }
    }
}