namespace LG.Domain.Roles;

public sealed class LoupPerfide : LoupGarou
{
    public override RoleId Id => RoleId.LoupPerfide;
    public override string Nom => "Loup Perfide";
    public override string Description => "Vous chassez avec la meute. Si la Voyante vous sonde, elle vous voit comme un Simple Villageois.";
    public override string NomVuParVoyante => SimpleVillageois.NomRole;
}
