namespace LG.Domain;

/// <summary>Une ligne du journal de la partie : message du narrateur ou message de chat (Auteur renseigné).</summary>
public sealed record EvenementJournal(
    long Numero,
    DateTimeOffset Date,
    Canal Canal,
    string Texte,
    string? Auteur,
    IReadOnlyCollection<string> Destinataires)
{
    /// <summary>Tout devient visible une fois la partie terminée.</summary>
    public bool EstVisiblePar(Joueur joueur, Phase phase) => phase == Phase.Terminee || Canal switch
    {
        Canal.Public => true,
        Canal.Loups => joueur.EstLoup,
        Canal.Morts => !joueur.EstVivant,
        Canal.Prive => Destinataires.Contains(joueur.Id),
        _ => false,
    };
}
