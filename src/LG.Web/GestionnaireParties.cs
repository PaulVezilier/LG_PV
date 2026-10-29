using System.Collections.Concurrent;
using System.Security.Cryptography;
using LG.Domain;

namespace LG.Web;

/// <summary>Jeton secret détenu par le navigateur, qui l'identifie comme un joueur d'une partie.</summary>
public sealed record Session(string Jeton, string PartieId, string JoueurId);

/// <summary>Registre en mémoire des parties en cours et des sessions des joueurs.</summary>
public sealed class GestionnaireParties(TimeProvider horloge)
{
    private const string AlphabetCode = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly ConcurrentDictionary<string, Partie> _parties = new();
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public IEnumerable<Partie> Parties => _parties.Values;

    public Partie? Trouver(string partieId) => _parties.GetValueOrDefault(partieId);

    public Session? TrouverSession(string jeton) => _sessions.GetValueOrDefault(jeton);

    public Session CreerPartie(string nomHote)
    {
        while (true)
        {
            var partie = new Partie(RandomNumberGenerator.GetString(AlphabetCode, 5), horloge: horloge);
            var hote = partie.AjouterJoueur(nomHote);
            if (_parties.TryAdd(partie.Id, partie))
                return OuvrirSession(partie, hote);
        }
    }

    public Session RejoindrePartie(string code, string nom)
    {
        var partie = Trouver(code?.Trim().ToUpperInvariant() ?? "")
            ?? throw new ActionInvalideException("Aucune partie ne correspond à ce code.");
        Joueur joueur;
        lock (partie)
            joueur = partie.AjouterJoueur(nom);
        return OuvrirSession(partie, joueur);
    }

    private Session OuvrirSession(Partie partie, Joueur joueur)
    {
        var session = new Session(Guid.NewGuid().ToString("N"), partie.Id, joueur.Id);
        _sessions[session.Jeton] = session;
        return session;
    }
}
