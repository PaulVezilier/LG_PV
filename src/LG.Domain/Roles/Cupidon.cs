namespace LG.Domain.Roles;

public sealed class Cupidon : Role
{
    public override RoleId Id => RoleId.Cupidon;
    public override string Nom => "Cupidon";
    public override string Description => "La première nuit, vous unissez deux joueurs (vous compris). Si l'un meurt, l'autre meurt de chagrin. S'ils sont de camps opposés, ils gagnent ensemble en étant les derniers survivants.";
    public override Camp Camp => Camp.Village;
}
