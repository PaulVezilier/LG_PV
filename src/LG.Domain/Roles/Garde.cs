namespace LG.Domain.Roles;

public sealed class Garde : Role
{
    public override RoleId Id => RoleId.Garde;
    public override string Nom => "Garde";
    public override string Description => "Chaque nuit, vous protégez un joueur (vous compris) de l'attaque des loups. Vous ne pouvez pas protéger la même personne deux nuits de suite.";
    public override Camp Camp => Camp.Village;

    public string? DernierProtegeId { get; private set; }

    public bool PeutProteger(Joueur cible) => cible.Id != DernierProtegeId;

    internal void EnregistrerProtection(Joueur? cible) => DernierProtegeId = cible?.Id;
}
