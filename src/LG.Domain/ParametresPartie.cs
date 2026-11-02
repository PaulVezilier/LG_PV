namespace LG.Domain;

public sealed record ParametresPartie
{
    public int JoueursMinimum { get; init; } = 4;
    public int JoueursMaximum { get; init; } = 16;
    /// <summary>Décompte affiché entre le clic sur « Lancer la partie » et la première nuit (zéro = immédiat).</summary>
    public TimeSpan DureeLancement { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan DureeEtapeNuit { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan DureeJour { get; init; } = TimeSpan.FromMinutes(3);
    public TimeSpan DureeDerniereVolonte { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Rôles à distribuer. Null = composition automatique (l'hôte peut la changer dans le lobby).</summary>
    public IReadOnlyList<RoleId>? Composition { get; init; }
}
