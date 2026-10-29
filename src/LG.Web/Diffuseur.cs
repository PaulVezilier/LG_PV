using System.Collections.Concurrent;
using LG.Domain;
using LG.Domain.Vues;
using Microsoft.AspNetCore.SignalR;

namespace LG.Web;

/// <summary>Envoie à chaque joueur sa propre vue de la partie (chaque joueur a son groupe SignalR).</summary>
public sealed class Diffuseur(IHubContext<PartieHub> hub)
{
    // Un verrou asynchrone par partie : garantit que les vues arrivent dans l'ordre.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _verrous = new();

    public static string Groupe(string partieId, string joueurId) => $"{partieId}:{joueurId}";

    public async Task DiffuserAsync(Partie partie)
    {
        var verrou = _verrous.GetOrAdd(partie.Id, _ => new SemaphoreSlim(1, 1));
        await verrou.WaitAsync();
        try
        {
            List<(string Groupe, VuePartie Vue)> vues;
            lock (partie)
                vues = partie.Joueurs.Select(j => (Groupe(partie.Id, j.Id), VuePartie.Pour(partie, j.Id))).ToList();

            await Task.WhenAll(vues.Select(v => hub.Clients.Group(v.Groupe).SendAsync("etat", v.Vue)));
        }
        finally
        {
            verrou.Release();
        }
    }
}
