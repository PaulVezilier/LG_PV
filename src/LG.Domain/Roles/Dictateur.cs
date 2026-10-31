namespace LG.Domain.Roles;

public sealed class Dictateur : Role
{
    public override RoleId Id => RoleId.Dictateur;
    public override string Nom => "Dictateur";
    public override string Description => "Une fois dans la partie, à n'importe quel moment de la journée, vous pouvez faire un coup d'État : vous exécutez le joueur de votre choix et la nuit tombe aussitôt, sans vote.";
    public override Camp Camp => Camp.Village;

    public bool CoupDisponible { get; private set; } = true;

    internal void UtiliserCoup()
    {
        if (!CoupDisponible)
            throw new ActionInvalideException("Vous avez déjà fait votre coup d'État.");
        CoupDisponible = false;
    }

    internal override IEnumerable<ActionDisponible> ActionsDuJour(Partie partie, Joueur moi)
        => CoupDisponible ? [ActionDisponible.AvecCible(ActionType.CoupDEtat, "Coup d'État", partie.Vivants.Where(j => j != moi))] : [];

    /// <summary>La cible meurt, le vote est annulé et la nuit tombe.</summary>
    internal override void ExecuterActionDuJour(Partie partie, Joueur moi, ActionType type, Joueur? cible)
    {
        UtiliserCoup();
        partie.JournaliserPublic($"⚔️ Coup d'État ! {moi.Nom} prend le contrôle du village et fait exécuter {cible!.Nom}.");
        partie.Tuer(cible, "a été exécuté par le Dictateur");
        partie.TerminerJournee();
    }
}
