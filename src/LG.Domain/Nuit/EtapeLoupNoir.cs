using LG.Domain.Roles;

namespace LG.Domain.Nuit;

internal sealed class EtapeLoupNoir : EtapeNuit
{
    public override string Nom => "Loup-Garou Noir : voulez-vous infecter la victime ?";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.EtatNuit.VictimeLoups is null
        ? Enumerable.Empty<Joueur>()
        : partie.Vivants.Where(j => j.Role is LoupGarouNoir { InfectionDisponible: true });

    public override void Debuter(Partie partie)
    {
        foreach (var joueur in Acteurs(partie))
            partie.JournaliserPrive(joueur, $"Vous pouvez infecter {partie.EtatNuit.VictimeLoups!.Nom} au lieu de le dévorer.");
    }

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
    {
        if (ATermine(partie, joueur))
            return [];
        return
        [
            ActionDisponible.SansCible(ActionType.Infecter, $"Infecter {partie.EtatNuit.VictimeLoups!.Nom}"),
            ActionDisponible.SansCible(ActionType.Passer, "Ne pas infecter"),
        ];
    }

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        if (action == ActionType.Infecter)
        {
            ((LoupGarouNoir)joueur.Role!).UtiliserInfection();
            partie.EtatNuit.VictimeInfectee = true;
            partie.JournaliserPrive(joueur, $"Vous infectez {partie.EtatNuit.VictimeLoups!.Nom}.");
        }
        MarquerTermine(partie, joueur);
    }
}
