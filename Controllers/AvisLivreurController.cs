using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/avis-livreur")]
    public class AvisLivreurController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Services.IAuditLogService _auditLogService;

        public AvisLivreurController(AppDbContext context, Services.IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: api/avis-livreur/client — Récupérer toutes les évaluations laissées par un client
        [HttpGet("client")]
        public async Task<IActionResult> GetAvisParClient([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email requis." });

            var avis = await _context.AvisLivreurs
                .Where(a => a.ClientEmail.ToLower() == email.ToLower())
                .ToListAsync();

            return Ok(avis);
        }

        // GET: api/avis-livreur/{livreurId} — Récupérer les avis d'un livreur
        [HttpGet("{livreurId}")]
        public async Task<IActionResult> GetAvisLivreur(int livreurId)
        {
            var avis = await _context.AvisLivreurs
                .Where(a => a.LivreurId == livreurId)
                .OrderByDescending(a => a.DateAvis)
                .ToListAsync();

            return Ok(avis);
        }

        // GET: api/avis-livreur/{livreurId}/stats — Statistiques (moyenne, total) d'un livreur
        [HttpGet("{livreurId}/stats")]
        public async Task<IActionResult> GetStats(int livreurId)
        {
            var notes = await _context.AvisLivreurs
                .Where(a => a.LivreurId == livreurId)
                .Select(a => a.Note)
                .ToListAsync();

            if (notes.Count == 0)
                return Ok(new { moyenne = 0.0, total = 0 });

            return Ok(new
            {
                moyenne = Math.Round(notes.Average(), 1),
                total = notes.Count
            });
        }

        // GET: api/avis-livreur/stats-all — Statistiques globales de tous les livreurs (pour l'admin)
        [HttpGet("stats-all")]
        public async Task<IActionResult> GetAllStats()
        {
            var stats = await _context.AvisLivreurs
                .GroupBy(a => a.LivreurId)
                .Select(g => new
                {
                    livreurId = g.Key,
                    moyenne = Math.Round(g.Average(a => (double)a.Note), 1),
                    total = g.Count()
                })
                .ToListAsync();

            return Ok(stats);
        }

        // POST: api/avis-livreur — Poster un avis sur un livreur
        [HttpPost]
        public async Task<IActionResult> PostAvisLivreur([FromBody] PostAvisLivreurRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientEmail))
                return BadRequest(new { message = "Email du client requis." });

            if (request.Note < 1 || request.Note > 5)
                return BadRequest(new { message = "La note doit être comprise entre 1 et 5." });

            // Trouver la commande correspondante
            var commande = await _context.Commandes.FindAsync(request.CommandeId);
            if (commande == null)
                return NotFound(new { message = "Commande non trouvée." });

            // Vérifier que la commande appartient bien au client
            if (commande.ClientEmail.ToLower() != request.ClientEmail.ToLower())
                return BadRequest(new { message = "Cette commande ne vous appartient pas." });

            // Vérifier que la commande est livrée
            if (commande.Statut != "Livré")
                return BadRequest(new { message = "Vous ne pouvez évaluer le livreur que pour une commande livrée." });

            // Vérifier qu'un livreur a bien été assigné
            if (!commande.LivreurId.HasValue)
                return BadRequest(new { message = "Aucun livreur n'était assigné à cette commande." });

            // Vérifier si un avis existe déjà pour cette commande
            var dejaEvalue = await _context.AvisLivreurs.AnyAsync(a => a.CommandeId == request.CommandeId);
            if (dejaEvalue)
                return BadRequest(new { message = "Vous avez déjà évalué le livreur pour cette commande." });

            var avis = new AvisLivreur
            {
                LivreurId = commande.LivreurId.Value,
                ClientEmail = request.ClientEmail.ToLower(),
                ClientNom = request.ClientNom,
                Note = request.Note,
                Commentaire = request.Commentaire ?? string.Empty,
                CommandeId = request.CommandeId,
                DateAvis = DateTime.UtcNow
            };

            _context.AvisLivreurs.Add(avis);
            await _context.SaveChangesAsync();

            var nameLivreur = await _context.Users.Where(u => u.Id == commande.LivreurId.Value).Select(u => u.Nom).FirstOrDefaultAsync() ?? "Livreur";
            await _auditLogService.CreateLogAsync(request.ClientEmail, request.ClientNom, "LIVREUR_EVALUATION", $"Évaluation du livreur {nameLivreur} par {request.ClientNom} (Commande n°{request.CommandeId}, Note : {request.Note}/5).");

            return Ok(new { message = "Évaluation du livreur enregistrée avec succès !", avis });
        }
    }

    public class PostAvisLivreurRequest
    {
        public int CommandeId { get; set; }
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientNom { get; set; } = string.Empty;
        public int Note { get; set; }
        public string? Commentaire { get; set; }
    }
}
