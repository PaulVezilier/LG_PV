using LG.Data;
using Microsoft.EntityFrameworkCore;

namespace LG.Web;

public static class Base
{
    /// <summary>
    /// Au démarrage : applique les migrations, recopie le catalogue (rôles, ambiances, personnages),
    /// et clôt les parties restées ouvertes, puisque les parties en cours vivent en mémoire et ont disparu avec l'arrêt du serveur.
    /// </summary>
    public static async Task PreparerAsync(IServiceProvider services)
    {
        var fabrique = services.GetRequiredService<IDbContextFactory<LgDbContext>>();
        await using var db = await fabrique.CreateDbContextAsync();

        await db.Database.MigrateAsync();
        await Catalogue.SynchroniserAsync(db);
        await db.Parties
            .Where(p => p.Statut == StatutPartie.Lobby || p.Statut == StatutPartie.EnCours)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.Statut, StatutPartie.Abandonnee));
    }
}
