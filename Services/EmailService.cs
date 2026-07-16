using System.Net;
using System.Net.Mail;

namespace OlfactiveParfum.Backend.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string clientEmail, string clientNom, int orderId, List<string> articles, decimal total);
        Task SendOrderStatusUpdateAsync(string clientEmail, string clientNom, int orderId, string newStatus, string? commentaire = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // ─── Envoi d'email générique ───────────────────────────────────────────
        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            var server    = smtp["Server"];
            var portStr   = smtp["Port"];
            var sender    = smtp["SenderEmail"];
            var username  = smtp["Username"];
            var password  = smtp["Password"];
            var senderName = smtp["SenderName"] ?? "Olfactive Maison de Parfum";

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(sender) ||
                string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("[OLF-EMAIL] SMTP non configuré — email non envoyé à {Email}", toEmail);
                return;
            }

            int.TryParse(portStr, out int port);
            if (port == 0) port = 587;

            try
            {
                using var client = new SmtpClient(server, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl   = true
                };

                var mail = new MailMessage
                {
                    From       = new MailAddress(sender, senderName),
                    Subject    = subject,
                    Body       = htmlBody,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(toEmail, toName));

                await client.SendMailAsync(mail);
                _logger.LogInformation("[OLF-EMAIL] Email «{Subject}» envoyé à {Email}", subject, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OLF-EMAIL] Échec de l'envoi à {Email}", toEmail);
            }
        }

        // ─── Email de confirmation de commande ────────────────────────────────
        public async Task SendOrderConfirmationAsync(
            string clientEmail, string clientNom, int orderId,
            List<string> articles, decimal total)
        {
            var articlesHtml = string.Join("", articles.Select(a =>
                $"<tr><td style='padding:8px 0;border-bottom:1px solid #f0ede8;color:#4b4540;font-size:13px;'>{a}</td></tr>"
            ));

            var html = GetBaseTemplate(clientNom, $"Commande #{orderId} confirmée", $@"
                <p style='color:#6b6460;font-size:14px;line-height:1.7;'>
                    Nous avons bien reçu votre commande et nous la préparons avec soin.<br/>
                    Vous recevrez une notification dès que votre colis sera expédié.
                </p>
                <div style='background:#f9f7f4;border-radius:12px;padding:20px;margin:24px 0;'>
                    <p style='margin:0 0 12px;font-size:11px;text-transform:uppercase;letter-spacing:0.15em;color:#8c765c;font-weight:700;'>Vos articles</p>
                    <table style='width:100%;border-collapse:collapse;'>{articlesHtml}</table>
                    <div style='margin-top:16px;padding-top:16px;border-top:1px solid #e8e2d9;display:flex;justify-content:space-between;'>
                        <span style='font-size:12px;color:#8c765c;font-weight:700;text-transform:uppercase;letter-spacing:0.1em;'>Total</span>
                        <span style='font-size:18px;font-weight:700;color:#1c1917;'>{total:F2} €</span>
                    </div>
                </div>
                <p style='color:#9b9390;font-size:12px;text-align:center;font-style:italic;'>
                    Numéro de commande : <strong style='color:#4b4540;'>#{orderId}</strong>
                </p>
            ");

            await SendEmailAsync(clientEmail, clientNom,
                $"✨ Commande #{orderId} confirmée — Olfactive",
                html);
        }

        // ─── Email de mise à jour de statut ───────────────────────────────────
        public async Task SendOrderStatusUpdateAsync(
            string clientEmail, string clientNom, int orderId,
            string newStatus, string? commentaire = null)
        {
            var (emoji, titre, message, accentColor) = newStatus switch
            {
                "En préparation" => ("🧴", "Votre commande est en préparation",
                    "Notre équipe prépare votre sélection avec le plus grand soin. Nous vous informerons dès l'expédition.",
                    "#f97316"),
                "En cours de livraison" => ("🚚", "Votre commande est en route !",
                    "Votre fragrance est en chemin vers vous. Un livreur vous a été assigné et prendra contact avec vous pour la remise du colis.",
                    "#0284c7"),
                "Livré" => ("✅", "Votre commande a été livrée",
                    "Votre commande a bien été livrée. Nous espérons que votre fragrance vous enchantera. Profitez de votre nouvelle signature olfactive !",
                    "#059669"),
                "Annulé" => ("❌", "Votre commande a été annulée",
                    commentaire != null
                        ? $"Votre commande a malheureusement été annulée.<br/>Raison : <em>{commentaire}</em>"
                        : "Votre commande a malheureusement été annulée. Pour toute question, contactez notre service client.",
                    "#e11d48"),
                _ => ("📦", $"Mise à jour de votre commande : {newStatus}",
                    $"Le statut de votre commande #{orderId} a été mis à jour : <strong>{newStatus}</strong>",
                    "#8c765c")
            };

            var html = GetBaseTemplate(clientNom, $"{emoji} {titre}", $@"
                <div style='background:linear-gradient(135deg,{accentColor}12,{accentColor}05);border-left:3px solid {accentColor};border-radius:0 12px 12px 0;padding:16px 20px;margin:20px 0;'>
                    <p style='margin:0;font-size:14px;line-height:1.7;color:#4b4540;'>{message}</p>
                </div>
                <p style='color:#9b9390;font-size:12px;text-align:center;'>
                    Commande <strong style='color:#4b4540;'>#{orderId}</strong> • 
                    Statut : <strong style='color:{accentColor};'>{newStatus}</strong>
                </p>
            ");

            var subjectEmoji = newStatus switch
            {
                "En préparation"        => "🧴",
                "En cours de livraison" => "🚚",
                "Livré"                 => "✅",
                "Annulé"                => "❌",
                _                       => "📦"
            };

            await SendEmailAsync(clientEmail, clientNom,
                $"{subjectEmoji} Commande #{orderId} — {newStatus} | Olfactive",
                html);
        }

        // ─── Template HTML de base luxe ───────────────────────────────────────
        private static string GetBaseTemplate(string clientNom, string titre, string contentHtml)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head><meta charset='UTF-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/></head>
<body style='margin:0;padding:0;background:#f4f1ec;font-family:Georgia,serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f1ec;padding:40px 20px;'>
    <tr><td align='center'>
      <table width='580' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.06);'>

        <!-- HEADER -->
        <tr>
          <td style='background:linear-gradient(135deg,#1c1917,#292524);padding:36px 40px;text-align:center;'>
            <p style='margin:0;font-size:10px;letter-spacing:0.3em;text-transform:uppercase;color:#8c765c;font-family:Arial,sans-serif;font-weight:700;'>Maison de Parfum</p>
            <h1 style='margin:8px 0 0;font-size:28px;font-weight:300;color:#fafaf9;letter-spacing:0.05em;'>OLFACTIVE</h1>
            <div style='width:40px;height:1px;background:#8c765c;margin:16px auto 0;'></div>
          </td>
        </tr>

        <!-- CONTENT -->
        <tr>
          <td style='padding:40px;'>
            <p style='margin:0 0 8px;font-size:11px;text-transform:uppercase;letter-spacing:0.2em;color:#8c765c;font-family:Arial,sans-serif;font-weight:700;'>Bonjour,</p>
            <h2 style='margin:0 0 24px;font-size:22px;font-weight:400;color:#1c1917;letter-spacing:0.02em;'>{clientNom}</h2>
            <h3 style='margin:0 0 20px;font-size:16px;font-weight:600;color:#1c1917;font-family:Arial,sans-serif;'>{titre}</h3>
            {contentHtml}
          </td>
        </tr>

        <!-- FOOTER -->
        <tr>
          <td style='background:#f9f7f4;padding:28px 40px;text-align:center;border-top:1px solid #ede9e3;'>
            <p style='margin:0;font-size:11px;color:#a09890;font-family:Arial,sans-serif;'>
              © {DateTime.Now.Year} Olfactive Maison de Parfum — Tous droits réservés<br/>
              <span style='font-style:italic;color:#b8a898;font-size:10px;'>
                Cet email a été envoyé automatiquement, merci de ne pas y répondre.
              </span>
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
        }
    }
}
