namespace LG.Domain.Roles;

/// <summary>
/// À la mort de sa cible, l'Héritier reste Héritier, mais reçoit le même objet <see cref="Role"/> que le défunt
/// comme <see cref="Pouvoir"/> : il en récupère tout l'état (potions restantes, infection déjà utilisée...) et le camp.
/// </summary>
public sealed class Heritier : Role
{
    public override RoleId Id => RoleId.Heritier;
    public override string Nom => "Héritier";
    public override string Description => "La première nuit, vous choisissez un joueur. À sa mort, vous héritez de ses pouvoirs, dans l'état où il les a laissés (et de son camp), tout en restant Héritier.";
    public override Camp Camp => RoleHerite?.Camp ?? Camp.Village;

    public string? CibleId { get; private set; }

    /// <summary>Rôle du défunt, dont l'Héritier utilise désormais les pouvoirs (null tant que sa cible est en vie).</summary>
    public Role? RoleHerite { get; private set; }

    public override Role Pouvoir => RoleHerite?.Pouvoir ?? this;

    // « Héritier (Sorcière) » pour lui-même et à sa mort ; la Voyante, elle, voit seulement « Héritier ».
    public override string NomPourSoi => RoleHerite is null ? Nom : $"{Nom} ({RoleHerite.NomPourSoi})";
    public override string NomRevele => RoleHerite is null ? Nom : $"{Nom} ({RoleHerite.NomRevele})";
    public override string DescriptionPourSoi => RoleHerite is null
        ? Description
        : $"Vous avez hérité des pouvoirs et du camp de ce rôle. {RoleHerite.DescriptionPourSoi}";

    internal void Choisir(Joueur cible) => CibleId = cible.Id;

    internal override void QuandUnJoueurMeurt(Partie partie, Joueur moi, Joueur defunt)
    {
        if (RoleHerite is not null || defunt.Id != CibleId)
            return;

        RoleHerite = defunt.Pouvoir!;
        partie.JournaliserPrive(moi, $"📜 {defunt.Nom} est mort : vous restez Héritier, mais vous obtenez les pouvoirs de {RoleHerite.NomPourSoi}. {RoleHerite.DescriptionPourSoi}");
        if (moi.EstLoup)
            partie.JournaliserLoups($"{moi.Nom} a hérité des pouvoirs de {defunt.Nom} et rejoint la meute.");
    }
}
