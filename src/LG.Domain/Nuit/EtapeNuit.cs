namespace LG.Domain.Nuit;

/// <summary>
/// Une étape de la nuit (Garde, Voyante, Loups...). Les étapes sont jouées l'une après l'autre ;
/// une étape se termine quand tous ses acteurs ont fini, ou à la fin du temps imparti.
/// La validation « action proposée + cible autorisée » est faite par <see cref="Partie"/> avant <see cref="Executer"/>.
/// </summary>
internal abstract class EtapeNuit
{
    /// <summary>Ce que tout le village voit pendant l'étape (« Le Garde doit protéger quelqu'un »).</summary>
    public abstract string Narration { get; }

    /// <summary>Instruction affichée aux acteurs de l'étape.</summary>
    public abstract string Consigne { get; }

    /// <summary>Joueurs vivants qui agissent pendant cette étape.</summary>
    public abstract IEnumerable<Joueur> Acteurs(Partie partie);

    public virtual bool DoitEtreJouee(Partie partie) => Acteurs(partie).Any();

    public virtual void Debuter(Partie partie) { }

    public abstract IReadOnlyList<ActionDisponible> ActionsDisponibles(Partie partie, Joueur joueur);

    public abstract void Executer(Partie partie, Joueur joueur, ActionType action, Joueur? cible);

    public virtual void Terminer(Partie partie) { }

    public bool EstTerminee(Partie partie) => Acteurs(partie).All(a => ATermine(partie, a));

    protected static void MarquerTermine(Partie partie, Joueur joueur) => partie.EtatNuit.ActeursTermines.Add(joueur.Id);

    protected static bool ATermine(Partie partie, Joueur joueur) => partie.EtatNuit.ActeursTermines.Contains(joueur.Id);
}
