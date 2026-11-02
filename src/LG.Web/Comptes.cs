using System.Security.Claims;
using System.Text.RegularExpressions;
using LG.Data;
using Microsoft.AspNetCore.Identity;

namespace LG.Web;

public sealed record Inscription(string Pseudo, string Email, string MotDePasse);

/// <param name="Identifiant">Pseudo ou adresse e-mail.</param>
public sealed record Connexion(string Identifiant, string MotDePasse, bool SeSouvenir);

/// <summary>Le compte connecté, tel que le front le connaît.</summary>
public sealed record Moi(Guid Id, string Pseudo);

/// <summary>
/// API des comptes : inscription, connexion, déconnexion. La session est un cookie HttpOnly
/// posé par Identity, que le navigateur renvoie aussi au hub SignalR.
/// </summary>
public static partial class Comptes
{
    public const string PolitiqueLimite = "comptes";

    [GeneratedRegex(@"^[\p{L}\p{N}_.-]{3,20}$")]
    private static partial Regex PseudoValide();

    public static void MapComptes(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/compte");

        groupe.MapGet("/moi", async (ClaimsPrincipal utilisateur, UserManager<Compte> comptes) =>
            await comptes.GetUserAsync(utilisateur) is { } compte ? new Moi(compte.Id, compte.UserName!) : null);

        groupe.MapPost("/inscription", Inscrire).RequireRateLimiting(PolitiqueLimite);
        groupe.MapPost("/connexion", Connecter).RequireRateLimiting(PolitiqueLimite);

        groupe.MapPost("/deconnexion", async (SignInManager<Compte> connexions) =>
        {
            await connexions.SignOutAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult> Inscrire(Inscription demande, UserManager<Compte> comptes, SignInManager<Compte> connexions, TimeProvider horloge)
    {
        var pseudo = demande.Pseudo?.Trim() ?? "";
        if (!PseudoValide().IsMatch(pseudo))
            return Erreur("Le pseudo doit faire de 3 à 20 caractères : lettres, chiffres, « - », « _ » ou « . ».");

        var compte = new Compte { UserName = pseudo, Email = demande.Email?.Trim(), InscritLe = horloge.GetUtcNow() };
        var resultat = await comptes.CreateAsync(compte, demande.MotDePasse ?? "");
        if (!resultat.Succeeded)
            return Erreur(string.Join(" ", resultat.Errors.Select(e => e.Description)));

        await connexions.SignInAsync(compte, isPersistent: true);
        return Results.Ok(new Moi(compte.Id, compte.UserName));
    }

    private static async Task<IResult> Connecter(Connexion demande, UserManager<Compte> comptes, SignInManager<Compte> connexions)
    {
        var identifiant = demande.Identifiant?.Trim() ?? "";
        var compte = identifiant.Contains('@')
            ? await comptes.FindByEmailAsync(identifiant)
            : await comptes.FindByNameAsync(identifiant);
        // Même message que le compte existe ou non : on ne révèle pas quels pseudos sont inscrits.
        if (compte is null)
            return Erreur("Identifiant ou mot de passe incorrect.", StatusCodes.Status401Unauthorized);

        var resultat = await connexions.PasswordSignInAsync(compte, demande.MotDePasse ?? "", demande.SeSouvenir, lockoutOnFailure: true);
        if (resultat.IsLockedOut)
            return Erreur("Trop de tentatives : compte bloqué pendant quelques minutes.", StatusCodes.Status429TooManyRequests);
        if (!resultat.Succeeded)
            return Erreur("Identifiant ou mot de passe incorrect.", StatusCodes.Status401Unauthorized);

        return Results.Ok(new Moi(compte.Id, compte.UserName!));
    }

    private static IResult Erreur(string message, int statut = StatusCodes.Status400BadRequest)
        => Results.Problem(detail: message, statusCode: statut);
}

/// <summary>Messages d'erreur d'Identity en français (seuls ceux qui peuvent arriver jusqu'au joueur).</summary>
public sealed class ErreursIdentityFr : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "Ce pseudo est déjà pris." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = "Cette adresse e-mail est déjà utilisée." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = "Adresse e-mail invalide." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "Pseudo invalide." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Le mot de passe doit faire au moins {length} caractères." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Le mot de passe doit contenir au moins un chiffre." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents." };
}
