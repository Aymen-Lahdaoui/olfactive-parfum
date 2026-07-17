using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlfactiveParfum.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParfumsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Services.IAuditLogService _auditLogService;

        public ParfumsController(AppDbContext context, Services.IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Parfum>>> GetParfums()
        {
            return await _context.Parfums.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Parfum>> PostParfum(Parfum parfum)
        {
            _context.Parfums.Add(parfum);
            await _context.SaveChangesAsync();

            await _auditLogService.CreateLogAsync("admin@olfactive.com", "Administrateur", "PRODUIT_AJOUT", $"Ajout du produit '{parfum.Nom}' au catalogue (Prix : {parfum.Prix} €).");

            return CreatedAtAction(nameof(GetParfums), new { id = parfum.Id }, parfum);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutParfum(int id, Parfum parfum)
        {
            if (id != parfum.Id)
            {
                return BadRequest("L'ID du produit ne correspond pas.");
            }

            var parfumExistant = await _context.Parfums.FindAsync(id);
            if (parfumExistant == null)
            {
                return NotFound();
            }

            parfumExistant.Nom = parfum.Nom;
            parfumExistant.Description = parfum.Description;
            parfumExistant.Prix = parfum.Prix;
            parfumExistant.Stock = parfum.Stock;

            try
            {
                await _context.SaveChangesAsync();
                await _auditLogService.CreateLogAsync("admin@olfactive.com", "Administrateur", "PRODUIT_MAJ", $"Mise à jour du produit n°{id} : '{parfum.Nom}' (Prix : {parfum.Prix} €, Stock : {parfum.Stock}).");
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Erreur lors de la mise à jour.");
            }

            return NoContent();
        }
    }
}