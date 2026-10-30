using LG.Domain.Nuit;
using LG.Domain.Roles;

namespace LG.Domain;

/// <summary>
/// Une partie de Loup-Garou : Lobby → (Nuit ⇄ Jour)* → Terminée, avec des pauses « Dernières volontés »
/// quand un mort a encore un pouvoir à utiliser.
/// Cette classe n'est pas thread-safe : l'appelant doit sérialiser les accès (lock).
/// </summary>
public sealed class Partie
{
    /// <summary>Ordre de réveil pendant la nuit. Ajouter un rôle de nuit = ajouter une étape ici.</summary>
    private static readonly EtapeNuit[] OrdreNuit =
    [
        new EtapeCupidon(),
        new EtapeHeritier(),
        new EtapeGarde(),
        new EtapeVoyante(),
        new EtapeLoupsGarous(),
        new EtapeLoupNoir(),
        new EtapeSorciere(),
    ];

    private readonly List<Joueur> _joueurs = [];
    private readonly List<EvenementJournal> _journal = [];
    private readonly Dictionary<string, string> _votesJour = [];
    private readonly Queue<(Joueur Joueur, string Cause)> _morts = new();
    private readonly List<DerniereVolonte> _volontes = [];
    private readonly TimeProvider _horloge;
    private int _indexEtape = -1;

    /// <summary>Ce qui reprend une fois les morts traitées et les dernières volontés exprimées.</summary>
    private Action? _suite;

