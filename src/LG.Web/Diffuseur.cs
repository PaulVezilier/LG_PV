using System.Collections.Concurrent;
using System.Text.Json;
using LG.Domain;
using LG.Domain.Vues;
using Microsoft.AspNetCore.SignalR;

namespace LG.Web;

/// <summary>
/// Envoie à chaque joueur sa propre vue de la partie (chaque joueur a son groupe SignalR),
/// et la liste des parties aux visiteurs du salon quand elle change.
/// </summary>
public sealed class Diffuseur(IHubContext<PartieHub> hub, GestionnaireParties gestionnaire)
{
    public const string GroupeSalon = "salon";

    // Un verrou asynchrone par partie : garantit que les vues arrivent dans l'ordre.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _verrous = new();
    private readonly SemaphoreSlim _verrouSalon = new(1, 1);
    private string _dernierSalon = "";

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
        await DiffuserSalonAsync();
    }

    /// <summary>N'envoie la liste au salon que si elle a changé depuis le dernier envoi (un message de chat ne la change pas).</summary>
    public async Task DiffuserSalonAsync()
    {
        await _verrouSalon.WaitAsync();
        try
        {
            var resumes = gestionnaire.Resumes();
            var empreinte = JsonSerializer.Serialize(resumes);
            if (empreinte == _dernierSalon)
                return;
            _dernierSalon = empreinte;
            await hub.Clients.Group(GroupeSalon).SendAsync("parties", resumes);
        }
        finally
        {
            _verrouSalon.Release();
        }
    }
}
