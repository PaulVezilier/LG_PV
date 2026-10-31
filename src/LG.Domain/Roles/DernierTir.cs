namespace LG.Domain.Roles;

/// <summary>Dernière volonté partagée par le Chasseur et le Loup Vengeur : abattre un joueur en mourant.</summary>
internal static class DernierTir
{
    public const string Annonce = "Vous êtes mort, mais il vous reste une balle : choisissez sur qui tirer.";
    public const string Consigne = "Dernière volonté : cliquez sur le joueur à abattre";

    public static IReadOnlyList<ActionDisponible> Actions(Partie partie)
        => [ActionDisponible.AvecCible(ActionType.Tirer, "Tirer", partie.Vivants), ActionDisponible.SansCible(ActionType.Passer, "Ne tirer sur personne")];

    public static void Executer(Partie partie, Joueur tireur, Joueur cible)
    {
        partie.JournaliserPublic($"🎯 Dans un dernier souffle, {tireur.Nom} tire sur {cible.Nom} !");
        partie.Tuer(cible, "a été abattu");
    }
}
