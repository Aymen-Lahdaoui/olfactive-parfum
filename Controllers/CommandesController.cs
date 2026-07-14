using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data; // Utilise uniquement le namespace correct
using OlfactiveParfum.Backend.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OlfactiveParfum.Backend.Controllers // Ton namespace doit correspondre à la structure de tes dossiers
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
        public async Task<IActionResult> GetCommandes([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("L'email est requis.");
            }

            var userOrders = await _context.Commandes
                .Include(c => c.Articles)
                .Where(c => c.ClientEmail.ToLower() == email.ToLower())
                .OrderByDescending(c => c.DateCommande)
                .ToListAsync();

            return Ok(userOrders);
        }

        [HttpPost]
        public async Task<IActionResult> CreerCommande([FromBody] Commande nouvelleCommande)
        {
            if (nouvelleCommande == null || nouvelleCommande.Articles == null || !nouvelleCommande.Articles.Any())
            {
                return BadRequest("Données de la commande invalides.");
            }

            nouvelleCommande.Id = "OLF-" + new Random().Next(10000, 99999).ToString();
            nouvelleCommande.DateCommande = DateTime.Now;
            nouvelleCommande.Statut = "En cours";

            foreach (var article in nouvelleCommande.Articles)
            {
                var parfum = await _context.Parfums.FindAsync(article.ParfumId);
                if (parfum != null)
                {
                    parfum.Stock -= article.Quantite;
                }
            }

            _context.Commandes.Add(nouvelleCommande);
            await _context.SaveChangesAsync();

            return Ok(nouvelleCommande);
        }
    }
}