namespace LG.Domain;

public static class CompositionRoles
{
    private static readonly RoleId[] RolesSpeciaux = [RoleId.Voyante, RoleId.Sorciere, RoleId.Garde];

    /// <summary>
    /// Un loup pour 4 joueurs (le premier est Noir dès qu'il y a au moins 2 loups),
    /// puis Voyante, Sorcière, Garde, et des Simples Villageois pour compléter.
    /// </summary>
    public static IReadOnlyList<RoleId> Automatique(int nombreJoueurs)
    {
        var roles = new List<RoleId>();
        var nombreLoups = Math.Max(1, nombreJoueurs / 4);
        for (var i = 0; i < nombreLoups; i++)
            roles.Add(i == 0 && nombreLoups >= 2 ? RoleId.LoupGarouNoir : RoleId.LoupGarou);

        foreach (var role in RolesSpeciaux)
            if (roles.Count < nombreJoueurs)
                roles.Add(role);

        while (roles.Count < nombreJoueurs)
            roles.Add(RoleId.SimpleVillageois);

        return roles;
    }
}
