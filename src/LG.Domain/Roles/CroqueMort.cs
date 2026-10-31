namespace LG.Domain.Roles;

public sealed class CroqueMort : Role
{
    public override RoleId Id => RoleId.CroqueMort;
    public override string Nom => "Croque-Mort";
    public override string Description => "La nuit, vous pouvez discuter avec les morts. À votre mort, vous désignez un joueur : le village apprend qu'entre lui et un autre joueur tiré au sort, il y a un loup-garou.";
    public override Camp Camp => Camp.Village;

    internal override bool EntendLesMortsLaNuit => true;

    internal override string AnnonceDerniereVolonte => "Avant de partir, désignez un joueur : le village saura qu'entre lui et un autre joueur, il y a un loup-garou.";
    internal override string ConsigneDerniereVolonte => "Dernière volonté : cliquez sur un joueur à examiner";

    internal override IReadOnlyList<ActionDisponible> ActionsDerniereVolonte(Partie partie, Joueur moi)
        => [ActionDisponible.AvecCible(ActionType.Designer, "Désigner", partie.Vivants), ActionDisponible.SansCible(ActionType.Passer, "Ne désigner personne")];

    /// <summary>Le désigné + un joueur tiré au sort dans le camp opposé : exactement un des deux est un loup.</summary>
    internal override void ExecuterDerniereVolonte(Partie partie, Joueur moi, Joueur designe)
    {
        var candidats = partie.Vivants.Where(j => j != designe && j.EstLoup != designe.EstLoup).ToList();
        if (candidats.Count == 0)
        {
            partie.JournaliserPublic($"⚰️ Le Croque-Mort {moi.Nom} n'a rien pu découvrir.");
            return;
        }

        var autre = candidats[partie.Aleatoire.Next(candidats.Count)];
        var (premier, second) = partie.Aleatoire.Next(2) == 0 ? (designe, autre) : (autre, designe);
        partie.JournaliserPublic($"⚰️ Le Croque-Mort {moi.Nom} a parlé : entre {premier.Nom} et {second.Nom}, il y a un loup-garou.");
    }
}
