namespace LG.Domain.Roles;

public sealed class LoupVengeur : LoupGarou
{
    public override RoleId Id => RoleId.LoupVengeur;
    public override string Nom => "Loup Vengeur";
    public override string Description => "Vous chassez avec la meute. Quand vous mourez, vous pouvez emporter le joueur de votre choix avec vous.";
    public override ActionType? DerniereVolonte => ActionType.Tirer;
}
