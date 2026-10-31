namespace LG.Domain.Roles;

/// <summary>
/// La blessure est portée par le joueur (<see cref="Joueur.EstBlesse"/>), pas par le rôle :
/// un Héritier qui récupère le rôle d'un Vétéran blessé n'hérite pas de sa blessure.
/// </summary>
public sealed class Veteran : Role
{
    public override RoleId Id => RoleId.Veteran;
    public override string Nom => "Vétéran";
    public override string Description => "Si les loups vous attaquent, vous survivez à la nuit : tout le village l'apprend, et vous succombez à vos blessures à la fin de la journée suivante, après avoir voté une dernière fois.";
    public override Camp Camp => Camp.Village;

    internal override bool SurvivreMorsure(Partie partie, Joueur moi)
    {
        if (moi.EstBlesse)
            return false;
        moi.Blesser();
        partie.JournaliserPublic($"🩸 {moi.Nom} a été attaqué par les loups mais a survécu ! Gravement blessé, il succombera à ses blessures ce soir.");
        return true;
    }

    internal override void FinDeJournee(Partie partie, Joueur moi)
    {
        if (moi.EstBlesse)
            partie.Tuer(moi, "a succombé à ses blessures");
    }
}
