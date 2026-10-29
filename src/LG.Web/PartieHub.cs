using LG.Domain;
using Microsoft.AspNetCore.SignalR;

namespace LG.Web;

/// <summary>Point d'entrée temps réel du navigateur. Toute la logique de jeu est dans LG.Domain.</summary>
public sealed class PartieHub(GestionnaireParties gestionnaire, Diffuseur diffuseur) : Hub
{
    private const string CleSession = "session";

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

    public Task Demarrer() => ModifierPartie((partie, session) => partie.Demarrer(session.JoueurId));

    public Task Agir(ActionType action, string? cibleId) => ModifierPartie((partie, session) => partie.Agir(session.JoueurId, action, cibleId));

    public Task EnvoyerMessage(string texte) => ModifierPartie((partie, session) => partie.EnvoyerMessage(session.JoueurId, texte));

    private async Task Associer(Session session)
    {
        Context.Items[CleSession] = session;
        await Groups.AddToGroupAsync(Context.ConnectionId, Diffuseur.Groupe(session.PartieId, session.JoueurId));
        await diffuseur.DiffuserAsync(gestionnaire.Trouver(session.PartieId)!);
    }

    private async Task ModifierPartie(Action<Partie, Session> modification)
    {
        if (!Context.Items.TryGetValue(CleSession, out var valeur) || valeur is not Session session)
            throw new HubException("Vous n'êtes dans aucune partie.");
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
