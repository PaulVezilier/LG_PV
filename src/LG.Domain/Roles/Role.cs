namespace LG.Domain.Roles;

/// <summary>
/// Classe de base de tous les rôles. Un rôle décrit l'identité d'un joueur (nom, camp),
/// porte l'état propre à son pouvoir (potions, dernière protection...) et réagit aux moments
/// clés de la partie grâce aux crochets ci-dessous, que <see cref="Partie"/> appelle sans connaître les rôles.
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

    /// <summary>Ce que le village découvre à la mort du joueur et en fin de partie.</summary>
    public virtual string NomRevele => Nom;

    /// <summary>
    /// Rôle dont le joueur utilise les pouvoirs : le rôle lui-même, sauf pour un Héritier qui a hérité.
    /// Les étapes de nuit et les crochets ci-dessous sont toujours appelés sur le pouvoir, jamais sur l'identité.
    /// </summary>
    public virtual Role Pouvoir => this;

    /// <summary>Vrai si le joueur lit et écrit dans le chat des morts pendant la nuit (Croque-Mort).</summary>
    internal virtual bool EntendLesMortsLaNuit => false;

    public override string ToString() => Nom;

    // ---------------------------------------------------------------- Crochets
    // « moi » est le joueur qui porte ce rôle. Il n'est pas stocké dans le rôle,
    // car un Héritier peut récupérer le même objet rôle à la mort de son propriétaire.

    /// <summary>Au lever du jour, pour chaque vivant (Loup Bavard : tirage du mot).</summary>
    internal virtual void DebutDeJournee(Partie partie, Joueur moi) { }

    /// <summary>Après le vote ou le coup d'État, pour chaque vivant : morts de fin de journée (Vétéran, Loup Bavard).</summary>
    internal virtual void FinDeJournee(Partie partie, Joueur moi) { }

    /// <summary>Après chaque message de ce joueur dans le chat du village, le jour.</summary>
    internal virtual void ApresMessageDuJour(Partie partie, Joueur moi, string texte) { }

    /// <summary>Actions du jour en plus du vote (Dictateur).</summary>
    internal virtual IEnumerable<ActionDisponible> ActionsDuJour(Partie partie, Joueur moi) => [];

    internal virtual void ExecuterActionDuJour(Partie partie, Joueur moi, ActionType type, Joueur? cible) { }

    /// <summary>Morsure des loups, avant l'infection et la potion de vie : vrai si le rôle l'absorbe (Loup Amnésique).</summary>
    internal virtual bool AbsorberMorsure(Partie partie, Joueur moi) => false;

    /// <summary>Morsure des loups ni infectée ni soignée : vrai si le joueur y survit (Chaperon Rouge, Vétéran).</summary>
    internal virtual bool SurvivreMorsure(Partie partie, Joueur moi) => false;

    /// <summary>Un autre joueur vient de mourir, et « moi » est encore vivant (Héritier).</summary>
    internal virtual void QuandUnJoueurMeurt(Partie partie, Joueur moi, Joueur defunt) { }

    // ---------------------------------------------------------------- Dernière volonté

    /// <summary>Message privé reçu à la mort quand le rôle a une dernière volonté (null = aucune).</summary>
    internal virtual string? AnnonceDerniereVolonte => null;

    /// <summary>Texte du bandeau pendant la phase « Dernières volontés ».</summary>
    internal virtual string? ConsigneDerniereVolonte => null;

    internal virtual IReadOnlyList<ActionDisponible> ActionsDerniereVolonte(Partie partie, Joueur moi) => [];

    /// <summary>Appelée avec une cible : l'action « Passer » est traitée par <see cref="Partie"/>.</summary>
    internal virtual void ExecuterDerniereVolonte(Partie partie, Joueur moi, Joueur cible) { }
}
