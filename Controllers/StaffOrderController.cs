using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/staff")]
    public class StaffOrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffOrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/staff/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Commandes
                .Include(c => c.Articles)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateCommande)
                .Select(c => new
                {
                    c.Id,
                    c.ClientNom,
                    c.ClientEmail,
                    c.ClientAdresse,
                    c.ClientTelephone,
                    c.Statut,
                    c.DateCommande,
                    c.LivreurId,
                    LivreurNom = c.Livreur != null ? c.Livreur.Nom : null,
                    ArticlesCount = c.Articles.Count
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PUT: api/staff/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null)
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            commande.Statut = request.NewStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Statut de la commande mis à jour avec succès.", statut = commande.Statut });
        }

        // PUT: api/staff/orders/{id}/assign-delivery
        [HttpPut("orders/{id}/assign-delivery")]
        public async Task<IActionResult> AssignDelivery(int id, [FromBody] AssignDeliveryRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null)
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            // Vérifier si le livreur existe
            var livreur = await _context.Users.FindAsync(request.LivreurId);
            if (livreur == null)
            {
                return BadRequest(new { message = "Livreur non trouvé." });
            }

            if (livreur.Role != "Livreur")
            {
                return BadRequest(new { message = "L'utilisateur sélectionné n'est pas un livreur." });
            }

            commande.LivreurId = request.LivreurId;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Commande assignée au livreur avec succès.", 
                livreurNom = livreur.Nom 
            });
        }

        // GET: api/staff/livreurs
        [HttpGet("livreurs")]
        public async Task<IActionResult> GetAllLivreurs()
        {
            var livreurs = await _context.Users
                .Where(u => u.Role == "Livreur" && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Email
                })
                .OrderBy(u => u.Nom)
                .ToListAsync();

            return Ok(livreurs);
        }
    }

    public class UpdateStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
    }

    public class AssignDeliveryRequest
    {
        public int LivreurId { get; set; }
    }
}
