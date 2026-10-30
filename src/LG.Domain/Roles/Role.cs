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

    /// <summary>Ce que le joueur croit être (diffère du vrai rôle pour le Loup Amnésique).</summary>
    public virtual string NomPourSoi => Nom;
    public virtual string DescriptionPourSoi => Description;

    /// <summary>Ce que la Voyante découvre en sondant ce joueur.</summary>
    public virtual string NomVuParVoyante => Nom;

    /// <summary>Pouvoir utilisable juste après la mort du joueur (null = aucun).</summary>
    public virtual ActionType? DerniereVolonte => null;

    public override string ToString() => Nom;
}
