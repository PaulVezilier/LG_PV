using LG.Domain.Roles;

namespace LG.Domain;

public sealed class Joueur
{
    internal Joueur(string id, string nom)
    {
        Id = id;
        Nom = nom;
    }

    public string Id { get; }
    public string Nom { get; }

    /// <summary>Rôle attribué au lancement de la partie (null dans le lobby). Peut changer : voir Héritier.</summary>
    public Role? Role { get; internal set; }

    /// <summary>Carte reçue au lancement, même si le rôle a changé depuis (Héritier) : sert au récapitulatif des rôles.</summary>
    public RoleId? RoleInitial { get; internal set; }

    public bool EstVivant { get; internal set; } = true;

    /// <summary>Infecté par le Loup-Garou Noir : garde son rôle mais rejoint le camp des loups.</summary>
    public bool EstInfecte { get; private set; }

    /// <summary>Vétéran mordu par les loups : il mourra à la fin de la journée.</summary>
    public bool EstBlesse { get; private set; }

    /// <summary>Partenaire désigné par Cupidon : si l'un meurt, l'autre meurt aussi.</summary>
    public Joueur? Amoureux { get; internal set; }

    public Camp Camp => EstInfecte ? Camp.LoupsGarous : Role?.Camp ?? Camp.Village;

    public bool EstLoup => Role is not null && Camp == Camp.LoupsGarous;

    internal void Infecter() => EstInfecte = true;

    internal void Blesser() => EstBlesse = true;

    public override string ToString() => Nom;
}
