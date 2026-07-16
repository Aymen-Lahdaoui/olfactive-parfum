using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models;
using System.Collections.Concurrent;

namespace OlfactiveParfum.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // In-memory cache for verification codes: Key is "UserId_Type"
        private static readonly ConcurrentDictionary<string, VerificationCodeEntry> VerificationCodes = new();

        public ProfileController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("request-change")]
        public async Task<IActionResult> RequestChange([FromBody] RequestChangeDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.AncienEmail) || string.IsNullOrEmpty(model.Type) || string.IsNullOrEmpty(model.NewValue))
            {
                return BadRequest(new { message = "Données de requête invalides." });
            }

            var typeLower = model.Type.ToLower();
            if (typeLower != "phone" && typeLower != "email")
            {
                return BadRequest(new { message = "Type de changement invalide. Utilisez 'phone' ou 'email'." });
            }

            // 1. Recherche de l'utilisateur
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.AncienEmail);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            // 2. Si changement d'email, vérifier s'il est déjà pris
            if (typeLower == "email" && model.NewValue != model.AncienEmail)
            {
                var emailExiste = await _context.Users.AnyAsync(u => u.Email == model.NewValue);
                if (emailExiste)
                {
                    return BadRequest(new { message = "Cette adresse électronique est déjà associée à un autre compte." });
                }
            }

            // 3. Génération du code OTP à 6 chiffres
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(5);

            var entry = new VerificationCodeEntry
            {
                UserId = user.Id,
                Type = typeLower,
                NewValue = model.NewValue,
                Code = code,
                ExpirationTime = expiration
            };

            var cacheKey = $"{user.Id}_{typeLower}";
            VerificationCodes[cacheKey] = entry;

            // Simuler l'envoi (Console log de prestige)
            Console.WriteLine("\n************************************************************");
            Console.WriteLine($"[OLF-OTP] VERIFICATION CODE FOR {typeLower.ToUpper()} CHANGE");
            Console.WriteLine($"User: {user.Nom} (ID: {user.Id})");
            Console.WriteLine($"New Value: {model.NewValue}");
            Console.WriteLine($"CODE: {code}");
            Console.WriteLine($"Expires at: {expiration.ToLocalTime()}");
            Console.WriteLine("************************************************************\n");

            // Tenter d'envoyer le code réel
            try
            {
                if (typeLower == "email")
                {
                    await SendEmailAsync(model.NewValue, code);
                }
                else if (typeLower == "phone")
                {
                    await SendSmsAsync(model.NewValue, code);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OLF-OTP] Échec de l'envoi réel (SMTP ou Twilio) : {ex.Message}");
            }

            return Ok(new { 
                message = "Code de vérification envoyé avec succès.",
                debugCode = code // Renvoyé pour faciliter les tests directs si l'envoi échoue/n'est pas configuré
            });
        }

        [HttpPost("confirm-change")]
        public async Task<IActionResult> ConfirmChange([FromBody] ConfirmChangeDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.AncienEmail) || string.IsNullOrEmpty(model.Type) || string.IsNullOrEmpty(model.Code))
            {
                return BadRequest(new { message = "Données de confirmation invalides." });
            }

            var typeLower = model.Type.ToLower();
            if (typeLower != "phone" && typeLower != "email")
            {
                return BadRequest(new { message = "Type de changement invalide." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.AncienEmail);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé." });
            }

            var cacheKey = $"{user.Id}_{typeLower}";
            if (!VerificationCodes.TryGetValue(cacheKey, out var entry))
            {
                return BadRequest(new { message = "Aucun code de vérification n'a été demandé ou le code a expiré." });
            }

            // Vérifier l'expiration
            if (DateTime.UtcNow > entry.ExpirationTime)
            {
                VerificationCodes.TryRemove(cacheKey, out _);
                return BadRequest(new { message = "Le code de vérification a expiré (limite de 5 minutes)." });
            }

            // Vérifier le code
            if (entry.Code != model.Code)
            {
                return BadRequest(new { message = "Code de vérification incorrect." });
            }

            // Mettre à jour l'utilisateur dans SQL Server
            if (typeLower == "email")
            {
                user.Email = entry.NewValue;
            }
            else if (typeLower == "phone")
            {
                user.Telephone = entry.NewValue;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Supprimer le code utilisé du cache
            VerificationCodes.TryRemove(cacheKey, out _);

            return Ok(new {
                message = "Coordonnée mise à jour avec succès.",
                newValue = entry.NewValue,
                type = typeLower
            });
        }

        // Helper: Send SMTP Email
        private async Task SendEmailAsync(string targetEmail, string code)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var server = smtpSettings["Server"];
            var portVal = smtpSettings["Port"];
            var senderEmail = smtpSettings["SenderEmail"];
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("[OLF-OTP-EMAIL] SMTP configuration is empty. Fallback: skip email sending.");
                return;
            }

            int.TryParse(portVal, out int port);
            if (port == 0) port = 587;

            using (var client = new System.Net.Mail.SmtpClient(server, port))
            {
                client.Credentials = new System.Net.NetworkCredential(username, password);
                client.EnableSsl = true;

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(senderEmail, smtpSettings["SenderName"] ?? "Olfactive"),
                    Subject = "Votre code de sécurité Olfactive",
                    Body = $"Bonjour,\n\nVotre code de vérification à 6 chiffres pour modifier vos informations est : {code}\nCe code est valide pendant 5 minutes.\n\nCordialement,\nL'équipe Olfactive",
                    IsBodyHtml = false
                };
                mailMessage.To.Add(targetEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"[OLF-OTP-EMAIL] Real verification email successfully sent to {targetEmail}");
            }
        }

        // Helper: Send SMS via Twilio API
        private async Task SendSmsAsync(string targetPhone, string code)
        {
            var twilioSettings = _configuration.GetSection("TwilioSettings");
            var accountSid = twilioSettings["AccountSid"];
            var authToken = twilioSettings["AuthToken"];
            var fromPhone = twilioSettings["FromPhoneNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
            {
                Console.WriteLine("[OLF-OTP-SMS] Twilio configuration is empty. Fallback: skip SMS sending.");
                return;
            }

            using (var client = new HttpClient())
            {
                var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("To", targetPhone),
                    new KeyValuePair<string, string>("From", fromPhone),
                    new KeyValuePair<string, string>("Body", $"Olfactive : Votre code de verification est {code}. Valide pendant 5 minutes.")
                });
                
                request.Content = content;

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[OLF-OTP-SMS] Real SMS successfully sent via Twilio to {targetPhone}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[OLF-OTP-SMS] Twilio SMS sending failed: {response.StatusCode} - {errorContent}");
                }
            }
        }
    }

    public class VerificationCodeEntry
    {
        public int UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
    }

    public class RequestChangeDto
    {
        public string AncienEmail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "phone" ou "email"
        public string NewValue { get; set; } = string.Empty;
    }

    public class ConfirmChangeDto
    {
        public string AncienEmail { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "phone" ou "email"
        public string Code { get; set; } = string.Empty;
    }
}
