namespace LG.Domain;

/// <summary>Pouvoir qu'un joueur qui vient de mourir peut encore utiliser (tir du Chasseur, Croque-Mort...).</summary>
internal sealed record DerniereVolonte(Joueur Joueur, ActionType Type)
{
    public string Consigne => Type == ActionType.Tirer
        ? "Dernière volonté : cliquez sur le joueur à abattre"
        : "Dernière volonté : cliquez sur un joueur à examiner";

    public IReadOnlyList<ActionDisponible> Actions(Partie partie)
    {
        if (Type == ActionType.Tirer)
            return [ActionDisponible.AvecCible(ActionType.Tirer, "Tirer", partie.Vivants), ActionDisponible.SansCible(ActionType.Passer, "Ne tirer sur personne")];
        return [ActionDisponible.AvecCible(ActionType.Designer, "Désigner", partie.Vivants), ActionDisponible.SansCible(ActionType.Passer, "Ne désigner personne")];
    }
}