    public Partie(string id, ParametresPartie? parametres = null, TimeProvider? horloge = null, Random? aleatoire = null)
    {
        Id = id;
        Parametres = parametres ?? new ParametresPartie();
        CompositionChoisie = Parametres.Composition;
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

    /// <summary>Composition imposée par l'hôte (null = automatique).</summary>
    public IReadOnlyList<RoleId>? CompositionChoisie { get; private set; }
    public IReadOnlyList<RoleId> CompositionPrevue => CompositionChoisie ?? CompositionRoles.Automatique(_joueurs.Count);

    internal Random Aleatoire { get; }
    internal EtatNuit EtatNuit { get; private set; } = new();

    internal EtapeNuit? EtapeCourante =>
        Phase == Phase.Nuit && _indexEtape >= 0 && _indexEtape < OrdreNuit.Length ? OrdreNuit[_indexEtape] : null;

    internal DerniereVolonte? VolonteDe(Joueur joueur) => _volontes.FirstOrDefault(v => v.Joueur == joueur);

    internal IEnumerable<Joueur> JoueursAvecVolonte => _volontes.Select(v => v.Joueur);

    public Joueur TrouverJoueur(string id)
        => _joueurs.FirstOrDefault(j => j.Id == id) ?? throw new ActionInvalideException("Joueur introuvable.");

    /// <summary>Camp pour la victoire : un couple aux camps opposés joue pour lui-même.</summary>
    public static Camp CampDeVictoire(Joueur joueur)
        => joueur.Amoureux is { EstVivant: true } amoureux && amoureux.Camp != joueur.Camp ? Camp.Amoureux : joueur.Camp;

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

    /// <summary>Départ d'un joueur avant le lancement. Si l'hôte part, le joueur suivant devient hôte.</summary>
    public void RetirerJoueur(string joueurId)
    {
        if (Phase != Phase.Lobby)
            throw new ActionInvalideException("On ne peut plus quitter une partie commencée.");
        var joueur = TrouverJoueur(joueurId);
        _joueurs.Remove(joueur);
        if (HoteId == joueur.Id)
            HoteId = _joueurs.FirstOrDefault()?.Id;
        JournaliserPublic($"{joueur.Nom} a quitté la partie.");
    }

    /// <param name="roles">Un élément par joueur ; null pour revenir à la composition automatique.</param>
    public void DefinirComposition(string demandeurId, IReadOnlyList<RoleId>? roles)
    {
        if (Phase != Phase.Lobby)
            throw new ActionInvalideException("La partie a déjà commencé.");
        if (demandeurId != HoteId)
            throw new ActionInvalideException("Seul l'hôte peut choisir la composition.");
        if (roles is not null && (roles.Count > Parametres.JoueursMaximum || roles.Any(r => !Enum.IsDefined(r))))
            throw new ActionInvalideException("Composition invalide.");
        CompositionChoisie = roles?.ToList();
    }

    /// <summary>Pourquoi la partie ne peut pas encore être lancée (null si elle le peut).</summary>
    public string? ObstacleAuDemarrage()
    {
        var composition = CompositionPrevue;
        if (_joueurs.Count < Parametres.JoueursMinimum)
            return $"Il faut au moins {Parametres.JoueursMinimum} joueurs.";
        if (composition.Count != _joueurs.Count)
            return $"La composition compte {composition.Count} rôle(s) pour {_joueurs.Count} joueur(s).";
        var camps = composition.Select(r => FabriqueRoles.Creer(r).Camp).ToList();
        if (!camps.Contains(Camp.LoupsGarous))
            return "Il faut au moins un loup-garou (le Loup Amnésique ne compte pas).";
        if (!camps.Contains(Camp.Village))
            return "Il faut au moins un rôle du village.";
        return null;
    }

    public void Demarrer(string demandeurId)
    {
        if (Phase != Phase.Lobby)
            throw new ActionInvalideException("La partie a déjà commencé.");
        if (demandeurId != HoteId)
            throw new ActionInvalideException("Seul l'hôte peut lancer la partie.");
        if (ObstacleAuDemarrage() is { } obstacle)
            throw new ActionInvalideException(obstacle);

        var roles = CompositionPrevue.Select(FabriqueRoles.Creer).ToArray();
        Aleatoire.Shuffle(roles);
        for (var i = 0; i < roles.Length; i++)
        {
            _joueurs[i].Role = roles[i];
            _joueurs[i].RoleInitial = roles[i].Id;
        }

        var resume = roles.GroupBy(r => r.Nom).Select(g => g.Count() > 1 ? $"{g.Count()} × {g.Key}" : g.Key);
        JournaliserPublic($"La partie commence ! Composition : {string.Join(", ", resume)}.");
        foreach (var joueur in _joueurs)
            JournaliserPrive(joueur, $"Vous êtes {joueur.Role!.NomPourSoi}. {joueur.Role.DescriptionPourSoi}");
        JournaliserLoups($"Votre meute : {string.Join(", ", _joueurs.Where(j => j.EstLoup))}.");

        CommencerNuit();
    }

    // ---------------------------------------------------------------- Actions des joueurs

    public IReadOnlyList<ActionDisponible> ActionsDisponibles(string joueurId) => ActionsDisponibles(TrouverJoueur(joueurId));

    private IReadOnlyList<ActionDisponible> ActionsDisponibles(Joueur joueur)
    {
        if (Phase == Phase.DernieresVolontes)
            return VolonteDe(joueur) is { } volonte ? volonte.Actions(this) : [];
        if (!joueur.EstVivant)
            return [];

        switch (Phase)
        {
            case Phase.Nuit when EtapeCourante is { } etape && etape.Acteurs(this).Contains(joueur):
                return etape.ActionsDisponibles(this, joueur);

            case Phase.Jour:
                var autres = Vivants.Where(j => j != joueur).ToList();
                var actions = new List<ActionDisponible> { ActionDisponible.AvecCible(ActionType.Voter, "Voter contre", autres) };
                if (joueur.Role is Dictateur { CoupDisponible: true })
                    actions.Add(ActionDisponible.AvecCible(ActionType.CoupDEtat, "Coup d'État", autres));
                return actions;

            default:
                return [];
        }
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

        switch (Phase)
        {
            case Phase.Nuit:
                var etape = EtapeCourante!;
                etape.Executer(this, joueur, type, cible);
                if (etape.EstTerminee(this))
                    PasserEtapeSuivante();
                break;

            case Phase.Jour when type == ActionType.CoupDEtat:
                ((Dictateur)joueur.Role!).UtiliserCoup();
                JournaliserPublic($"⚔️ Coup d'État ! {joueur.Nom} prend le contrôle du village et fait exécuter {cible!.Nom}.");
                Tuer(cible, "a été exécuté par le Dictateur");
                Resoudre(FinirJournee);
                break;

            case Phase.Jour:
                _votesJour[joueur.Id] = cible!.Id;
                JournaliserPublic($"{joueur.Nom} vote contre {cible.Nom}.");
                if (Vivants.All(j => _votesJour.ContainsKey(j.Id)))
                    ResoudreJour();
                break;

            case Phase.DernieresVolontes:
                ExecuterDerniereVolonte(joueur, type, cible);
                break;
        }
    }

    /// <summary>Canal dans lequel ce joueur parle en ce moment (null = il doit se taire).</summary>
    public Canal? CanalDeDiscussion(string joueurId) => CanalDeDiscussion(TrouverJoueur(joueurId));

    private Canal? CanalDeDiscussion(Joueur joueur) => (Phase, joueur) switch
    {
        (Phase.Lobby or Phase.Terminee, _) => Canal.Public,
        (_, { EstVivant: false }) => Canal.Morts,
        (Phase.Nuit, { EstLoup: true }) => Canal.Loups,
        (Phase.Nuit, { Role: CroqueMort }) => Canal.Morts,
        (Phase.Nuit, _) => null,
        _ => Canal.Public,
    };

    public void EnvoyerMessage(string joueurId, string texte)
    {
        var joueur = TrouverJoueur(joueurId);
        texte = texte?.Trim() ?? "";
        if (texte.Length == 0)
            return;
        if (texte.Length > 300)
            texte = texte[..300];

        var canal = CanalDeDiscussion(joueur)
            ?? throw new ActionInvalideException("Le village dort : vous ne pouvez pas parler la nuit.");
        Journaliser(canal, texte, joueur.Nom, []);

        if (canal == Canal.Public && Phase == Phase.Jour && joueur.Role is LoupBavard bavard && bavard.EssayerDePlacer(texte))
            JournaliserPrive(joueur, "✅ Mot placé : vous êtes sauf pour aujourd'hui.");
    }

    /// <summary>À appeler régulièrement : fait avancer la partie quand le temps de la phase est écoulé.</summary>
    /// <returns>true si l'état de la partie a changé.</returns>
    public bool Actualiser()
    {
        if (FinPhase is null || _horloge.GetUtcNow() < FinPhase)
            return false;

        switch (Phase)
        {
            case Phase.Nuit:
                PasserEtapeSuivante();
                break;
            case Phase.Jour:
                ResoudreJour();
                break;
            case Phase.DernieresVolontes:
                foreach (var volonte in _volontes)
                    JournaliserPublic($"{volonte.Joueur.Nom} n'a pas eu le temps d'exprimer sa dernière volonté.");
                _volontes.Clear();
                Continuer();
                break;
        }
        return true;
    }

    // ---------------------------------------------------------------- Nuit

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
        _indexEtape = -1;
        JournaliserPublic($"☀️ Jour {NumeroTour} : le village se réveille.");

        if (EtatNuit.VictimeLoups is { } victime && victime != EtatNuit.Protege)
            SubirAttaqueDesLoups(victime);
        if (EtatNuit.Empoisonne is { } empoisonne)
            Tuer(empoisonne, "a été retrouvé mort ce matin");
        if (_morts.Count == 0)
            JournaliserPublic("Personne n'est mort cette nuit.");

        Resoudre(DebuterJour);
    }

