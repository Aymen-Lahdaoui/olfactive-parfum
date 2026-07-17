using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using OlfactiveParfum.Backend.Services;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/staff")]
    public class StaffOrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public StaffOrderController(AppDbContext context, IEmailService emailService, INotificationService notificationService)
        {
            _context             = context;
            _emailService        = emailService;
            _notificationService = notificationService;
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
                    LivreurNom    = c.Livreur != null ? c.Livreur.Nom : null,
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
                return NotFound(new { message = "Commande non trouvée." });

            commande.Statut = request.NewStatus;
            await _context.SaveChangesAsync();

            var (titre, message, type) = request.NewStatus switch
            {
                "En préparation"        => ("Commande en cours de préparation",   $"Votre commande n°{commande.Id} est actuellement prise en charge par nos équipes. Vous serez informé dès la mise en expédition.",              "info"),
                "En cours de livraison" => ("Commande expédiée",                  $"Votre commande n°{commande.Id} a été remise à notre service de livraison et est en cours d'acheminement vers votre adresse.",             "info"),
                "Livré"                 => ("Commande livrée avec succès",         $"Votre commande n°{commande.Id} a été remise à l'adresse indiquée. Nous vous souhaitons une très belle expérience olfactive.",             "success"),
                "Annulé"                => ("Annulation de commande",              $"Nous vous informons que votre commande n°{commande.Id} a été annulée. Notre service client reste à votre disposition pour tout renseignement.", "error"),
                _                       => ("Mise à jour de votre commande",       $"Le statut de votre commande n°{commande.Id} a été mis à jour : {request.NewStatus}.",                                                           "info")
            };

            // 🔔 Notification in-app au client
            await _notificationService.CreateAsync(commande.ClientEmail, titre, message, type, commande.Id);

            // ✉️ Email au client
            _ = _emailService.SendOrderStatusUpdateAsync(commande.ClientEmail, commande.ClientNom, commande.Id, request.NewStatus);

            return Ok(new { message = "Statut de la commande mis à jour avec succès.", statut = commande.Statut });
        }

        // PUT: api/staff/orders/{id}/assign-delivery
        [HttpPut("orders/{id}/assign-delivery")]
        public async Task<IActionResult> AssignDelivery(int id, [FromBody] AssignDeliveryRequest request)
        {
            var commande = await _context.Commandes.FindAsync(id);
            if (commande == null)
                return NotFound(new { message = "Commande non trouvée." });

            var livreur = await _context.Users.FindAsync(request.LivreurId);
            if (livreur == null)
                return BadRequest(new { message = "Livreur non trouvé." });

            if (livreur.Role != "Livreur")
                return BadRequest(new { message = "L'utilisateur sélectionné n'est pas un livreur." });

            commande.LivreurId = request.LivreurId;
            await _context.SaveChangesAsync();

            // Notification au livreur — nouvelle assignation
            await _notificationService.CreateAsync(
                livreur.Email,
                "Nouvelle mission de livraison",
                $"La commande n°{commande.Id} destinée à {commande.ClientNom} vous a été attribuée. Veuillez prendre contact avec notre équipe pour les détails de livraison.",
                "info",
                commande.Id
            );

            return Ok(new { message = "Commande assignée au livreur avec succès.", livreurNom = livreur.Nom });
        }

        // GET: api/staff/livreurs
        [HttpGet("livreurs")]
        public async Task<IActionResult> GetAllLivreurs()
        {
            var livreurs = await _context.Users
                .Where(u => u.Role == "Livreur" && u.IsActive)
                .Select(u => new { u.Id, u.Nom, u.Email })
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
