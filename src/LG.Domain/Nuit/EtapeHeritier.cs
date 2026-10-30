using LG.Domain.Roles;

namespace LG.Domain.Nuit;

/// <summary>Première nuit seulement : l'Héritier choisit de qui il héritera.</summary>
internal sealed class EtapeHeritier : EtapeNuit
{
    public override string Narration => "L'Héritier choisit de qui il héritera";

    public override string Consigne => "Cliquez sur le joueur dont vous hériterez";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.NumeroTour == 1
        ? partie.Vivants.Where(j => j.Role is Heritier { CibleId: null })
        : Enumerable.Empty<Joueur>();

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
        => [ActionDisponible.AvecCible(ActionType.Heriter, "Hériter de", partie.Vivants.Where(j => j != joueur))];

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        ((Heritier)joueur.Role!).Choisir(cible!);
        partie.JournaliserPrive(joueur, $"📜 À la mort de {cible!.Nom}, vous hériterez de son rôle.");
        MarquerTermine(partie, joueur);
    }
}
