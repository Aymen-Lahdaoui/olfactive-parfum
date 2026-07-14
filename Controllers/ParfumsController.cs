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

        public ParfumsController(AppDbContext context)
        {
            _context = context;
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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Erreur lors de la mise à jour.");
            }

            return NoContent();
        }
    }
}