using System.Collections.Concurrent;
using System.Security.Cryptography;
using LG.Domain;

namespace LG.Web;

/// <summary>Place d'un compte dans une partie.</summary>
public sealed record Session(string PartieId, string JoueurId);

/// <summary>Ce que le salon public affiche d'une partie.</summary>
public sealed record ResumePartie(string Id, string Hote, int NombreJoueurs, int JoueursMaximum, Phase Phase, int NumeroTour);

/// <summary>Registre en mémoire des parties en cours, et du compte de chaque joueur.</summary>
public sealed class GestionnaireParties(TimeProvider horloge)
{
    private const string AlphabetCode = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly ConcurrentDictionary<string, Partie> _parties = new();
    private readonly ConcurrentDictionary<(string PartieId, Guid CompteId), Session> _sessions = new();
    private readonly ConcurrentDictionary<(string PartieId, string JoueurId), Guid> _comptes = new();

    public IEnumerable<Partie> Parties => _parties.Values;

    public Partie? Trouver(string partieId) => _parties.GetValueOrDefault(partieId);

    /// <summary>Compte qui joue ce joueur (null s'il a quitté le lobby).</summary>
    public Guid? CompteDe(string partieId, string joueurId)
        => _comptes.TryGetValue((partieId, joueurId), out var compteId) ? compteId : null;

    /// <summary>Partie non terminée où le compte a une place, pour lui proposer d'y retourner.</summary>
    public Session? PartieEnCours(Guid compteId)
        => _sessions.Where(s => s.Key.CompteId == compteId && Trouver(s.Key.PartieId) is { Phase: not Phase.Terminee })
            .Select(s => s.Value)
            .FirstOrDefault();

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

    public Session CreerPartie(Guid compteId, string pseudo)
    {
        while (true)
        {
            var partie = new Partie(RandomNumberGenerator.GetString(AlphabetCode, 5), horloge: horloge);
            var hote = partie.AjouterJoueur(pseudo);
            if (_parties.TryAdd(partie.Id, partie))
                return Enregistrer(partie, hote, compteId);
        }
    }

    /// <summary>Rejoint la partie, ou y reprend sa place si le compte y est déjà (rechargement, autre onglet).</summary>
    public Session RejoindrePartie(string code, Guid compteId, string pseudo)
    {
        var partie = Trouver(code?.Trim().ToUpperInvariant() ?? "")
            ?? throw new ActionInvalideException("Aucune partie ne correspond à ce code.");
        lock (partie)
        {
            if (_sessions.TryGetValue((partie.Id, compteId), out var session))
                return session;
            return Enregistrer(partie, partie.AjouterJoueur(pseudo), compteId);
        }
    }

    /// <summary>Retire le joueur d'une partie pas encore lancée ; une partie vide est supprimée.</summary>
    public void QuitterPartie(Session session, Guid compteId)
    {
        var partie = Trouver(session.PartieId) ?? throw new ActionInvalideException("Partie introuvable.");
        lock (partie)
        {
            partie.RetirerJoueur(session.JoueurId);
            if (partie.Joueurs.Count == 0)
                _parties.TryRemove(partie.Id, out _);
        }
        _sessions.TryRemove((session.PartieId, compteId), out _);
        _comptes.TryRemove((session.PartieId, session.JoueurId), out _);
    }

    private Session Enregistrer(Partie partie, Joueur joueur, Guid compteId)
    {
        var session = new Session(partie.Id, joueur.Id);
        _sessions[(partie.Id, compteId)] = session;
        _comptes[(partie.Id, joueur.Id)] = compteId;
        return session;
    }
}
