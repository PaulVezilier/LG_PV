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

    /// <summary>Rôle attribué au lancement de la partie (null dans le lobby).</summary>
    public Role? Role { get; internal set; }

    public bool EstVivant { get; internal set; } = true;

    /// <summary>Infecté par le Loup-Garou Noir : garde son rôle mais rejoint le camp des loups.</summary>
    public bool EstInfecte { get; private set; }

    public Camp Camp => EstInfecte ? Camp.LoupsGarous : Role?.Camp ?? Camp.Village;

    public bool EstLoup => Role is not null && Camp == Camp.LoupsGarous;

    internal void Infecter() => EstInfecte = true;

    public override string ToString() => Nom;
}
