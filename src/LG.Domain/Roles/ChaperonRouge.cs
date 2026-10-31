namespace LG.Domain.Roles;

public sealed class ChaperonRouge : Role
{
    public override RoleId Id => RoleId.ChaperonRouge;
    public override string Nom => "Chaperon Rouge";
    public override string Description => "Tant que le Chasseur est en vie, les loups-garous ne peuvent pas vous tuer. Le reste peut vous tuer normalement.";
    public override Camp Camp => Camp.Village;

    internal override bool SurvivreMorsure(Partie partie, Joueur moi) => partie.VivantsAvec<Chasseur>().Any();
}
