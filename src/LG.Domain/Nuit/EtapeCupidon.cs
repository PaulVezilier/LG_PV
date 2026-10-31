using LG.Domain.Roles;

namespace LG.Domain.Nuit;

/// <summary>Première nuit seulement : Cupidon désigne les amoureux en deux clics.</summary>
internal sealed class EtapeCupidon : EtapeNuit
{
    public override string Narration => "Cupidon désigne deux amoureux";

    public override string Consigne => "Cliquez sur les deux joueurs à unir";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.NumeroTour == 1
        ? partie.VivantsAvec<Cupidon>()
        : Enumerable.Empty<Joueur>();

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
    {
        if (ATermine(partie, joueur))
            return [];
        var premier = partie.EtatNuit.PremierAmoureux;
        var libelle = premier is null ? "1er amoureux" : "2e amoureux";
        return [ActionDisponible.AvecCible(ActionType.Unir, libelle, partie.Vivants.Where(j => j != premier))];
    }

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        if (partie.EtatNuit.PremierAmoureux is not { } premier)
        {
            partie.EtatNuit.PremierAmoureux = cible;
            partie.JournaliserPrive(joueur, $"Premier amoureux : {cible!.Nom}. Choisissez maintenant le second.");
            return;
        }

        var second = cible!;
        premier.Amoureux = second;
        second.Amoureux = premier;
        partie.JournaliserPrive(joueur, $"💘 {premier.Nom} et {second.Nom} sont désormais amoureux.");

        var campsOpposes = premier.Camp != second.Camp;
        foreach (var (amoureux, autre) in new[] { (premier, second), (second, premier) })
        {
            partie.JournaliserPrive(amoureux,
                $"💘 Cupidon vous a uni à {autre.Nom}, qui est {autre.Role!.NomPourSoi}. Si l'un de vous meurt, l'autre meurt aussi."
                + (campsOpposes ? " Vos camps sont opposés : vous gagnerez ensemble si vous êtes les deux derniers survivants." : ""));
        }
        MarquerTermine(partie, joueur);
    }
}
