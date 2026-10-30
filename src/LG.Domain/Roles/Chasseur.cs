namespace LG.Domain.Roles;

public sealed class Chasseur : Role
{
    public override RoleId Id => RoleId.Chasseur;
    public override string Nom => "Chasseur";
    public override string Description => "Quand vous mourez, de jour comme de nuit, vous pouvez tirer une dernière balle et tuer le joueur de votre choix.";
    public override Camp Camp => Camp.Village;
    public override ActionType? DerniereVolonte => ActionType.Tirer;
}
