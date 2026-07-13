using Microsoft.EntityFrameworkCore;
using OlfactiveParfum.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. INJECTION DES SERVICES (Configuration du Container)
// ============================================================

// Configuration sécurisée qui utilise les fichiers appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// PRISE EN CHARGE DES CONTRÔLEURS (Indispensable pour ParfumsController)
builder.Services.AddControllers();

// Support OpenAPI/.NET 9 (Généré par défaut)
builder.Services.AddOpenApi();

var app = builder.Build();

// ============================================================
// 2. CONFIGURATION DU PIPELINE HTTP (Middlewares)
// ============================================================

// Activation de la doc OpenAPI en mode développement
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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