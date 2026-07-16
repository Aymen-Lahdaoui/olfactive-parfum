using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using OlfactiveParfum.Backend.Services;
using System.Linq;
using System.Threading.Tasks;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public CommandesController(AppDbContext context, IEmailService emailService, INotificationService notificationService)
        {
            _context             = context;
            _emailService        = emailService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommandes([FromQuery] string? email, [FromQuery] int? livreurId)
        {
            IQueryable<Commande> query = _context.Commandes.Include(c => c.Articles);

            if (!string.IsNullOrEmpty(email))
                query = query.Where(c => c.ClientEmail.ToLower() == email.ToLower());

            if (livreurId.HasValue)
                query = query.Where(c => c.LivreurId == livreurId.Value);

            var result = await query.OrderByDescending(c => c.DateCommande).ToListAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreerCommande([FromBody] Commande nouvelleCommande)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            nouvelleCommande.DateCommande = System.DateTime.UtcNow;
            nouvelleCommande.Statut      = "En cours";
            nouvelleCommande.LivreurId   = null;

            _context.Commandes.Add(nouvelleCommande);
            await _context.SaveChangesAsync();

            // 🔔 Notification in-app de confirmation
            await _notificationService.CreateAsync(
                nouvelleCommande.ClientEmail,
                "Commande confirmée ✨",
                $"Votre commande #{nouvelleCommande.Id} a bien été reçue et est en cours de traitement.",
                "success",
                nouvelleCommande.Id
            );

            // ✉️ Email de confirmation
            _ = _emailService.SendOrderConfirmationAsync(
                nouvelleCommande.ClientEmail,
                nouvelleCommande.ClientNom,
                nouvelleCommande.Id,
                nouvelleCommande.Articles.Select(a => $"{a.Nom} × {a.Quantite}").ToList(),
                0m
            );

            return Ok(nouvelleCommande);
        }

        [HttpPut("{id}/assigner")]
        public async Task<IActionResult> AssignerLivreur(int id, [FromBody] AssignerLivreurRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.LivreurId = request.LivreurId;
            commande.Statut    = "Assignée";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Livreur assigné avec succès !" });
        }

        [HttpPut("{id}/statut")]
        public async Task<IActionResult> UpdateStatut(int id, [FromBody] UpdateStatutRequest request)
        {
            // Support both "Statut" and "newStatus" field names (livreur page vs staff page)
            var newStatut = request.Statut ?? request.NewStatus;
            if (string.IsNullOrEmpty(newStatut)) return BadRequest(new { message = "Statut requis." });

            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.Statut = newStatut;
            await _context.SaveChangesAsync();

            var (titre, message, type) = GetStatusNotificationContent(newStatut, commande.Id);

            // 🔔 Notification au CLIENT
            await _notificationService.CreateAsync(commande.ClientEmail, titre, message, type, commande.Id);

            // 🔔 Notification au LIVREUR s'il est assigné
            if (commande.LivreurId.HasValue)
            {
                var livreur = await _context.Users.FindAsync(commande.LivreurId.Value);
                if (livreur != null)
                {
                    var (livreurTitre, livreurMessage) = newStatut switch
                    {
                        "En cours de livraison" => (
                            "Prise en charge confirmée 🚚",
                            $"Vous avez pris en charge la commande #{commande.Id} de {commande.ClientNom}. Bonne livraison !"
                        ),
                        "Livré" => (
                            "Livraison terminée ✅",
                            $"La commande #{commande.Id} a bien été marquée comme livrée. Merci !"
                        ),
                        "Annulé" => (
                            "Livraison annulée ❌",
                            $"La commande #{commande.Id} a été annulée."
                        ),
                        _ => (
                            $"Commande #{commande.Id} mise à jour",
                            $"Statut de la commande #{commande.Id} : {newStatut}"
                        )
                    };
                    await _notificationService.CreateAsync(livreur.Email, livreurTitre, livreurMessage, type, commande.Id);
                }
            }

            // ✉️ Email au client
            _ = _emailService.SendOrderStatusUpdateAsync(commande.ClientEmail, commande.ClientNom, commande.Id, newStatut);

            return Ok(new { message = "Statut mis à jour avec succès !" });
        }

        [HttpPut("{id}/annuler")]
        public async Task<IActionResult> AnnulerCommande(int id, [FromBody] AnnulerCommandeRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null) return NotFound();

            commande.Statut                = "Annulé";
            commande.CommentaireAnnulation = request.Commentaire;

            await _context.SaveChangesAsync();

            // 🔔 Notification in-app
            await _notificationService.CreateAsync(
                commande.ClientEmail,
                "Commande annulée ❌",
                string.IsNullOrEmpty(request.Commentaire)
                    ? $"Votre commande #{commande.Id} a été annulée."
                    : $"Votre commande #{commande.Id} a été annulée. Raison : {request.Commentaire}",
                "error",
                commande.Id
            );

            // ✉️ Email
            _ = _emailService.SendOrderStatusUpdateAsync(commande.ClientEmail, commande.ClientNom, commande.Id, "Annulé", request.Commentaire);

            return Ok(new { message = "Commande annulée avec succès !" });
        }

        private static (string titre, string message, string type) GetStatusNotificationContent(string statut, int commandeId)
        {
            return statut switch
            {
                "En préparation"        => ("Commande en préparation 🧴", $"Votre commande #{commandeId} est en cours de préparation par notre équipe.", "info"),
                "En cours de livraison" => ("Commande en route ! 🚚",     $"Votre commande #{commandeId} est en chemin vers vous.", "info"),
                "Livré"                 => ("Commande livrée ✅",          $"Votre commande #{commandeId} a été livrée avec succès. Profitez de votre fragrance !", "success"),
                "Annulé"                => ("Commande annulée ❌",         $"Votre commande #{commandeId} a été annulée.", "error"),
                _                       => ("Mise à jour de commande 📦",  $"Le statut de votre commande #{commandeId} a été mis à jour : {statut}.", "info")
            };
        }
    }

    public class AssignerLivreurRequest
    {
        public int LivreurId { get; set; }
    }

    public class UpdateStatutRequest
    {
        public string? Statut    { get; set; }  // Livreur page
        public string? NewStatus { get; set; }  // Staff page (fallback)
    }

    public class AnnulerCommandeRequest
    {
        public string Commentaire { get; set; } = string.Empty;
    }
}