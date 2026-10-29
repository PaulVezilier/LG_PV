using LG.Domain.Nuit;

namespace LG.Domain.Vues;

/// <summary>
/// Ce qu'un joueur donné a le droit de voir de la partie. C'est le seul objet envoyé au navigateur :
/// aucune information cachée (rôles des autres, messages privés...) ne doit fuiter ici.
/// </summary>
public sealed record VuePartie(
    string PartieId,
    Phase Phase,
    int NumeroTour,
    string? Etape,
    DateTimeOffset? FinPhase,
    Camp? Vainqueur,
    bool PeutDemarrer,
    VueMoi Moi,
    IReadOnlyList<VueJoueur> Joueurs,
    IReadOnlyList<ActionDisponible> Actions,
    IReadOnlyList<VueEvenement> Journal)
{
    private const int TailleJournal = 200;

    public static VuePartie Pour(Partie partie, string joueurId)
    {
        var moi = partie.TrouverJoueur(joueurId);
        var etape = partie.EtapeCourante;
        var finie = partie.Phase == Phase.Terminee;

        IReadOnlyDictionary<string, string> votes = partie.Phase switch
        {
            Phase.Jour => partie.VotesJour,
            Phase.Nuit when moi.EstLoup && etape is EtapeLoupsGarous => partie.EtatNuit.VotesLoups,
            _ => new Dictionary<string, string>(),
        };
        var voix = votes.Values.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        var joueurs = partie.Joueurs.Select(j => new VueJoueur(
            j.Id,
            j.Nom,
            j.EstVivant,
            EstHote: j.Id == partie.HoteId,
            Role: j.Role is not null && (j == moi || !j.EstVivant || finie) ? Partie.NomRole(j) : null,
            EstLoupAllie: j != moi && moi.EstLoup && j.EstLoup,
            Voix: voix.GetValueOrDefault(j.Id),
            AVote: votes.ContainsKey(j.Id))).ToList();

        var vueMoi = new VueMoi(
            moi.Id,
            moi.Nom,
            moi.EstVivant,
            EstHote: moi.Id == partie.HoteId,
            Role: moi.Role is null ? null : Partie.NomRole(moi),
            DescriptionRole: moi.Role?.Description,
            Camp: moi.Role is null ? null : moi.Camp);

        var journal = partie.Journal
            .Where(e => e.EstVisiblePar(moi, partie.Phase))
            .TakeLast(TailleJournal)
            .Select(e => new VueEvenement(e.Numero, e.Date, e.Canal, e.Texte, e.Auteur))
            .ToList();

        var jeJoueCetteEtape = etape is not null && moi.EstVivant && etape.Acteurs(partie).Contains(moi);

        return new VuePartie(
            partie.Id,
            partie.Phase,
            partie.NumeroTour,
            Etape: jeJoueCetteEtape ? etape!.Nom : null,
            partie.FinPhase,
            partie.Vainqueur,
            PeutDemarrer: partie.Phase == Phase.Lobby && vueMoi.EstHote && partie.Joueurs.Count >= partie.Parametres.JoueursMinimum,
            vueMoi,
            joueurs,
            partie.ActionsDisponibles(moi.Id),
            journal);
    }
}

public sealed record VueMoi(string Id, string Nom, bool EstVivant, bool EstHote, string? Role, string? DescriptionRole, Camp? Camp);

public sealed record VueJoueur(string Id, string Nom, bool EstVivant, bool EstHote, string? Role, bool EstLoupAllie, int Voix, bool AVote);

public sealed record VueEvenement(long Numero, DateTimeOffset Date, Canal Canal, string Texte, string? Auteur);
