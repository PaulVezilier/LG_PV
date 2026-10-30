namespace LG.Domain.Roles;

/// <summary>
/// À la mort de sa cible, l'Héritier reçoit le même objet <see cref="Role"/> : il en récupère donc
/// tout l'état (potions restantes, infection déjà utilisée...) et le camp.
/// </summary>
public sealed class Heritier : Role
{
    public override RoleId Id => RoleId.Heritier;
    public override string Nom => "Héritier";
    public override string Description => "La première nuit, vous choisissez un joueur. À sa mort, vous héritez de son rôle, dans l'état où il l'a laissé (et de son camp).";
    public override Camp Camp => Camp.Village;

    public string? CibleId { get; private set; }

    internal void Choisir(Joueur cible) => CibleId = cible.Id;
}
