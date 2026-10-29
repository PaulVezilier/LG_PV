using LG.Domain.Roles;

namespace LG.Domain.Tests;

internal sealed class HorlogeFactice : TimeProvider
{
    public DateTimeOffset Maintenant { get; set; } = new(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Maintenant;

    public void Avancer(TimeSpan duree) => Maintenant += duree;
}

/// <summary>Une partie déjà lancée avec une composition imposée, et des raccourcis pour la piloter.</summary>
internal sealed class PartieDeTest
{
    public PartieDeTest(params RoleId[] composition)
    {
        var parametres = new ParametresPartie { Composition = composition, JoueursMinimum = 2 };
        Partie = new Partie("TEST", parametres, Horloge, new Random(42));
        for (var i = 0; i < composition.Length; i++)
            Partie.AjouterJoueur($"Joueur{i + 1}");
        Partie.Demarrer(Partie.HoteId!);
    }

    public HorlogeFactice Horloge { get; } = new();
    public Partie Partie { get; }

    /// <summary>Les joueurs ayant exactement ce rôle (un LoupGarouNoir n'est pas renvoyé pour LoupGarou).</summary>
    public IReadOnlyList<Joueur> Tous<TRole>() where TRole : Role
        => Partie.Joueurs.Where(j => j.Role!.GetType() == typeof(TRole)).ToList();

    public Joueur Un<TRole>() where TRole : Role => Tous<TRole>()[0];

    public void Agir(Joueur joueur, ActionType action, Joueur? cible = null) => Partie.Agir(joueur.Id, action, cible?.Id);

    public bool ARecu(Joueur joueur, string texte)
        => Partie.Journal.Any(e => e.Canal == Canal.Prive && e.Destinataires.Contains(joueur.Id) && e.Texte.Contains(texte));

    /// <summary>Laisse s'écouler le temps de chaque étape restante de la nuit.</summary>
    public void LaisserPasserLaNuit()
    {
        while (Partie.Phase == Phase.Nuit)
        {
            Horloge.Avancer(Partie.Parametres.DureeEtapeNuit);
            Partie.Actualiser();
        }
    }

    public void LaisserPasserLeJour()
    {
        Horloge.Avancer(Partie.Parametres.DureeJour);
        Partie.Actualiser();
    }
}
