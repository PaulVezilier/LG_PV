namespace LG.Domain.Roles;

/// <summary>
/// Se croit Simple Villageois et joue pour le village, jusqu'à ce que les loups le mordent :
/// il survit alors et rejoint la meute.
/// </summary>
public sealed class LoupAmnesique : Role
{
    public override RoleId Id => RoleId.LoupAmnesique;
    public override string Nom => "Loup Amnésique";
    public override string Description => "Un loup qui a oublié sa nature : il commence la partie en se croyant Simple Villageois. Si les loups le mordent, il survit, retrouve la mémoire et rejoint la meute.";
    public override Camp Camp => Reveille ? Camp.LoupsGarous : Camp.Village;

    public override string NomPourSoi => Reveille ? Nom : SimpleVillageois.NomRole;
    public override string DescriptionPourSoi => Reveille
        ? "La morsure de la meute vous a rendu la mémoire : vous chassez désormais avec les loups."
        : SimpleVillageois.DescriptionRole;

    public bool Reveille { get; private set; }

    internal void Reveiller() => Reveille = true;

    internal override bool AbsorberMorsure(Partie partie, Joueur moi)
    {
        if (Reveille)
            return false;
        Reveiller();
        partie.JournaliserPrive(moi, "🐺 La morsure des loups vous rend la mémoire : vous êtes un Loup Amnésique ! Vous survivez et rejoignez la meute.");
        partie.JournaliserLoups($"{moi.Nom} était un Loup Amnésique : la morsure l'a réveillé, il rejoint la meute !");
        return true;
    }
}
