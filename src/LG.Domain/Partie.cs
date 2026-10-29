using LG.Domain.Nuit;
using LG.Domain.Roles;

namespace LG.Domain;

/// <summary>
/// Une partie de Loup-Garou : Lobby → (Nuit ⇄ Jour)* → Terminée.
/// Cette classe n'est pas thread-safe : l'appelant doit sérialiser les accès (lock).
/// </summary>
public sealed class Partie
{
    /// <summary>Ordre de réveil pendant la nuit. Ajouter un rôle de nuit = ajouter une étape ici.</summary>
    private static readonly EtapeNuit[] OrdreNuit =
    [
        new EtapeGarde(),
        new EtapeVoyante(),
        new EtapeLoupsGarous(),
        new EtapeLoupNoir(),
        new EtapeSorciere(),
    ];

    private readonly List<Joueur> _joueurs = [];
    private readonly List<EvenementJournal> _journal = [];
    private readonly Dictionary<string, string> _votesJour = [];
    private readonly TimeProvider _horloge;
    private int _indexEtape = -1;

    public Partie(string id, ParametresPartie? parametres = null, TimeProvider? horloge = null, Random? aleatoire = null)
    {
        Id = id;
        Parametres = parametres ?? new ParametresPartie();
        _horloge = horloge ?? TimeProvider.System;
        Aleatoire = aleatoire ?? Random.Shared;
    }

    public string Id { get; }
    public ParametresPartie Parametres { get; }
    public Phase Phase { get; private set; } = Phase.Lobby;
    public int NumeroTour { get; private set; }
    public DateTimeOffset? FinPhase { get; private set; }

    /// <summary>Camp gagnant une fois la partie terminée (null = égalité ou partie en cours).</summary>
    public Camp? Vainqueur { get; private set; }

    public string? HoteId { get; private set; }
    public IReadOnlyList<Joueur> Joueurs => _joueurs;
    public IEnumerable<Joueur> Vivants => _joueurs.Where(j => j.EstVivant);
    public IReadOnlyList<EvenementJournal> Journal => _journal;
    public IReadOnlyDictionary<string, string> VotesJour => _votesJour;

    internal Random Aleatoire { get; }
    internal EtatNuit EtatNuit { get; private set; } = new();

    internal EtapeNuit? EtapeCourante =>
        Phase == Phase.Nuit && _indexEtape >= 0 && _indexEtape < OrdreNuit.Length ? OrdreNuit[_indexEtape] : null;

    public Joueur TrouverJoueur(string id)
        => _joueurs.FirstOrDefault(j => j.Id == id) ?? throw new ActionInvalideException("Joueur introuvable.");

    // ---------------------------------------------------------------- Lobby

    public Joueur AjouterJoueur(string nom)
    {
        nom = nom?.Trim() ?? "";
        if (Phase != Phase.Lobby)
            throw new ActionInvalideException("La partie a déjà commencé.");
        if (nom.Length is 0 or > 20)
            throw new ActionInvalideException("Le pseudo doit faire entre 1 et 20 caractères.");
        if (_joueurs.Count >= Parametres.JoueursMaximum)
            throw new ActionInvalideException("La partie est complète.");
        if (_joueurs.Any(j => string.Equals(j.Nom, nom, StringComparison.OrdinalIgnoreCase)))
            throw new ActionInvalideException("Ce pseudo est déjà pris.");

        var joueur = new Joueur(Guid.NewGuid().ToString("N")[..8], nom);
        _joueurs.Add(joueur);
        HoteId ??= joueur.Id;
        JournaliserPublic($"{nom} a rejoint la partie.");
        return joueur;
    }

    public void Demarrer(string demandeurId)
    {
        if (Phase != Phase.Lobby)
            throw new ActionInvalideException("La partie a déjà commencé.");
        if (demandeurId != HoteId)
            throw new ActionInvalideException("Seul l'hôte peut lancer la partie.");
        if (_joueurs.Count < Parametres.JoueursMinimum)
            throw new ActionInvalideException($"Il faut au moins {Parametres.JoueursMinimum} joueurs.");

        var composition = Parametres.Composition ?? CompositionRoles.Automatique(_joueurs.Count);
        if (composition.Count != _joueurs.Count)
            throw new ActionInvalideException("La composition ne correspond pas au nombre de joueurs.");

        var roles = composition.Select(FabriqueRoles.Creer).ToArray();
        Aleatoire.Shuffle(roles);
        for (var i = 0; i < roles.Length; i++)
            _joueurs[i].Role = roles[i];

        var resume = roles.GroupBy(r => r.Nom).Select(g => g.Count() > 1 ? $"{g.Count()} × {g.Key}" : g.Key);
        JournaliserPublic($"La partie commence ! Composition : {string.Join(", ", resume)}.");
        foreach (var joueur in _joueurs)
            JournaliserPrive(joueur, $"Vous êtes {joueur.Role!.Nom}. {joueur.Role.Description}");
        JournaliserLoups($"Votre meute : {string.Join(", ", _joueurs.Where(j => j.EstLoup))}.");

        CommencerNuit();
    }

