namespace LG.Domain.Roles;

public sealed class Sorciere : Role
{
    public override RoleId Id => RoleId.Sorciere;
    public override string Nom => "Sorcière";
    public override string Description => "Vous possédez une potion de vie pour sauver la victime des loups, et une potion de mort pour éliminer un joueur. Chacune ne sert qu'une fois.";
    public override Camp Camp => Camp.Village;

    public bool PotionVieDisponible { get; private set; } = true;
    public bool PotionMortDisponible { get; private set; } = true;
    public bool ADesPotions => PotionVieDisponible || PotionMortDisponible;

    internal void UtiliserPotionVie()
    {
        if (!PotionVieDisponible)
            throw new ActionInvalideException("Vous avez déjà utilisé votre potion de vie.");
        PotionVieDisponible = false;
    }

    internal void UtiliserPotionMort()
    {
        if (!PotionMortDisponible)
            throw new ActionInvalideException("Vous avez déjà utilisé votre potion de mort.");
        PotionMortDisponible = false;
    }
}
