using LG.Domain.Roles;

namespace LG.Domain.Nuit;

internal sealed class EtapeLoupNoir : EtapeNuit
{
    public override string Narration => "Le Loup-Garou Noir hésite à infecter la victime";

    public override string Consigne => "Voulez-vous infecter la victime ?";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.EtatNuit.VictimeLoups is null
        ? Enumerable.Empty<Joueur>()
        : partie.Vivants.Where(j => j.Pouvoir is LoupGarouNoir { InfectionDisponible: true });

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
            ((LoupGarouNoir)joueur.Pouvoir!).UtiliserInfection();
            partie.EtatNuit.VictimeInfectee = true;
            partie.JournaliserPrive(joueur, $"Vous infectez {partie.EtatNuit.VictimeLoups!.Nom}.");
        }
        MarquerTermine(partie, joueur);
    }
}
