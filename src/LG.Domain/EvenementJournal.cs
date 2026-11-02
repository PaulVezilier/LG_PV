namespace LG.Domain;

/// <summary>Une ligne du journal de la partie : message du narrateur ou message de chat (Auteur renseigné).</summary>
public sealed record EvenementJournal(
    long Numero,
    DateTimeOffset Date,
    int Tour,
    Phase Phase,
    Canal Canal,
    string Texte,
    Joueur? Auteur,
    IReadOnlyCollection<string> Destinataires)
{
    public bool Nocturne => Phase == Phase.Nuit;

    /// <summary>Tout devient visible une fois la partie terminée.</summary>
    public bool EstVisiblePar(Joueur joueur, Phase phase) => phase == Phase.Terminee || Canal switch
    {
        Canal.Public => true,
        Canal.Loups => joueur.EstLoup,
        // Le Croque-Mort entend les morts, mais seulement la nuit.
        Canal.Morts => !joueur.EstVivant || Nocturne && joueur.Pouvoir is { EntendLesMortsLaNuit: true },
        Canal.Prive => Destinataires.Contains(joueur.Id),
        _ => false,
    };
}
