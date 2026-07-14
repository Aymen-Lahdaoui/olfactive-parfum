using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data; 
using OlfactiveParfum.Backend.Models;
using System;
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

            // Coupe la boucle de sérialisation pour le GET également
            foreach (var order in userOrders)
            {
                if (order.Articles != null)
                {
                    foreach (var article in order.Articles)
                    {
                        article.Commande = null;
                    }
                }
            }

            return Ok(userOrders);
        }

        [HttpPost]
        public async Task<IActionResult> CreerCommande([FromBody] Commande nouvelleCommande)
        {
            // 1. Sécurité : On ignore la validation automatique du parent pour chaque article reçu
            if (nouvelleCommande?.Articles != null)
            {
                for (int i = 0; i < nouvelleCommande.Articles.Count; i++)
                {
                    ModelState.Remove($"Articles[{i}].Commande");
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (nouvelleCommande == null || nouvelleCommande.Articles == null || !nouvelleCommande.Articles.Any())
            {
                return BadRequest("Données de la commande invalides.");
            }

            // 2. Initialisation des valeurs de la commande
            nouvelleCommande.Id = "OLF-" + new Random().Next(10000, 99999).ToString();
            nouvelleCommande.DateCommande = DateTime.UtcNow; // Compatible PostgreSQL
            nouvelleCommande.Statut = "En cours";

            // 3. Mise à jour des stocks
            foreach (var article in nouvelleCommande.Articles)
            {
                var parfum = await _context.Parfums.FindAsync(article.ParfumId);
                if (parfum == null)
                {
                    return BadRequest($"Le parfum avec l'ID {article.ParfumId} n'existe pas en base de données.");
                }

                // Décrémente le stock du parfum
                parfum.Stock -= article.Quantite;
            }

            try
            {
                _context.Commandes.Add(nouvelleCommande);
                await _context.SaveChangesAsync();

                // 4. CRUCIAL : On coupe la référence cyclique avant de renvoyer le JSON
                if (nouvelleCommande.Articles != null)
                {
                    foreach (var article in nouvelleCommande.Articles)
                    {
                        article.Commande = null; // Empêche le convertisseur JSON de boucler à l'infini
                    }
                }

                return Ok(nouvelleCommande);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR CRITIQUE DETECTEE] : {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DETAILS] : {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Erreur serveur lors de la sauvegarde : {ex.Message}");
            }
        }
    }
}