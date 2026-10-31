using LG.Domain.Roles;

namespace LG.Domain.Nuit;

internal sealed class EtapeSorciere : EtapeNuit
{
    public override string Narration => "La Sorcière prépare ses potions";

    public override string Consigne => "Utilisez vos potions, ou cliquez sur un joueur à empoisonner";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.Vivants.Where(j => j.Pouvoir is Sorciere { ADesPotions: true });

    /// <summary>Une victime infectée ne mourra pas : la Sorcière ne la voit donc pas.</summary>
    private static Joueur? VictimeVisible(Partie partie) => partie.EtatNuit.VictimeInfectee ? null : partie.EtatNuit.VictimeLoups;

    public override void Debuter(Partie partie)
    {
        var victime = VictimeVisible(partie);
        foreach (var joueur in Acteurs(partie))
            partie.JournaliserPrive(joueur, victime is null
                ? "Les loups n'ont attaqué personne cette nuit."
                : $"Cette nuit, les loups ont attaqué {victime.Nom}.");
    }

    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
    {
        if (joueur.Pouvoir is not Sorciere sorciere || ATermine(partie, joueur))
            return [];

        var actions = new List<ActionDisponible>();
        if (sorciere.PotionVieDisponible && VictimeVisible(partie) is { } victime && !partie.EtatNuit.VictimeSoignee)
            actions.Add(ActionDisponible.SansCible(ActionType.Soigner, $"Sauver {victime.Nom}"));
        if (sorciere.PotionMortDisponible && partie.EtatNuit.Empoisonne is null)
            actions.Add(ActionDisponible.AvecCible(ActionType.Empoisonner, "Empoisonner", partie.Vivants.Where(j => j != joueur)));
        actions.Add(ActionDisponible.SansCible(ActionType.Passer, "Terminer mon tour"));
        return actions;
    }

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        var sorciere = (Sorciere)joueur.Pouvoir!;
        switch (action)
        {
            case ActionType.Soigner:
                sorciere.UtiliserPotionVie();
                partie.EtatNuit.VictimeSoignee = true;
                partie.JournaliserPrive(joueur, $"Vous utilisez votre potion de vie sur {VictimeVisible(partie)!.Nom}.");
                break;
            case ActionType.Empoisonner:
                sorciere.UtiliserPotionMort();
                partie.EtatNuit.Empoisonne = cible;
                partie.JournaliserPrive(joueur, $"Vous empoisonnez {cible!.Nom}.");
                break;
        }

        if (action == ActionType.Passer || ActionsDisponibles(partie, joueur).All(a => a.Type == ActionType.Passer))
            MarquerTermine(partie, joueur);
    }
}
