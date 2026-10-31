using LG.Domain.Roles;

namespace LG.Domain.Nuit;

internal sealed class EtapeVoyante : EtapeNuit
{
    public override string Narration => "La Voyante sonde un joueur";

    public override string Consigne => "Cliquez sur le joueur dont vous voulez voir le rôle";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.VivantsAvec<Voyante>();

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
    {
        if (ATermine(partie, joueur))
            return [];
        return [ActionDisponible.AvecCible(ActionType.Sonder, "Sonder", partie.Vivants.Where(j => j != joueur))];
    }

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        partie.JournaliserPrive(joueur, $"🔮 {cible!.Nom} est {cible.Role!.NomVuParVoyante}.");
        MarquerTermine(partie, joueur);
    }
}
