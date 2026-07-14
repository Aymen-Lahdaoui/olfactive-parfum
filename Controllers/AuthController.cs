using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using BCrypt.Net;

namespace OlfactiveParfum.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // 1. Vérifier si l'adresse email existe déjà
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Cette adresse électronique est déjà utilisée." });
            }

            string role = "Client";

            // 2. Vérifier si l'utilisateur demande à être Admin
            if (model.Email.ToLower().Contains("admin"))
            {
                // Vérifier s'il y a déjà UN administrateur dans la base de données
                bool adminExists = await _context.Users.AnyAsync(u => u.Role == "Admin");

                if (adminExists)
                {
                    // Option stricte : On refuse l'inscription pour protéger l'accès unique
                    return BadRequest(new { message = "Un administrateur unique existe déjà. Impossible de créer un autre compte admin." });
                    
                    // Variante (si tu préfères) : Remplacer par 'role = "Client";' pour le forcer à être client.
                }

                role = "Admin";
            }

            var user = new User
            {
                Nom = model.Nom,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscription réussie." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Adresse électronique ou mot de passe incorrect." });
            }

            return Ok(new { 
                id = user.Id, 
                nom = user.Nom, 
                email = user.Email, 
                role = user.Role 
            });
        }

        // GET: api/auth/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new { 
                    u.Id, 
                    u.Nom, 
                    u.Email, 
                    u.Role, 
                    u.CreatedAt 
                })
                .ToListAsync();
                
            return Ok(users);
        }
    }

    public class RegisterDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}