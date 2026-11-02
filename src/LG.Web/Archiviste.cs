using System.Runtime.CompilerServices;
using LG.Data;
using LG.Domain;
using Microsoft.EntityFrameworkCore;

namespace LG.Web;

/// <summary>
/// Recopie les parties dans la base au fil du jeu : la partie elle-même, ses participants au lancement
/// (complétés en fin de partie) et chaque nouvelle ligne du journal.
/// La partie vit en mémoire : une panne de la base est journalisée mais n'interrompt jamais le jeu.
/// </summary>
public sealed class Archiviste(IDbContextFactory<LgDbContext> fabrique, GestionnaireParties gestionnaire, TimeProvider horloge, ILogger<Archiviste> logger)
{
    /// <summary>Ce qui est déjà en base pour une partie. Rattaché à l'objet Partie : un code peut resservir plus tard.</summary>
    private sealed class Suivi
    {
        public readonly SemaphoreSlim Verrou = new(1, 1);
        public Guid Id = Guid.CreateVersion7();
        public bool EstEnBase;
        public long DernierNumero;
        public StatutPartie Statut;
        public Guid? HoteId;
        public Ambiance Ambiance;
    }

    private sealed record Photo(
        string Code, Guid? HoteId, Ambiance Ambiance, StatutPartie Statut, int NumeroTour, Camp? Vainqueur,
        List<EvenementArchive> Evenements, List<Participant> Participants);

    private readonly ConditionalWeakTable<Partie, Suivi> _suivis = [];

    public async Task EnregistrerAsync(Partie partie)
    {
        var suivi = _suivis.GetValue(partie, _ => new Suivi());
        await suivi.Verrou.WaitAsync();
        try
        {
            Photo photo;
            lock (partie)
                photo = Photographier(partie, suivi);

            var inchange = suivi.EstEnBase && photo.Evenements.Count == 0 && photo.Statut == suivi.Statut
                && photo.HoteId == suivi.HoteId && photo.Ambiance == suivi.Ambiance;
            if (inchange)
                return;

            await EcrireAsync(suivi, photo);

            suivi.EstEnBase = true;
            suivi.Statut = photo.Statut;
            suivi.HoteId = photo.HoteId;
            suivi.Ambiance = photo.Ambiance;
            if (photo.Evenements.Count > 0)
                suivi.DernierNumero = photo.Evenements[^1].Numero;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Impossible d'enregistrer la partie {PartieId} en base", partie.Id);
        }
        finally
        {
            suivi.Verrou.Release();
        }
    }

    /// <summary>Copie, sous le verrou de la partie, tout ce qui doit être écrit.</summary>
    private Photo Photographier(Partie partie, Suivi suivi)
    {
        var statut = partie.Phase switch
        {
            Phase.Lobby or Phase.Lancement when partie.Joueurs.Count == 0 => StatutPartie.Abandonnee,
            Phase.Lobby or Phase.Lancement => StatutPartie.Lobby,
            Phase.Terminee => StatutPartie.Terminee,
            _ => StatutPartie.EnCours,
        };

        var evenements = partie.Journal.Skip((int)suivi.DernierNumero).Select(e => new EvenementArchive
        {
            PartieId = suivi.Id,
            Numero = e.Numero,
            Date = e.Date,
            Tour = e.Tour,
            Phase = e.Phase,
            Canal = e.Canal,
            Texte = e.Texte,
            AuteurId = e.Auteur is null ? null : gestionnaire.CompteDe(partie.Id, e.Auteur.Id),
            Destinataires = e.Destinataires.Select(id => gestionnaire.CompteDe(partie.Id, id)).OfType<Guid>().ToList(),
        }).ToList();

        // Les participants sont écrits au lancement, puis complétés à la fin de la partie.
        var participants = new List<Participant>();
        if (statut is StatutPartie.EnCours or StatutPartie.Terminee && statut != suivi.Statut)
        {
            for (var siege = 0; siege < partie.Joueurs.Count; siege++)
            {
                var joueur = partie.Joueurs[siege];
                if (gestionnaire.CompteDe(partie.Id, joueur.Id) is not { } compteId)
                    continue;
                var termine = statut == StatutPartie.Terminee;
                participants.Add(new Participant
                {
                    PartieId = suivi.Id,
                    CompteId = compteId,
                    Pseudo = joueur.Nom,
                    Siege = siege,
                    RoleId = joueur.RoleInitial!.Value,
                    RoleRevele = termine ? joueur.Role!.NomRevele : null,
                    CampFinal = termine ? Partie.CampDeVictoire(joueur) : null,
                    Survivant = termine ? joueur.EstVivant : null,
                    Vainqueur = termine ? partie.Vainqueur is { } camp && Partie.CampDeVictoire(joueur) == camp : null,
                });
            }
        }

        var hoteId = partie.HoteId is null ? null : gestionnaire.CompteDe(partie.Id, partie.HoteId);
        return new Photo(partie.Id, hoteId, partie.Ambiance, statut, partie.NumeroTour, partie.Vainqueur, evenements, participants);
    }

    private async Task EcrireAsync(Suivi suivi, Photo photo)
    {
        await using var db = await fabrique.CreateDbContextAsync();
        var maintenant = horloge.GetUtcNow();

        var ligne = suivi.EstEnBase
            ? await db.Parties.Include(p => p.Participants).SingleAsync(p => p.Id == suivi.Id)
            : db.Parties.Add(new PartieArchivee { Id = suivi.Id, Code = photo.Code, CreeeLe = maintenant }).Entity;

        ligne.HoteId = photo.HoteId;
        ligne.AmbianceId = photo.Ambiance;
        ligne.NombreTours = photo.NumeroTour;
        ligne.CampVainqueur = photo.Vainqueur;
        if (photo.Statut != ligne.Statut)
        {
            if (photo.Statut == StatutPartie.EnCours)
                ligne.CommenceeLe = maintenant;
            if (photo.Statut is StatutPartie.Terminee or StatutPartie.Abandonnee)
                ligne.TermineeLe = maintenant;
            ligne.Statut = photo.Statut;
        }

        if (photo.Participants.Count > 0)
        {
            var skins = await db.Skins.ToDictionaryAsync(s => s.Code, s => s.Id);
            foreach (var participant in photo.Participants)
            {
                participant.SkinId = skins.TryGetValue(Catalogue.CodePersonnage(participant.Siege), out var skinId) ? skinId : null;
                var existant = ligne.Participants.SingleOrDefault(p => p.CompteId == participant.CompteId);
                if (existant is null)
                    ligne.Participants.Add(participant);
                else
                {
                    existant.RoleRevele = participant.RoleRevele;
                    existant.CampFinal = participant.CampFinal;
                    existant.Survivant = participant.Survivant;
                    existant.Vainqueur = participant.Vainqueur;
                }
            }
        }

        db.Evenements.AddRange(photo.Evenements);
        await db.SaveChangesAsync();
    }
}
