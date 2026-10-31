namespace LG.Domain.Roles;

public sealed class LoupVengeur : LoupGarou
{
    public override RoleId Id => RoleId.LoupVengeur;
    public override string Nom => "Loup Vengeur";
    public override string Description => "Vous chassez avec la meute. Quand vous mourez, vous pouvez emporter le joueur de votre choix avec vous.";

    internal override string AnnonceDerniereVolonte => DernierTir.Annonce;
    internal override string ConsigneDerniereVolonte => DernierTir.Consigne;
    internal override IReadOnlyList<ActionDisponible> ActionsDerniereVolonte(Partie partie, Joueur moi) => DernierTir.Actions(partie);
    internal override void ExecuterDerniereVolonte(Partie partie, Joueur moi, Joueur cible) => DernierTir.Executer(partie, moi, cible);
}
