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
        private readonly IAuditLogService _auditLogService;

        public CommandesController(AppDbContext context, IEmailService emailService, INotificationService notificationService, IAuditLogService auditLogService)
        {
            _context             = context;
            _emailService        = emailService;
            _notificationService = notificationService;
            _auditLogService     = auditLogService;
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

            // Populate LivreurNom manually to avoid circular refs and BDD migrations
            var tempLivreurIds = result.Where(c => c.LivreurId.HasValue).Select(c => c.LivreurId!.Value).Distinct().ToList();
            if (tempLivreurIds.Any())
            {
                var map = await _context.Users
                    .Where(u => tempLivreurIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Nom);
                foreach (var c in result)
                {
                    if (c.LivreurId.HasValue && map.TryGetValue(c.LivreurId.Value, out var name))
                    {
                        c.LivreurNom = name;
                    }
                }
            }

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

            // Notification in-app de confirmation de commande
            await _notificationService.CreateAsync(
                nouvelleCommande.ClientEmail,
                "Confirmation de commande",
                $"Votre commande n°{nouvelleCommande.Id} a bien été enregistrée. Notre équipe prendra en charge votre demande dans les meilleurs délais.",
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

            await _auditLogService.CreateLogAsync(nouvelleCommande.ClientEmail, nouvelleCommande.ClientNom, "COMMANDE_CREATION", $"Commande n°{nouvelleCommande.Id} enregistrée par le client {nouvelleCommande.ClientNom}.");

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

            await _auditLogService.CreateLogAsync("admin@olfactive.com", "Administrateur", "COMMANDE_ASSIGNATION", $"La commande n°{commande.Id} a été attribuée au livreur ID: {request.LivreurId}.");

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

            if (commande.LivreurId.HasValue)
            {
                var livreur = await _context.Users.FindAsync(commande.LivreurId.Value);
                if (livreur != null)
                {
                    var (livreurTitre, livreurMessage) = newStatut switch
                    {
                        "En cours de livraison" => (
                            "Prise en charge de livraison",
                            $"Vous avez pris en charge la commande n°{commande.Id} au nom de {commande.ClientNom}. Merci de procéder à la livraison selon les instructions."
                        ),
                        "Livré" => (
                            "Livraison finalisée",
                            $"La commande n°{commande.Id} a été confirmée comme livrée. Merci pour votre service."
                        ),
                        "Annulé" => (
                            "Livraison annulée",
                            $"La commande n°{commande.Id} qui vous était attribuée a été annulée. Aucune action n'est requise de votre part."
                        ),
                        _ => (
                            $"Mise à jour — Commande n°{commande.Id}",
                            $"Le statut de la commande n°{commande.Id} a été mis à jour : {newStatut}."
                        )
                    };
                    await _notificationService.CreateAsync(livreur.Email, livreurTitre, livreurMessage, type, commande.Id);
                }
            }

            // ✉️ Email au client
            _ = _emailService.SendOrderStatusUpdateAsync(commande.ClientEmail, commande.ClientNom, commande.Id, newStatut);

            await _auditLogService.CreateLogAsync("système@olfactive.com", "Système", "COMMANDE_STATUT_MAJ", $"Changement de statut pour la commande n°{commande.Id} : '{newStatut}'.");

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

            // Notification in-app — annulation de commande
            await _notificationService.CreateAsync(
                commande.ClientEmail,
                "Annulation de commande",
                string.IsNullOrEmpty(request.Commentaire)
                    ? $"Nous vous informons que votre commande n°{commande.Id} a été annulée. Pour toute question, veuillez contacter notre service client."
                    : $"Votre commande n°{commande.Id} a été annulée. Motif communiqué : {request.Commentaire}. Nous restons à votre disposition pour tout renseignement.",
                "error",
                commande.Id
            );

            // ✉️ Email
            _ = _emailService.SendOrderStatusUpdateAsync(commande.ClientEmail, commande.ClientNom, commande.Id, "Annulé", request.Commentaire);

            await _auditLogService.CreateLogAsync(commande.ClientEmail, commande.ClientNom, "COMMANDE_ANNULATION", $"La commande n°{commande.Id} a été annulée. Raison : {request.Commentaire}");

            return Ok(new { message = "Commande annulée avec succès !" });
        }

        private static (string titre, string message, string type) GetStatusNotificationContent(string statut, int commandeId)
        {
            return statut switch
            {
                "En préparation"        => ("Commande en cours de préparation",   $"Votre commande n°{commandeId} est actuellement en cours de préparation par nos équipes. Vous serez notifié dès l'expédition.",  "info"),
                "En cours de livraison" => ("Commande expédiée",                  $"Votre commande n°{commandeId} a été confiée à notre service de livraison et est en acheminement vers votre adresse.",          "info"),
                "Livré"                 => ("Commande livrée avec succès",         $"Votre commande n°{commandeId} a été remise à l'adresse indiquée. Nous vous souhaitons une excellente expérience olfactive.",  "success"),
                "Annulé"                => ("Annulation de commande",              $"Votre commande n°{commandeId} a été annulée. Notre service client reste à votre disposition pour tout complément d'information.", "error"),
                _                       => ("Mise à jour de votre commande",       $"Le statut de votre commande n°{commandeId} a été mis à jour : {statut}.",                                                         "info")
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