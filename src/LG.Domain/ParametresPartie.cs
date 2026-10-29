namespace LG.Domain;

public sealed record ParametresPartie
{
    public int JoueursMinimum { get; init; } = 4;
    public int JoueursMaximum { get; init; } = 16;
    public TimeSpan DureeEtapeNuit { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan DureeJour { get; init; } = TimeSpan.FromMinutes(3);

    /// <summary>Rôles à distribuer. Null = composition automatique selon le nombre de joueurs.</summary>
    public IReadOnlyList<RoleId>? Composition { get; init; }
}
