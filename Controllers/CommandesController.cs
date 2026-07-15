using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommandesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommandes([FromQuery] string? email, [FromQuery] int? livreurId)
        {
            IQueryable<Commande> query = _context.Commandes.Include(c => c.Articles);

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(c => c.ClientEmail.ToLower() == email.ToLower());
            }

            if (livreurId.HasValue)
            {
                query = query.Where(c => c.LivreurId == livreurId.Value);
            }

            var result = await query.OrderByDescending(c => c.DateCommande).ToListAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreerCommande([FromBody] Commande nouvelleCommande)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Génération ID auto (PostgreSQL gère normalement cela avec SERIAL)
            nouvelleCommande.DateCommande = System.DateTime.UtcNow;
            nouvelleCommande.Statut = "En cours";
            nouvelleCommande.LivreurId = null;

            _context.Commandes.Add(nouvelleCommande);
            await _context.SaveChangesAsync();
            return Ok(nouvelleCommande);
        }

        [HttpPut("{id}/assigner")]
        public async Task<IActionResult> AssignerLivreur(int id, [FromBody] AssignerLivreurRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.LivreurId = request.LivreurId; // On utilise l'ID (int)
            commande.Statut = "Assignée";
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Livreur assigné avec succès !" });
        }
    }

    public class AssignerLivreurRequest
    {
        public int LivreurId { get; set; } // Changé de string à int
    }
}