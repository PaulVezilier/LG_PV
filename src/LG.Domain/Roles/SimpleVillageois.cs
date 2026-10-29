namespace LG.Domain.Roles;

public sealed class SimpleVillageois : Role
{
    public override RoleId Id => RoleId.SimpleVillageois;
    public override string Nom => "Simple Villageois";
    public override string Description => "Vous n'avez aucun pouvoir, seulement votre flair. Démasquez les loups-garous et éliminez-les lors des votes du jour.";
    public override Camp Camp => Camp.Village;
}