    // ---------------------------------------------------------------- Actions des joueurs

    public IReadOnlyList<ActionDisponible> ActionsDisponibles(string joueurId) => ActionsDisponibles(TrouverJoueur(joueurId));

    private IReadOnlyList<ActionDisponible> ActionsDisponibles(Joueur joueur)
    {
        if (!joueur.EstVivant)
            return [];
        return Phase switch
        {
            Phase.Nuit when EtapeCourante is { } etape && etape.Acteurs(this).Contains(joueur) => etape.ActionsDisponibles(this, joueur),
            Phase.Jour => [ActionDisponible.AvecCible(ActionType.Voter, "Voter contre", Vivants.Where(j => j != joueur))],
            _ => [],
        };
    }

    public void Agir(string joueurId, ActionType type, string? cibleId)
    {
        var joueur = TrouverJoueur(joueurId);
        var action = ActionsDisponibles(joueur).FirstOrDefault(a => a.Type == type)
            ?? throw new ActionInvalideException("Cette action n'est pas possible maintenant.");

        Joueur? cible = null;
        if (action.CibleRequise)
        {
            if (cibleId is null || !action.Cibles.Contains(cibleId))
                throw new ActionInvalideException("Cible invalide.");
            cible = TrouverJoueur(cibleId);
        }

        if (Phase == Phase.Nuit)
        {
            var etape = EtapeCourante!;
            etape.Executer(this, joueur, type, cible);
            if (etape.EstTerminee(this))
                PasserEtapeSuivante();
        }
        else
        {
            _votesJour[joueur.Id] = cible!.Id;
            JournaliserPublic($"{joueur.Nom} vote contre {cible.Nom}.");
            if (Vivants.All(j => _votesJour.ContainsKey(j.Id)))
                ResoudreJour();
        }
    }

    public void EnvoyerMessage(string joueurId, string texte)
    {
        var joueur = TrouverJoueur(joueurId);
        texte = texte?.Trim() ?? "";
        if (texte.Length == 0)
            return;
        if (texte.Length > 300)
            texte = texte[..300];

        var canal = (Phase, joueur) switch
        {
            (Phase.Lobby or Phase.Terminee, _) => Canal.Public,
            (_, { EstVivant: false }) => Canal.Morts,
            (Phase.Nuit, { EstLoup: true }) => Canal.Loups,
            (Phase.Nuit, _) => throw new ActionInvalideException("Le village dort : vous ne pouvez pas parler la nuit."),
            _ => Canal.Public,
        };
        Journaliser(canal, texte, joueur.Nom, []);
    }

    /// <summary>À appeler régulièrement : fait avancer la partie quand le temps de la phase est écoulé.</summary>
    /// <returns>true si l'état de la partie a changé.</returns>
    public bool Actualiser()
    {
        if (FinPhase is null || _horloge.GetUtcNow() < FinPhase)
            return false;

        if (Phase == Phase.Nuit)
            PasserEtapeSuivante();
        else if (Phase == Phase.Jour)
            ResoudreJour();
        return true;
    }

    // ---------------------------------------------------------------- Déroulement

    private void CommencerNuit()
    {
        NumeroTour++;
        Phase = Phase.Nuit;
        EtatNuit = new EtatNuit();
        _indexEtape = -1;
        JournaliserPublic($"🌙 Nuit {NumeroTour} : le village s'endort.");
        PasserEtapeSuivante();
    }

    private void PasserEtapeSuivante()
    {
        EtapeCourante?.Terminer(this);

        do _indexEtape++;
        while (_indexEtape < OrdreNuit.Length && !OrdreNuit[_indexEtape].DoitEtreJouee(this));

        if (_indexEtape >= OrdreNuit.Length)
        {
            ResoudreNuit();
            return;
        }

        EtatNuit.ActeursTermines.Clear();
        FinPhase = _horloge.GetUtcNow() + Parametres.DureeEtapeNuit;
        OrdreNuit[_indexEtape].Debuter(this);
    }

