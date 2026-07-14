using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;
using OlfactiveParfum.Backend.Models; // Ajouté pour pouvoir manipuler la classe Parfum

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
        context.Database.Migrate();

        // Si la table Parfums est vide, on ajoute des données par défaut
        if (!context.Parfums.Any())
        {
            context.Parfums.AddRange(
                new Parfum 
                { 
                    Nom = "Impression Élégante", 
                    Description = "Un parfum boisé et ambré d'une élégance rare.", 
                    Prix = 85.00m, 
                    Stock = 15, 
                    ImageUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601" 
                },
                new Parfum 
                { 
                    Nom = "Éclat d'Agrumes", 
                    Description = "Une fraîcheur intense mêlant bergamote et mandarine.", 
                    Prix = 65.50m, 
                    Stock = 22, 
                    ImageUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f" 
                },
                new Parfum 
                { 
                    Nom = "Nuit Cuivrée", 
                    Description = "Un sillage mystérieux de cuir, de tabac doux et de vanille.", 
                    Prix = 110.00m, 
                    Stock = 8, 
                    ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f" 
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

// ============================================================
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