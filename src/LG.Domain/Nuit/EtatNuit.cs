namespace LG.Domain.Nuit;

/// <summary>Ce qui s'est décidé pendant la nuit en cours ; résolu à l'aube par <see cref="Partie"/>.</summary>
internal sealed class EtatNuit
{
    public Joueur? PremierAmoureux { get; set; }
    public Joueur? Protege { get; set; }
    public Dictionary<string, string> VotesLoups { get; } = [];
    public Joueur? VictimeLoups { get; set; }
    public bool VictimeInfectee { get; set; }
    public bool VictimeSoignee { get; set; }
    public Joueur? Empoisonne { get; set; }

    /// <summary>Joueurs ayant fini d'agir pendant l'étape en cours (vidé à chaque nouvelle étape).</summary>
    public HashSet<string> ActeursTermines { get; } = [];
}