    private void ResoudreNuit()
    {
        var morts = new List<Joueur>();
        if (EtatNuit.VictimeLoups is { } victime && victime != EtatNuit.Protege)
        {
            if (EtatNuit.VictimeInfectee)
            {
                victime.Infecter();
                JournaliserPrive(victime, "🐺 Vous avez été infecté par le Loup-Garou Noir : vous rejoignez la meute, tout en gardant votre pouvoir !");
                JournaliserLoups($"{victime.Nom} a été infecté et rejoint la meute.");
            }
            else if (!EtatNuit.VictimeSoignee)
            {
                morts.Add(victime);
            }
        }
        if (EtatNuit.Empoisonne is { } empoisonne && !morts.Contains(empoisonne))
            morts.Add(empoisonne);

        Phase = Phase.Jour;
        _indexEtape = -1;
        JournaliserPublic($"☀️ Jour {NumeroTour} : le village se réveille.");
        if (morts.Count == 0)
            JournaliserPublic("Personne n'est mort cette nuit.");
        foreach (var mort in morts)
            Eliminer(mort, "a été retrouvé mort ce matin");

        if (VerifierVictoire())
            return;

        _votesJour.Clear();
        FinPhase = _horloge.GetUtcNow() + Parametres.DureeJour;
    }

    private void ResoudreJour()
    {
        var elimine = Depouiller(_votesJour, departageAleatoire: false);
        if (elimine is null)
            JournaliserPublic("Le village ne s'est pas mis d'accord : personne n'est éliminé.");
        else
            Eliminer(elimine, "a été éliminé par le village");

        if (!VerifierVictoire())
            CommencerNuit();
    }

    private void Eliminer(Joueur joueur, string cause)
    {
        joueur.EstVivant = false;
        JournaliserPublic($"💀 {joueur.Nom} {cause}. Son rôle : {NomRole(joueur)}.");
    }

    private bool VerifierVictoire()
    {
        var loups = Vivants.Count(j => j.EstLoup);
        var villageois = Vivants.Count(j => !j.EstLoup);
        if (loups > 0 && villageois > 0)
            return false;

        Phase = Phase.Terminee;
        FinPhase = null;
        Vainqueur = loups > 0 ? Camp.LoupsGarous : villageois > 0 ? Camp.Village : null;
        JournaliserPublic(Vainqueur switch
        {
            Camp.Village => "🎉 Tous les loups-garous sont morts : victoire du village !",
            Camp.LoupsGarous => "🐺 Les loups-garous ont dévoré tout le village : victoire des loups !",
            _ => "Plus personne n'est en vie : égalité.",
        });
        JournaliserPublic("Rôles : " + string.Join(", ", _joueurs.Select(j => $"{j.Nom} ({NomRole(j)})")) + ".");
        return true;
    }

    // ---------------------------------------------------------------- Outils

    internal static string NomRole(Joueur joueur) => joueur.Role!.Nom + (joueur.EstInfecte ? " infecté" : "");

    /// <summary>Joueur ayant reçu le plus de voix. En cas d'égalité : tirage au sort, ou personne.</summary>
    internal Joueur? Depouiller(IReadOnlyDictionary<string, string> votes, bool departageAleatoire)
    {
        if (votes.Count == 0)
            return null;

        var decompte = votes.Values.GroupBy(id => id).Select(g => (Id: g.Key, Voix: g.Count())).ToList();
        var maximum = decompte.Max(d => d.Voix);
        var enTete = decompte.Where(d => d.Voix == maximum).ToList();
        if (enTete.Count > 1 && !departageAleatoire)
            return null;
        return TrouverJoueur(enTete[Aleatoire.Next(enTete.Count)].Id);
    }

    internal void JournaliserPublic(string texte) => Journaliser(Canal.Public, texte, null, []);

    internal void JournaliserLoups(string texte) => Journaliser(Canal.Loups, texte, null, []);

    internal void JournaliserPrive(Joueur destinataire, string texte) => Journaliser(Canal.Prive, texte, null, [destinataire.Id]);

    private void Journaliser(Canal canal, string texte, string? auteur, IReadOnlyCollection<string> destinataires)
        => _journal.Add(new EvenementJournal(_journal.Count + 1, _horloge.GetUtcNow(), canal, texte, auteur, destinataires));
}
