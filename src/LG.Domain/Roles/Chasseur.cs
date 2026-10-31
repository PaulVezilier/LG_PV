namespace LG.Domain.Roles;

public sealed class Chasseur : Role
{
    public override RoleId Id => RoleId.Chasseur;
    public override string Nom => "Chasseur";
    public override string Description => "Quand vous mourez, de jour comme de nuit, vous pouvez tirer une dernière balle et tuer le joueur de votre choix.";
    public override Camp Camp => Camp.Village;

    internal override string AnnonceDerniereVolonte => DernierTir.Annonce;
    internal override string ConsigneDerniereVolonte => DernierTir.Consigne;
    internal override IReadOnlyList<ActionDisponible> ActionsDerniereVolonte(Partie partie, Joueur moi) => DernierTir.Actions(partie);
    internal override void ExecuterDerniereVolonte(Partie partie, Joueur moi, Joueur cible) => DernierTir.Executer(partie, moi, cible);
}
