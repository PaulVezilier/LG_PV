namespace LG.Domain.Roles;

public sealed class Voyante : Role
{
    public override RoleId Id => RoleId.Voyante;
    public override string Nom => "Voyante";
    public override string Description => "Chaque nuit, vous pouvez découvrir le rôle d'un joueur de votre choix.";
    public override Camp Camp => Camp.Village;
}
