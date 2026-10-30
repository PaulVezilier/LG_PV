namespace LG.Domain.Nuit;

/// <summary>Tous les joueurs du camp des loups (infectés compris) votent pour une victime.</summary>
internal sealed class EtapeLoupsGarous : EtapeNuit
{
    public override string Narration => "Les loups-garous choisissent leur victime";

    public override string Consigne => "Cliquez sur la victime de la meute";

    public override IEnumerable<Joueur> Acteurs(Partie partie) => partie.Vivants.Where(j => j.EstLoup);

    // Pas de filtre ATermine : un loup peut changer son vote tant que la meute n'a pas fini.
    public override IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur)
        => [ActionDisponible.AvecCible(ActionType.Voter, "Dévorer", partie.Vivants.Where(j => !j.EstLoup))];

    public override void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible)
    {
        partie.EtatNuit.VotesLoups[joueur.Id] = cible!.Id;
        partie.JournaliserLoups($"{joueur.Nom} veut dévorer {cible.Nom}.");
        MarquerTermine(partie, joueur);
    }

    public override void Terminer(Partie partie)
    {
        var victime = partie.Depouiller(partie.EtatNuit.VotesLoups, departageAleatoire: true);
        partie.EtatNuit.VictimeLoups = victime;
        partie.JournaliserLoups(victime is null
            ? "La meute n'a choisi aucune victime cette nuit."
            : $"La meute a choisi de dévorer {victime.Nom}.");
    }
}
