namespace LG.Domain.Roles;

public sealed class CroqueMort : Role
{
    public override RoleId Id => RoleId.CroqueMort;
    public override string Nom => "Croque-Mort";
    public override string Description => "La nuit, vous pouvez discuter avec les morts. À votre mort, vous désignez un joueur : le village apprend qu'entre lui et un autre joueur tiré au sort, il y a un loup-garou.";
    public override Camp Camp => Camp.Village;
    public override ActionType? DerniereVolonte => ActionType.Designer;
}
