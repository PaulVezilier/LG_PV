namespace LG.Domain.Roles;

public sealed class SimpleVillageois : Role
{
    internal const string NomRole = "Simple Villageois";
    internal const string DescriptionRole = "Vous n'avez aucun pouvoir, seulement votre flair. Démasquez les loups-garous et éliminez-les lors des votes du jour.";

    public override RoleId Id => RoleId.SimpleVillageois;
    public override string Nom => NomRole;
    public override string Description => DescriptionRole;
    public override Camp Camp => Camp.Village;
}
