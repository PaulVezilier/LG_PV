namespace LG.Domain.Roles;

public static class FabriqueRoles
{
    public static Role Creer(RoleId id) => id switch
    {
        RoleId.SimpleVillageois => new SimpleVillageois(),
        RoleId.LoupGarou => new LoupGarou(),
        RoleId.LoupGarouNoir => new LoupGarouNoir(),
        RoleId.Sorciere => new Sorciere(),
        RoleId.Voyante => new Voyante(),
        RoleId.Garde => new Garde(),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Rôle inconnu."),
    };
}
