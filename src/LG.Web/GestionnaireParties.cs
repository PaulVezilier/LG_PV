using System.Collections.Concurrent;
using System.Security.Cryptography;
using LG.Domain;

namespace LG.Web;

/// <summary>Jeton secret détenu par le navigateur, qui l'identifie comme un joueur d'une partie.</summary>
public sealed record Session(string Jeton, string PartieId, string JoueurId);

/// <summary>Ce que le salon public affiche d'une partie.</summary>
public sealed record ResumePartie(string Id, string Hote, int NombreJoueurs, int JoueursMaximum, Phase Phase, int NumeroTour);

/// <summary>Registre en mémoire des parties en cours et des sessions des joueurs.</summary>
public sealed class GestionnaireParties(TimeProvider horloge)
{
    private const string AlphabetCode = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly ConcurrentDictionary<string, Partie> _parties = new();
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public IEnumerable<Partie> Parties => _parties.Values;

    public Partie? Trouver(string partieId) => _parties.GetValueOrDefault(partieId);

    public Session? TrouverSession(string jeton) => _sessions.GetValueOrDefault(jeton);

    /// <summary>Parties visibles dans le salon : en attente d'abord, puis en cours. Les parties terminées n'y figurent pas.</summary>
    public IReadOnlyList<ResumePartie> Resumes()
    {
        var resumes = new List<ResumePartie>();
        foreach (var partie in _parties.Values)
        {
            lock (partie)
            {
                if (partie.Phase == Phase.Terminee || partie.Joueurs.Count == 0)
                    continue;
                var hote = partie.Joueurs.FirstOrDefault(j => j.Id == partie.HoteId)?.Nom ?? "";
                resumes.Add(new ResumePartie(partie.Id, hote, partie.Joueurs.Count, partie.Parametres.JoueursMaximum, partie.Phase, partie.NumeroTour));
            }
        }
        return resumes.OrderBy(r => r.Phase != Phase.Lobby).ThenBy(r => r.Id).ToList();
    }

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

    /// <summary>Retire le joueur d'une partie pas encore lancée ; une partie vide est supprimée.</summary>
    public void QuitterPartie(Session session)
    {
        var partie = Trouver(session.PartieId) ?? throw new ActionInvalideException("Partie introuvable.");
        lock (partie)
        {
            partie.RetirerJoueur(session.JoueurId);
            if (partie.Joueurs.Count == 0)
                _parties.TryRemove(partie.Id, out _);
        }
        _sessions.TryRemove(session.Jeton, out _);
    }

    private Session OuvrirSession(Partie partie, Joueur joueur)
    {
        var session = new Session(Guid.NewGuid().ToString("N"), partie.Id, joueur.Id);
        _sessions[session.Jeton] = session;
        return session;
    }
}