    /// <summary>Victime des loups non protégée par le Garde : dans l'ordre, ce qui peut encore la sauver.</summary>
    private void SubirAttaqueDesLoups(Joueur victime)
    {
        if (victime.Role is LoupAmnesique { Reveille: false } amnesique)
        {
            amnesique.Reveiller();
            JournaliserPrive(victime, "🐺 La morsure des loups vous rend la mémoire : vous êtes un Loup Amnésique ! Vous survivez et rejoignez la meute.");
            JournaliserLoups($"{victime.Nom} était un Loup Amnésique : la morsure l'a réveillé, il rejoint la meute !");
        }
        else if (EtatNuit.VictimeInfectee)
        {
            victime.Infecter();
            JournaliserPrive(victime, "🐺 Vous avez été infecté par le Loup-Garou Noir : vous rejoignez la meute, tout en gardant votre pouvoir !");
            JournaliserLoups($"{victime.Nom} a été infecté et rejoint la meute.");
        }
        else if (EtatNuit.VictimeSoignee || victime.Role is ChaperonRouge && Vivants.Any(j => j.Role is Chasseur))
        {
            // Sauvé par la Sorcière, ou Chaperon Rouge protégé par la présence du Chasseur.
        }
        else if (victime.Role is Veteran && !victime.EstBlesse)
        {
            victime.Blesser();
            JournaliserPublic($"🩸 {victime.Nom} a été attaqué par les loups mais a survécu ! Gravement blessé, il succombera à ses blessures ce soir.");
        }
        else
        {
            Tuer(victime, "a été retrouvé mort ce matin");
        }
    }

