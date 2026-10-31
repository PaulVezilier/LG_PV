using LG.Domain.Nuit;
using LG.Domain.Roles;

namespace LG.Domain.Vues;

/// <summary>
/// Ce qu'un joueur donné a le droit de voir de la partie. C'est le seul objet envoyé au navigateur :
/// aucune information cachée (rôles des autres, messages privés...) ne doit fuiter ici.
/// </summary>
public sealed record VuePartie(
    string PartieId,
    Phase Phase,
    int NumeroTour,
    string Narration,
    string? Consigne,
    string? MonVote,
    DateTimeOffset? FinPhase,
    Camp? Vainqueur,
    bool PeutDemarrer,
    string? ObstacleAuDemarrage,
    bool CompositionModifiable,
    bool CompositionAutomatique,
    IReadOnlyList<VueRole> Composition,
    Canal? CanalChat,
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

        // Les amoureux se connaissent, et Cupidon sait qui il a uni.
        bool VoitLeCouple(Joueur j) => j.Amoureux is not null && (finie || j == moi || j.Amoureux == moi || moi.Pouvoir is Cupidon);

        var joueurs = partie.Joueurs.Select(j => new VueJoueur(
            j.Id,
            j.Nom,
            j.EstVivant,
            EstHote: j.Id == partie.HoteId,
            Role: j.Role is null ? null
                : j == moi && !finie ? Partie.NomRolePourSoi(j)
                : !j.EstVivant || finie ? Partie.NomRole(j)
                : null,
            EstLoupAllie: j != moi && moi.EstLoup && j.EstLoup,
            EstAmoureux: VoitLeCouple(j),
            Voix: voix.GetValueOrDefault(j.Id),
            AVote: votes.ContainsKey(j.Id))).ToList();

        var vueMoi = new VueMoi(
            moi.Id,
            moi.Nom,
            moi.EstVivant,
            EstHote: moi.Id == partie.HoteId,
            Role: moi.Role is null ? null : Partie.NomRolePourSoi(moi),
            DescriptionRole: moi.Role?.DescriptionPourSoi,
            Camp: moi.Role is null ? null : moi.Camp);

        var journal = partie.Journal
            .Where(e => e.EstVisiblePar(moi, partie.Phase))
            .TakeLast(TailleJournal)
            .Select(e => new VueEvenement(e.Numero, e.Date, e.Canal, e.Texte, e.Auteur))
            .ToList();

        var narration = partie.Phase switch
        {
            Phase.Lobby => "En attente des joueurs…",
            Phase.Nuit => etape?.Narration ?? "Le village dort…",
            Phase.Jour => "Le village débat et vote pour éliminer un suspect",
            Phase.DernieresVolontes => $"Dernières volontés de {string.Join(" et ", partie.JoueursAvecVolonte)}",
            _ => partie.Vainqueur switch
            {
                Camp.Village => "Victoire du village !",
                Camp.LoupsGarous => "Victoire des loups-garous !",
                Camp.Amoureux => "Victoire du couple !",
                _ => "Égalité : plus personne n'est en vie",
            },
        };

        var actions = partie.ActionsDisponibles(moi.Id);
        var consigne = partie.Phase switch
        {
            Phase.Nuit when etape is not null && actions.Count > 0 => etape.Consigne,
            Phase.Jour when actions.Count > 0 => "Cliquez sur le joueur contre qui vous votez",
            Phase.DernieresVolontes when partie.JoueursAvecVolonte.Contains(moi) => moi.Pouvoir!.ConsigneDerniereVolonte,
            _ => null,
        };

        var compositionModifiable = partie.Phase == Phase.Lobby && vueMoi.EstHote;
        var nombres = partie.CompositionPrevue.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        // Calculé sur la carte reçue au lancement : un Héritier qui change de rôle ne fausse pas le décompte.
        var vivants = partie.Vivants.Where(j => j.RoleInitial is not null)
            .GroupBy(j => j.RoleInitial!.Value).ToDictionary(g => g.Key, g => g.Count());
        var composition = FabriqueRoles.Catalogue
            .Where(r => compositionModifiable || nombres.ContainsKey(r.Id))
            .Select(r => new VueRole(r.Id, r.Nom, r.Camp, r.Description, nombres.GetValueOrDefault(r.Id),
                partie.Phase == Phase.Lobby ? nombres.GetValueOrDefault(r.Id) : vivants.GetValueOrDefault(r.Id)))
            .ToList();
        var obstacle = partie.Phase == Phase.Lobby ? partie.ObstacleAuDemarrage() : null;

        return new VuePartie(
            partie.Id,
            partie.Phase,
            partie.NumeroTour,
            narration,
            consigne,
            votes.GetValueOrDefault(moi.Id),
            partie.FinPhase,
            partie.Vainqueur,
            PeutDemarrer: partie.Phase == Phase.Lobby && vueMoi.EstHote && obstacle is null,
            obstacle,
            compositionModifiable,
            CompositionAutomatique: partie.CompositionChoisie is null,
            composition,
            partie.CanalDeDiscussion(moi.Id),
            vueMoi,
            joueurs,
            actions,
            journal);
    }
}

public sealed record VueMoi(string Id, string Nom, bool EstVivant, bool EstHote, string? Role, string? DescriptionRole, Camp? Camp);

public sealed record VueJoueur(string Id, string Nom, bool EstVivant, bool EstHote, string? Role, bool EstLoupAllie, bool EstAmoureux, int Voix, bool AVote);

public sealed record VueEvenement(long Numero, DateTimeOffset Date, Canal Canal, string Texte, string? Auteur);

/// <param name="Nombre">Exemplaires dans la composition.</param>
/// <param name="Vivants">Exemplaires encore détenus par un joueur vivant.</param>
public sealed record VueRole(RoleId Id, string Nom, Camp Camp, string Description, int Nombre, int Vivants);
