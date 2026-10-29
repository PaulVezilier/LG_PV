namespace LG.Domain.Roles;

public class LoupGarou : Role
{
    public override RoleId Id => RoleId.LoupGarou;
    public override string Nom => "Loup-Garou";
    public override string Description => "Chaque nuit, vous vous réveillez avec la meute pour dévorer un villageois. Vous gagnez quand tous les villageois sont morts.";
    public override Camp Camp => Camp.LoupsGarous;
}
