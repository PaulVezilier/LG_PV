namespace LG.Domain.Roles;

public static class FabriqueRoles
{
    /// <summary>Un exemplaire de chaque rôle, pour afficher noms, camps et descriptions.</summary>
    public static IReadOnlyList<Role> Catalogue { get; } = Enum.GetValues<RoleId>().Select(Creer).ToList();

    public static Role Creer(RoleId id) => id switch
    {
        RoleId.SimpleVillageois => new SimpleVillageois(),
        RoleId.Voyante => new Voyante(),
        RoleId.Sorciere => new Sorciere(),
        RoleId.Garde => new Garde(),
        RoleId.Chasseur => new Chasseur(),
        RoleId.Cupidon => new Cupidon(),
        RoleId.Dictateur => new Dictateur(),
        RoleId.Heritier => new Heritier(),
        RoleId.ChaperonRouge => new ChaperonRouge(),
        RoleId.CroqueMort => new CroqueMort(),
        RoleId.Veteran => new Veteran(),
        RoleId.LoupGarou => new LoupGarou(),
        RoleId.LoupGarouNoir => new LoupGarouNoir(),
        RoleId.LoupBavard => new LoupBavard(),
        RoleId.LoupVengeur => new LoupVengeur(),
        RoleId.LoupPerfide => new LoupPerfide(),
        RoleId.LoupAmnesique => new LoupAmnesique(),
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Rôle inconnu."),
    };
}
