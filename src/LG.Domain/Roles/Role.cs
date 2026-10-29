namespace LG.Domain.Roles;

/// <summary>
/// Classe de base de tous les rôles. Un rôle décrit l'identité d'un joueur (nom, camp)
/// et porte l'état propre à son pouvoir (potions, dernière protection...).
/// Le déroulement des actions de nuit est orchestré par les étapes de <see cref="Nuit.EtapeNuit"/>.
/// </summary>
public abstract class Role
{
    public abstract RoleId Id { get; }
    public abstract string Nom { get; }
    public abstract string Description { get; }
    public abstract Camp Camp { get; }

    public override string ToString() => Nom;
}
