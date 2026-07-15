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
                }

                role = "Admin";
            }

            var user = new User
            {
                Nom = model.Nom,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = role,
                Telephone = model.Telephone
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inscription réussie." });
        }

        // POST: api/auth/register-staff
        [HttpPost("register-staff")]
        public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffDto model)
        {
            // 1. Vérifier si l'adresse email existe déjà
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Cette adresse électronique est déjà utilisée par un autre membre." });
            }

            // 2. Création du compte staff avec le rôle spécifié
            var user = new User
            {
                Nom = model.Nom,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = string.IsNullOrEmpty(model.Role) ? "Livreur" : model.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Membre du personnel enregistré avec succès." });
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

        // PUT: api/auth/update
        [HttpPost("update")] // Changé en HttpPost ou HttpPut selon tes besoins, gardons cohérent avec ton front
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.AncienEmail))
            {
                return BadRequest(new { message = "Données de requête invalides." });
            }

            // 1. Recherche de l'utilisateur par son adresse email actuelle
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.AncienEmail);
            
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // 2. Vérifier si la nouvelle adresse est déjà prise par un autre compte
            if (model.Email != model.AncienEmail)
            {
                var emailExiste = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExiste)
                {
                    return BadRequest(new { message = "Cette adresse électronique est déjà associée à un autre compte." });
                }
            }

            // 3. Mise à jour des valeurs
            user.Nom = model.Nom;
            user.Email = model.Email;
            user.Telephone = model.Telephone;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Profil mis à jour avec succès.",
                nom = user.Nom,
                email = user.Email,
                telephone = user.Telephone
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
        public string Telephone { get; set; } = string.Empty;
    }

    public class RegisterStaffDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Livreur";
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string AncienEmail { get; set; } = string.Empty;
    }
}