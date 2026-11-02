using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using LG.Data;
using LG.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Base de données
// En développement, la chaîne de connexion (avec son mot de passe) est dans les user-secrets ; en production, dans
// la variable d'environnement ConnectionStrings__LG. Jamais dans un fichier versionné.
var chaineConnexion = builder.Configuration.GetConnectionString("LG")
    ?? throw new InvalidOperationException("Chaîne de connexion « ConnectionStrings:LG » manquante : voir le README, section Base de données.");
builder.Services.AddDbContextFactory<LgDbContext>(options => options.UseLg(chaineConnexion));

// ---------------------------------------------------------------- Comptes
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.AddAuthorization();
builder.Services
    .AddIdentityCore<Compte>(options =>
    {
        options.User.RequireUniqueEmail = true;
        // Les caractères du pseudo sont vérifiés à l'inscription (accents permis).
        options.User.AllowedUserNameCharacters = "";
        // Longueur plutôt que complexité : 10 caractères dont un chiffre.
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<LgDbContext>()
    .AddSignInManager()
    .AddErrorDescriber<ErreursIdentityFr>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "lg.session";
    options.Cookie.HttpOnly = true;
    // Strict : le cookie n'est jamais envoyé depuis un autre site (protège l'API et le hub contre le CSRF).
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    // Une API répond 401/403 au lieu de rediriger vers une page de connexion.
    options.Events.OnRedirectToLogin = contexte => { contexte.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = contexte => { contexte.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});

// Inscription et connexion : 10 essais par minute et par adresse IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(Comptes.PolitiqueLimite, contexte => RateLimitPartition.GetFixedWindowLimiter(
        contexte.Connection.RemoteIpAddress?.ToString() ?? "",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// ---------------------------------------------------------------- Jeu
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GestionnaireParties>();
builder.Services.AddSingleton<Archiviste>();
builder.Services.AddSingleton<Diffuseur>();
builder.Services.AddHostedService<HorlogeParties>();

var app = builder.Build();

await Base.PreparerAsync(app.Services);

app.UseEntetesDeSecurite();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapComptes();
app.MapHub<PartieHub>("/hub/partie");
// Le front (React) gère ses propres routes : /salon, /partie/ABCDE...
app.MapFallbackToFile("index.html");

app.Run();