    // ---------------------------------------------------------------- Jour

    private void DebuterJour()
    {
        Phase = Phase.Jour;
        _votesJour.Clear();
        FinPhase = _horloge.GetUtcNow() + Parametres.DureeJour;

        foreach (var joueur in Vivants.Where(j => j.Role is LoupBavard))
        {
            var bavard = (LoupBavard)joueur.Role!;
            bavard.TirerMot(Aleatoire);
            JournaliserPrive(joueur, $"🗣️ Votre mot du jour : « {bavard.MotImpose} ». Placez-le dans le chat du village avant ce soir, sinon vous mourrez.");
        }
    }

    private void ResoudreJour()
    {
        var elimine = Depouiller(_votesJour, departageAleatoire: false);
        if (elimine is null)
            JournaliserPublic("Le village ne s'est pas mis d'accord : personne n'est éliminé.");
        else
            Tuer(elimine, "a été éliminé par le village");

        Resoudre(FinirJournee);
    }

    /// <summary>Après le vote ou le coup d'État : les morts « de fin de journée », puis la nuit.</summary>
    private void FinirJournee()
    {
        foreach (var blesse in Vivants.Where(j => j.EstBlesse))
            Tuer(blesse, "a succombé à ses blessures");
        foreach (var bavard in Vivants.Where(j => j.Role is LoupBavard { MotImpose: not null, MotPlace: false }))
            Tuer(bavard, "n'a pas réussi à placer son mot et s'est étranglé de rage");

        Resoudre(CommencerNuit);
    }

    // ---------------------------------------------------------------- Morts et dernières volontés

    /// <summary>Les morts sont mises en file puis traitées par <see cref="Resoudre"/>, qui gère les réactions en chaîne.</summary>
    private void Tuer(Joueur joueur, string cause) => _morts.Enqueue((joueur, cause));

    private void Resoudre(Action suite)
    {
        _suite = suite;
        Continuer();
    }

    private void Continuer()
    {
        TraiterMorts();
        if (_volontes.Count > 0)
        {
            Phase = Phase.DernieresVolontes;
            FinPhase = _horloge.GetUtcNow() + Parametres.DureeDerniereVolonte;
            return;
        }
        if (VerifierVictoire())
            return;

        var suite = _suite!;
        _suite = null;
        suite();
    }

    private void TraiterMorts()
    {
        while (_morts.TryDequeue(out var mort))
        {
            var (joueur, cause) = mort;
            if (!joueur.EstVivant)
                continue;

            joueur.EstVivant = false;
            JournaliserPublic($"💀 {joueur.Nom} {cause}. Son rôle : {NomRole(joueur)}.");

            if (joueur.Amoureux is { EstVivant: true } amoureux)
                Tuer(amoureux, $"meurt de chagrin, inconsolable après la perte de {joueur.Nom}");

            foreach (var heritier in Vivants.Where(h => h.Role is Heritier role && role.CibleId == joueur.Id).ToList())
                Heriter(heritier, joueur);

            if (joueur.Role!.DerniereVolonte is { } type)
            {
                _volontes.Add(new DerniereVolonte(joueur, type));
                JournaliserPrive(joueur, type == ActionType.Tirer
                    ? "Vous êtes mort, mais il vous reste une balle : choisissez sur qui tirer."
                    : "Avant de partir, désignez un joueur : le village saura qu'entre lui et un autre joueur, il y a un loup-garou.");
            }
        }
    }

