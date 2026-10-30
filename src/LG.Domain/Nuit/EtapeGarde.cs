using LG.Domain.Roles;

namespace LG.Domain.Nuit;

internal sealed class EtapeGarde : EtapeNuit
{
    public override string Narration => "Le Garde doit protéger quelqu'un";

    public override string Consigne => "Cliquez sur le joueur à protéger cette nuit";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.Vivants.Where(j => j.Role is Garde);

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
    {
        if (joueur.Role is not Garde garde || ATermine(partie, joueur))
            return [];
        return [ActionDisponible.AvecCible(ActionType.Proteger, "Protéger", partie.Vivants.Where(garde.PeutProteger))];
    }

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        partie.EtatNuit.Protege = cible;
        ((Garde)joueur.Role!).EnregistrerProtection(cible);
        partie.JournaliserPrive(joueur, $"Vous protégez {cible!.Nom} cette nuit.");
        MarquerTermine(partie, joueur);
    }

    public override void Terminer(Partie partie)
    {
        // Un garde qui n'a protégé personne pourra protéger n'importe qui la nuit suivante.
        foreach (var joueur in Acteurs(partie).Where(j => !ATermine(partie, j)))
            ((Garde)joueur.Role!).EnregistrerProtection(null);
    }
}
