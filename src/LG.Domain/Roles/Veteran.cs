namespace LG.Domain.Roles;

/// <summary>La blessure est portée par le joueur (<see cref="Joueur.EstBlesse"/>), pas par le rôle.</summary>
public sealed class Veteran : Role
{
    public override RoleId Id => RoleId.Veteran;
    public override string Nom => "Vétéran";
    public override string Description => "Si les loups vous attaquent, vous survivez à la nuit : tout le village l'apprend, et vous succombez à vos blessures à la fin de la journée suivante, après avoir voté une dernière fois.";
    public override Camp Camp => Camp.Village;
}
