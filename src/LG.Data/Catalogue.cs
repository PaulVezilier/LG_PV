using LG.Domain;
using LG.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace LG.Data;

/// <summary>
/// Recopie dans la base ce que le code définit : rôles, ambiances et personnages.
/// Appelé au démarrage : ajoute ce qui manque et met à jour noms et descriptions,
/// sans toucher aux colonnes gérées en base (Actif, Gratuit).
/// </summary>
public static class Catalogue
{
    private static readonly (string Code, string Nom)[] Personnages =
    [
        ("aldric", "Aldric"), ("bertille", "Bertille"), ("come", "Côme"), ("douce", "Douce"),
        ("elouan", "Elouan"), ("faustine", "Faustine"), ("gaspard", "Gaspard"), ("hermine", "Hermine"),
        ("isaure", "Isaure"), ("jehan", "Jehan"), ("leonce", "Léonce"), ("mahaut", "Mahaut"),
        ("norbert", "Norbert"), ("oriane", "Oriane"), ("perceval", "Perceval"), ("quitterie", "Quitterie"),
    ];

    /// <summary>Personnage du n-ième joueur d'une partie (ordre d'arrivée), comme le fait le front.</summary>
    public static string CodePersonnage(int siege) => Personnages[siege % Personnages.Length].Code;

    public static async Task SynchroniserAsync(LgDbContext db, CancellationToken annulation = default)
    {
        var roles = await db.Roles.ToDictionaryAsync(r => r.Id, annulation);
        foreach (var role in FabriqueRoles.Catalogue)
        {
            if (!roles.TryGetValue(role.Id, out var ligne))
                db.Roles.Add(ligne = new RoleJeu { Id = role.Id, Nom = role.Nom, Description = role.Description });
            ligne.Nom = role.Nom;
            ligne.Camp = role.Camp;
            ligne.Description = role.Description;
        }

        var ambiances = await db.Ambiances.ToDictionaryAsync(a => a.Id, annulation);
        foreach (var ambiance in Enum.GetValues<Ambiance>())
            if (!ambiances.ContainsKey(ambiance))
                db.Ambiances.Add(new AmbianceJeu { Id = ambiance, Nom = ambiance.ToString() });

        var skins = await db.Skins.ToDictionaryAsync(s => s.Code, annulation);
        foreach (var (code, nom) in Personnages)
        {
            if (!skins.TryGetValue(code, out var skin))
                db.Skins.Add(skin = new Skin { Code = code, Nom = nom, Fichier = "" });
            skin.Nom = nom;
            skin.Fichier = $"/personnages/{code}.svg";
        }

        await db.SaveChangesAsync(annulation);
    }
}
