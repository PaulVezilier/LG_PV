namespace LG.Domain;

/// <summary>Une action qu'un joueur peut effectuer maintenant, avec ses cibles possibles.</summary>
public sealed record ActionDisponible(ActionType Type, string Libelle, bool CibleRequise, IReadOnlyList<string> Cibles)
{
    public static ActionDisponible AvecCible(ActionType type, string libelle, IEnumerable<Joueur> cibles)
        => new(type, libelle, true, cibles.Select(c => c.Id).ToList());

    public static ActionDisponible SansCible(ActionType type, string libelle)
        => new(type, libelle, false, []);
}
