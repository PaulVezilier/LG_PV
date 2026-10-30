using LG.Domain;
using Microsoft.AspNetCore.SignalR;

namespace LG.Web;

/// <summary>Point d'entrée temps réel du navigateur. Toute la logique de jeu est dans LG.Domain.</summary>
public sealed class PartieHub(GestionnaireParties gestionnaire, Diffuseur diffuseur) : Hub
{
    private const string CleSession = "session";

    /// <summary>Abonne le navigateur aux mises à jour de la liste des parties et renvoie la liste actuelle.</summary>
    public async Task<IReadOnlyList<ResumePartie>> ObserverSalon()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Diffuseur.GroupeSalon);
        return gestionnaire.Resumes();
    }

    public Task QuitterSalon() => Groups.RemoveFromGroupAsync(Context.ConnectionId, Diffuseur.GroupeSalon);

    public async Task<Session> CreerPartie(string nom)
    {
        var session = Proteger(() => gestionnaire.CreerPartie(nom));
        await Associer(session);
        return session;
    }

    public async Task<Session> RejoindrePartie(string code, string nom)
    {
        var session = Proteger(() => gestionnaire.RejoindrePartie(code, nom));
        await Associer(session);
        return session;
    }

    /// <summary>Après un rafraîchissement de page ou une reconnexion.</summary>
    public async Task<Session?> Reprendre(string jeton)
    {
        var session = gestionnaire.TrouverSession(jeton);
        if (session is not null)
            await Associer(session);
        return session;
    }

    /// <summary>Dans le lobby, le joueur quitte vraiment la partie ; une fois lancée, il ne fait que s'en détacher.</summary>
    public async Task QuitterPartie()
    {
        if (SessionCourante() is not { } session)
            return;
        await Dissocier(session);

        var partie = gestionnaire.Trouver(session.PartieId);
        if (partie is null || partie.Phase != Phase.Lobby)
            return;
        Proteger(() => { gestionnaire.QuitterPartie(session); return true; });
        await diffuseur.DiffuserAsync(partie);
    }

    /// <param name="roles">Un rôle par joueur, ou null pour la composition automatique.</param>
    public Task DefinirComposition(List<RoleId>? roles) => ModifierPartie((partie, session) => partie.DefinirComposition(session.JoueurId, roles));

    public Task Demarrer() => ModifierPartie((partie, session) => partie.Demarrer(session.JoueurId));

    public Task Agir(ActionType action, string? cibleId) => ModifierPartie((partie, session) => partie.Agir(session.JoueurId, action, cibleId));

    public Task EnvoyerMessage(string texte) => ModifierPartie((partie, session) => partie.EnvoyerMessage(session.JoueurId, texte));

    private Session? SessionCourante()
        => Context.Items.TryGetValue(CleSession, out var valeur) ? valeur as Session : null;

    private async Task Associer(Session session)
    {
        // Une connexion ne suit qu'une partie : on la détache de la précédente.
        if (SessionCourante() is { } precedente && precedente != session)
            await Dissocier(precedente);

        Context.Items[CleSession] = session;
        await Groups.AddToGroupAsync(Context.ConnectionId, Diffuseur.Groupe(session.PartieId, session.JoueurId));
        await diffuseur.DiffuserAsync(gestionnaire.Trouver(session.PartieId)!);
    }

    private async Task Dissocier(Session session)
    {
        Context.Items.Remove(CleSession);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Diffuseur.Groupe(session.PartieId, session.JoueurId));
    }

    private async Task ModifierPartie(Action<Partie, Session> modification)
    {
        var session = SessionCourante() ?? throw new HubException("Vous n'êtes dans aucune partie.");
        var partie = gestionnaire.Trouver(session.PartieId) ?? throw new HubException("Partie introuvable.");

        lock (partie)
            Proteger(() => { modification(partie, session); return true; });
        await diffuseur.DiffuserAsync(partie);
    }

    /// <summary>Transforme les erreurs de jeu en HubException, dont le message est transmis au navigateur.</summary>
    private static T Proteger<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (ActionInvalideException e)
        {
            throw new HubException(e.Message);
        }
    }
}
