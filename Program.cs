using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models; // Ajouté pour pouvoir manipuler la classe Parfum
using OlfactiveParfum.Backend.Services;

// ACTIVATION DU COMPORTEMENT DE COMPATIBILITÉ DES DATES POUR POSTGRESQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. INJECTION DES SERVICES (Configuration du Container)
// ============================================================

// Configuration sécurisée qui utilise les fichiers appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration du CORS pour autoriser le Frontend (React / Vite)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// PRISE EN CHARGE DES CONTRÔLEURS (Indispensable pour ParfumsController)
builder.Services.AddControllers();

// Service Email pour les notifications automatiques
builder.Services.AddScoped<IEmailService, EmailService>();

// Service de notifications in-app
builder.Services.AddScoped<INotificationService, NotificationService>();

// Service de journalisation des activités (Audit Logs)
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Support OpenAPI/.NET 9 (Généré par défaut)
builder.Services.AddOpenApi();

var app = builder.Build();

// ============================================================
// 2. SEED DATA : Insertion automatique des données de test
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // Applique automatiquement les migrations manquantes s'il y en a
        try
        {
            context.Database.Migrate();
        }
        catch (Exception migrationEx)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(migrationEx, "La migration automatique a échoué (les colonnes peuvent déjà exister). Passage au peuplement (Seeding) des données.");
        }

        // ✅ Création directe de la table Notifications si elle n'existe pas encore
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Notifications"" (
                    ""Id""          SERIAL PRIMARY KEY,
                    ""UserEmail""   TEXT        NOT NULL DEFAULT '',
                    ""Titre""       TEXT        NOT NULL DEFAULT '',
                    ""Message""     TEXT        NOT NULL DEFAULT '',
                    ""Type""        TEXT        NOT NULL DEFAULT 'info',
                    ""CommandeId""  INTEGER     NULL,
                    ""IsRead""      BOOLEAN     NOT NULL DEFAULT FALSE,
                    ""CreatedAt""   TIMESTAMP   NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserEmail"" ON ""Notifications"" (""UserEmail"");
            ");
        }
        catch (Exception sqlEx)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(sqlEx, "Création de la table Notifications ignorée (peut déjà exister).");
        }

        // ✅ Création directe de la table AvisLivreurs si elle n'existe pas encore
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""AvisLivreurs"" (
                    ""Id""          SERIAL PRIMARY KEY,
                    ""LivreurId""   INTEGER     NOT NULL,
                    ""ClientEmail"" TEXT        NOT NULL DEFAULT '',
                    ""ClientNom""   TEXT        NOT NULL DEFAULT '',
                    ""Note""        INTEGER     NOT NULL DEFAULT 5,
                    ""Commentaire"" TEXT        NOT NULL DEFAULT '',
                    ""CommandeId""  INTEGER     NOT NULL DEFAULT 0,
                    ""DateAvis""    TIMESTAMP   NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS ""IX_AvisLivreurs_LivreurId"" ON ""AvisLivreurs"" (""LivreurId"");
            ");
        }
        catch (Exception sqlExAvis)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(sqlExAvis, "Création de la table AvisLivreurs ignorée (peut déjà exister).");
        }

        // ✅ Création directe de la table AuditLogs si elle n'existe pas encore
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""Id""          SERIAL PRIMARY KEY,
                    ""DateAction""  TIMESTAMP   NOT NULL DEFAULT NOW(),
                    ""UserEmail""   TEXT        NOT NULL DEFAULT '',
                    ""UserNom""     TEXT        NOT NULL DEFAULT '',
                    ""Action""      TEXT        NOT NULL DEFAULT '',
                    ""Description""  TEXT        NOT NULL DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_DateAction"" ON ""AuditLogs"" (""DateAction"");
            ");
        }
        catch (Exception sqlExAudit)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(sqlExAudit, "Création de la table AuditLogs ignorée (peut déjà exister).");
        }

        // Si la table Parfums est vide, on ajoute des données par défaut
        if (!context.Parfums.Any())
        {
            context.Parfums.AddRange(
                new Parfum 
                { 
                    Nom = "Impression Élégante", 
                    Description = "Un sillage boisé et ambré d'une élégance rare et intemporelle.", 
                    Prix = 85.00m, 
                    Stock = 15, 
                    ImageUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601" 
                },
                new Parfum 
                { 
                    Nom = "Éclat d'Agrumes", 
                    Description = "Une fraîcheur vive et tonique mariant bergamote sauvage et mandarine.", 
                    Prix = 65.50m, 
                    Stock = 22, 
                    ImageUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f" 
                },
                new Parfum 
                { 
                    Nom = "Nuit Cuivrée", 
                    Description = "Un parfum mystérieux mêlant cuir noble, tabac doux et gousse de vanille.", 
                    Prix = 110.00m, 
                    Stock = 8, 
                    ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f" 
                },
                new Parfum 
                { 
                    Nom = "Or Canopée", 
                    Description = "Un sillage solaire de jasmin impérial, de néroli et de patchouli doré.", 
                    Prix = 145.00m, 
                    Stock = 12, 
                    ImageUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3" 
                },
                new Parfum 
                { 
                    Nom = "Sable d'Oud", 
                    Description = "Un accord somptueux d'Oud fumé, de santal crémeux et d'épices chaudes.", 
                    Prix = 160.00m, 
                    Stock = 6, 
                    ImageUrl = "https://images.unsplash.com/photo-1588405748373-122b2321bc31" 
                },
                new Parfum 
                { 
                    Nom = "Rose Éternelle", 
                    Description = "La fraîcheur d'une rose de Damas cueillie à l'aube, relevée de baies roses.", 
                    Prix = 95.00m, 
                    Stock = 18, 
                    ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539" 
                },
                new Parfum 
                { 
                    Nom = "Brume Indigo", 
                    Description = "Une envolée marine salée reposant sur de la sauge officinale et du cèdre bleu.", 
                    Prix = 115.00m, 
                    Stock = 14, 
                    ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f" 
                }
            );
            context.SaveChanges();
        }

        // Assurer qu'il y a un large choix de parfums (ajout supplémentaire)
        var listNouveauParfums = new List<Parfum>
        {
            new Parfum { Nom = "Ambre Mystique", Description = "Un élixir suave d'ambre gris, de labdanum chaleureux et de vanille de Madagascar.", Prix = 135.00m, Stock = 15, ImageUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601" },
            new Parfum { Nom = "Musc Nomade", Description = "Une caresse de musc blanc pur, de graine d'ambrette et de bois d'iris délicat.", Prix = 90.00m, Stock = 20, ImageUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f" },
            new Parfum { Nom = "Jasmin Sacré", Description = "Une célébration du jasmin sambac, enveloppé d'encens mystique et de cire d'abeille.", Prix = 125.00m, Stock = 10, ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f" },
            new Parfum { Nom = "Fleur de Cerisier", Description = "La poésie printanière du sakura en fleurs, agrémentée de poire juteuse et de musc poudré.", Prix = 75.00m, Stock = 18, ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539" },
            new Parfum { Nom = "Vétiver Céleste", Description = "Une fraîcheur terreuse de vétiver de Haïti, illuminée par le pamplemousse rose et la menthe fraîche.", Prix = 110.00m, Stock = 12, ImageUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3" },
            new Parfum { Nom = "Cuir Impérial", Description = "Une puissance cuirée affirmée, adoucie par le safran noir et la prune confite.", Prix = 155.00m, Stock = 7, ImageUrl = "https://images.unsplash.com/photo-1588405748373-122b2321bc31" },
            new Parfum { Nom = "Élixir d'Orient", Description = "Une richesse opulente de cannelle, de patchouli sombre et de benjoin liquoreux.", Prix = 140.00m, Stock = 9, ImageUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f" },
            new Parfum { Nom = "Soleil Néroli", Description = "Un éclat ensoleillé d'essence de néroli de Tunisie, de petit-grain et de musc blanc.", Prix = 98.00m, Stock = 14, ImageUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601" },
            new Parfum { Nom = "Or Canopée", Description = "Un sillage solaire de jasmin impérial, de néroli et de patchouli doré.", Prix = 145.00m, Stock = 12, ImageUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3" },
            new Parfum { Nom = "Sable d'Oud", Description = "Un accord somptueux d'Oud fumé, de santal crémeux et d'épices chaudes.", Prix = 160.00m, Stock = 6, ImageUrl = "https://images.unsplash.com/photo-1588405748373-122b2321bc31" },
            new Parfum { Nom = "Rose Éternelle", Description = "La fraîcheur d'une rose de Damas cueillie à l'aube, relevée de baies roses.", Prix = 95.00m, Stock = 18, ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539" },
            new Parfum { Nom = "Brume Indigo", Description = "Une envolée marine salée reposant sur de la sauge officinale et du cèdre bleu.", Prix = 115.00m, Stock = 14, ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f" }
        };

        foreach (var p in listNouveauParfums)
        {
            if (!context.Parfums.Any(x => x.Nom == p.Nom))
            {
                context.Parfums.Add(p);
            }
        }
        context.SaveChanges();

        // Si la table des utilisateurs est vide, on ajoute des comptes de test par défaut
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User
                {
                    Nom = "Maison Admin",
                    Email = "admin@olfactive.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin",
                    Telephone = "+33699887766",
                    IsActive = true
                },
                new User
                {
                    Nom = "Jean Livreur",
                    Email = "livreur.jean@olfactive.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Livreur123!"),
                    Role = "Livreur",
                    Telephone = "+33612345678",
                    IsActive = true
                },
                new User
                {
                    Nom = "Aymen Staff",
                    Email = "staff.aymen@olfactive.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                    Role = "Personnel",
                    Telephone = "+212645582265",
                    IsActive = true
                }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue lors du peuplement de la base de données.");
    }
}
// 3. CONFIGURATION DU PIPELINE HTTP (Middlewares)
// ============================================================

// Activation de la doc OpenAPI en mode développement
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Activation du middleware CORS (doit être placé avant l'autorisation)
app.UseCors("AllowFrontend");

app.UseAuthorization();

// INDISPENSABLE : Mappage automatique des routes de tes contrôleurs (api/parfums)
app.MapControllers();

// Exemple météo par défaut
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}