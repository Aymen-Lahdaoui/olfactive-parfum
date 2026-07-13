using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;

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
    }
}