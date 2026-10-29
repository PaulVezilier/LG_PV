namespace LG.Domain.Roles;

/// <summary>Un loup-garou qui peut, une fois par partie, infecter la victime de la meute au lieu de la tuer.</summary>
public sealed class LoupGarouNoir : LoupGarou
{
    public override RoleId Id => RoleId.LoupGarouNoir;
    public override string Nom => "Loup-Garou Noir";
    public override string Description => "Vous chassez avec la meute. Une fois dans la partie, vous pouvez infecter la victime des loups : elle survit, garde son rôle, mais rejoint votre camp.";

    public bool InfectionDisponible { get; private set; } = true;

    internal void UtiliserInfection()
    {
        if (!InfectionDisponible)
            throw new ActionInvalideException("Vous avez déjà utilisé votre infection.");
        InfectionDisponible = false;
    }
}