    /// <summary>L'Héritier reçoit le même objet rôle que le défunt, avec tout son état (potions, etc.).</summary>
    private void Heriter(Joueur heritier, Joueur defunt)
    {
        var role = defunt.Role!;
        heritier.Role = role;
        JournaliserPrive(heritier, $"📜 {defunt.Nom} est mort : vous héritez de son rôle. Vous êtes désormais {role.NomPourSoi}. {role.DescriptionPourSoi}");
        if (heritier.EstLoup)
            JournaliserLoups($"{heritier.Nom} a hérité du rôle de {defunt.Nom} et rejoint la meute.");
    }

    private void ExecuterDerniereVolonte(Joueur joueur, ActionType type, Joueur? cible)
    {
        _volontes.RemoveAll(v => v.Joueur == joueur);
        switch (type)
        {
            case ActionType.Tirer:
                JournaliserPublic($"🎯 Dans un dernier souffle, {joueur.Nom} tire sur {cible!.Nom} !");
                Tuer(cible, "a été abattu");
                break;
            case ActionType.Designer:
                RevelerParCroqueMort(joueur, cible!);
                break;
            default:
                JournaliserPublic($"{joueur.Nom} emporte son secret dans la tombe.");
                break;
        }

        if (_volontes.Count == 0)
            Continuer();
    }

    /// <summary>Le désigné + un joueur tiré au sort dans le camp opposé : exactement un des deux est un loup.</summary>
    private void RevelerParCroqueMort(Joueur croqueMort, Joueur designe)
    {
        var candidats = Vivants.Where(j => j != designe && j.EstLoup != designe.EstLoup).ToList();
        if (candidats.Count == 0)
        {
            JournaliserPublic($"⚰️ Le Croque-Mort {croqueMort.Nom} n'a rien pu découvrir.");
            return;
        }

        var autre = candidats[Aleatoire.Next(candidats.Count)];
        var (premier, second) = Aleatoire.Next(2) == 0 ? (designe, autre) : (autre, designe);
        JournaliserPublic($"⚰️ Le Croque-Mort {croqueMort.Nom} a parlé : entre {premier.Nom} et {second.Nom}, il y a un loup-garou.");
    }

    private bool VerifierVictoire()
    {
        var camps = Vivants.Select(CampDeVictoire).Distinct().ToList();
        if (camps.Count > 1)
            return false;

        Phase = Phase.Terminee;
        FinPhase = null;
        _volontes.Clear();
        Vainqueur = camps.Count == 1 ? camps[0] : null;
        JournaliserPublic(Vainqueur switch
        {
            Camp.Village => "🎉 Tous les loups-garous sont morts : victoire du village !",
            Camp.LoupsGarous => "🐺 Les loups-garous ont dévoré tout le village : victoire des loups !",
            Camp.Amoureux => "💘 Les amoureux sont les derniers survivants : victoire du couple !",
            _ => "Plus personne n'est en vie : égalité.",
        });
        JournaliserPublic("Rôles : " + string.Join(", ", _joueurs.Select(j => $"{j.Nom} ({NomRole(j)})")) + ".");
        return true;
    }

    // ---------------------------------------------------------------- Outils

    /// <summary>Vrai rôle, tel que révélé à la mort ou en fin de partie.</summary>
    internal static string NomRole(Joueur joueur) => joueur.Role!.Nom + (joueur.EstInfecte ? " infecté" : "");

    /// <summary>Rôle tel que le joueur le connaît lui-même.</summary>
    internal static string NomRolePourSoi(Joueur joueur) => joueur.Role!.NomPourSoi + (joueur.EstInfecte ? " infecté" : "");

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
        => _journal.Add(new EvenementJournal(_journal.Count + 1, _horloge.GetUtcNow(), canal, texte, auteur, destinataires, Phase == Phase.Nuit));
}
