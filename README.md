# Loup-Garou en ligne

Jeu de Loup-Garou multijoueur en temps réel : back-end .NET (C#, ASP.NET Core + SignalR), front React + Vite + TypeScript + Zustand.

## Lancer le projet

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) et [Node.js](https://nodejs.org/) (LTS).

**Pour jouer** (le serveur .NET sert le front compilé) :

```bash
cd src/LG.Client
npm install
npm run build
cd ../..
dotnet run --project src/LG.Web
```

Puis ouvrir http://localhost:5080. `npm run build` écrit le front dans `src/LG.Web/wwwroot` : à refaire après chaque modification du front.

**Pour développer le front** (rechargement à chaud) : lancer le serveur .NET comme ci-dessus, puis dans un second terminal
`npm run dev` depuis `src/LG.Client`, et ouvrir http://localhost:5173. Vite relaie le hub `/hub` vers le serveur .NET.

Pour jouer seul en test, ouvrir plusieurs **onglets** : chaque onglet garde son pseudo et sa session dans le `sessionStorage`,
c'est donc un joueur différent.

Tests :

```bash
dotnet test
```

## Architecture

```
src/
  LG.Domain/            Règles du jeu, sans aucune dépendance web
    Partie.cs           Cœur du jeu : lobby, nuit, jour, votes, victoire, journal
    Joueur.cs
    Roles/              Une classe par rôle (nom, camp, état de son pouvoir)
    Nuit/               Une classe par étape de la nuit (Garde → Voyante → Loups → Loup Noir → Sorcière)
    Vues/VuePartie.cs   Ce qu'un joueur a le droit de voir (seul objet envoyé au navigateur)
  LG.Web/
    PartieHub.cs        Hub SignalR : salon / créer / rejoindre / quitter / agir / chatter
    GestionnaireParties Parties et sessions en mémoire, résumés pour le salon
    Diffuseur.cs        Envoie à chaque joueur sa propre vue, et la liste des parties au salon quand elle change
    HorlogeParties.cs   Fait avancer les phases quand le temps est écoulé
    wwwroot/            Front compilé (généré, ne pas modifier à la main)
  LG.Client/            Front React
    src/hub.ts          Connexion SignalR unique
    src/stores/         Zustand : compte (pseudo) et jeu (session, VuePartie, liste des parties)
    src/pages/          Connexion, Salon, Partie
    src/components/     Lobby (table, décompte, zoom), Village (place du village), Bandeau, Chat, PanneauDroit…
    src/village.ts      Décors, personnages et emplacements tirés des maquettes
    src/styles.css      Charte des maquettes (couleurs en variables CSS en tête de fichier)
    public/             Décors (6 plateaux, 3 tables de lobby) et 16 personnages en SVG
    outils/             importer-decors.mjs : régénère public/ depuis les SVG de la direction artistique
tests/
  LG.Domain.Tests/      Tests xUnit des règles
```

Principes :

- **Le serveur fait autorité.** Le navigateur ne reçoit que sa `VuePartie` (jamais les rôles cachés) et n'envoie que des actions (`Agir(type, cible)`). `Partie` vérifie que l'action est proposée au joueur et que la cible est autorisée avant de l'exécuter.
- **Rôles et étapes de nuit sont séparés.** Un rôle (`Roles/`) porte l'identité et l'état de son pouvoir (potions, dernière protection…). Une étape (`Nuit/`) orchestre le moment où ce pouvoir s'utilise. Une Voyante infectée participe ainsi à la fois à l'étape Voyante et à l'étape Loups.
- **Le domaine est testable sans attendre** : l'heure est injectée (`TimeProvider`) ainsi que le hasard (`Random`).

### Ajouter un rôle

1. Ajouter la valeur dans `RoleId` (`Enumerations.cs`) et la classe dans `Roles/`, puis l'enregistrer dans `FabriqueRoles`.
2. Ses effets hors de la nuit se codent dans sa classe, en redéfinissant les crochets de `Role` : `DebutDeJournee`, `FinDeJournee`, `ApresMessageDuJour`, `ActionsDuJour` / `ExecuterActionDuJour`, `AbsorberMorsure` / `SurvivreMorsure`, `QuandUnJoueurMeurt`, et la dernière volonté (`AnnonceDerniereVolonte`, `ConsigneDerniereVolonte`, `ActionsDerniereVolonte`, `ExecuterDerniereVolonte`). `Partie` ne connaît aucun rôle en particulier.
3. S'il agit la nuit : créer une classe `EtapeXxx : EtapeNuit` et l'insérer au bon endroit de `Partie.OrdreNuit`.
4. Si son effet se résout à l'aube : ajouter l'info dans `EtatNuit` et la traiter dans `Partie.ResoudreNuit`.

## Règles implémentées (choix à valider)

- **Composition** : l'hôte la règle dans la salle d'attente (+/− par rôle). Par défaut, elle est automatique : 1 loup pour 4 joueurs (le premier est Noir dès 2 loups), puis Voyante, Sorcière, Garde, puis Simples Villageois. Minimum 4 joueurs, au moins un vrai loup et un rôle du village.
- **Morts en chaîne** : chaque mort passe par une file (`Partie.TraiterMorts`) qui déclenche le chagrin des amoureux, l'héritage et les dernières volontés. Une phase « Dernières volontés » (30 s) laisse le Chasseur, le Loup Vengeur et le Croque-Mort agir. La victoire n'est vérifiée qu'ensuite.
- **Nuit** : étapes successives de 30 s, sautées si personne ne peut agir. Une étape se termine dès que tous ses acteurs ont joué.
- **Loups** : chaque loup vote ; majorité, égalité départagée au hasard ; aucun vote = aucune victime.
- **Loup-Garou Noir** : une fois par partie, après le vote, peut infecter la victime. L'infecté garde son rôle et son pouvoir mais joue avec les loups. Si la victime est protégée par le Garde, l'infection échoue et elle est quand même consommée.
- **Garde** : peut se protéger lui-même, jamais la même personne deux nuits de suite. Ne protège pas du poison.
- **Sorcière** : voit la victime (sauf si infectée), peut se sauver elle-même, peut utiliser ses deux potions la même nuit.
- **Jour** : 3 min de débat + vote (modifiable). Fin anticipée quand tout le monde a voté. Égalité = personne n'est éliminé.
- **Victoire** : la partie s'arrête quand tous les vivants sont du même camp (village, loups, ou couple mixte).
- **Cupidon** : première nuit, deux clics pour désigner le couple (lui-même compris). Les amoureux connaissent le nom et le rôle (tel qu'ils le croient) l'un de l'autre. Un couple mixte forme le camp « Amoureux ».
- **Chasseur / Loup Vengeur** : à leur mort (nuit, jour, chagrin, poison), ils tirent sur un vivant. Un tir peut tuer un autre Chasseur : les volontés s'enchaînent.
- **Dictateur** : bouton « Coup d'État » pendant le jour, une fois par partie. La cible meurt, le vote est annulé et la nuit tombe.
- **Héritier** : choisit sa cible la première nuit. À sa mort, il **reste Héritier** mais reçoit comme pouvoir le même objet rôle que le défunt (potions, infection déjà utilisée, etc.), et donc son camp. Il s'affiche « Héritier (Sorcière) » pour lui-même et à sa mort ; la Voyante voit « Héritier ». Ni l'infection ni le couple du défunt ne s'héritent. Dans le code, `Joueur.Role` est l'identité et `Joueur.Pouvoir` le rôle dont il utilise les pouvoirs : les étapes de nuit et les crochets passent toujours par `Pouvoir`.
- **Chaperon Rouge** : survit à l'attaque des loups tant qu'un Chasseur est vivant. L'infection du Loup Noir fonctionne quand même sur lui.
- **Croque-Mort** : la nuit, il lit et écrit dans le chat des morts (il n'entend pas ce que disent les morts le jour). À sa mort, il désigne un vivant ; un vivant du camp opposé est tiré au sort ; le village apprend les deux noms, dans un ordre aléatoire.
- **Vétéran** : mordu par les loups (sans Garde, Sorcière ni infection), il survit, le village est prévenu, et il meurt à la fin de la journée (après le vote ou le coup d'État).
- **Loup Bavard** : reçoit un mot chaque matin, à placer dans le chat public avant la fin de la journée (casse, accents et pluriel ignorés). Sinon, il meurt le soir.
- **Loup Perfide** : la Voyante le voit « Simple Villageois ».
- **Loup Amnésique** : se croit Simple Villageois et joue pour le village. Mordu par les loups (sans protection du Garde), il survit et rejoint la meute. La Voyante, elle, voit son vrai rôle.
- **Priorités à l'aube** pour la victime des loups : Garde > réveil de l'Amnésique > infection > potion de vie / Chaperon Rouge > blessure du Vétéran > mort.
- **Chat** : public le jour et dans le lobby, meute la nuit, canal des morts pour les morts. Tout est révélé en fin de partie.

## Interface

- **Connexion** : un pseudo suffit pour l'instant ; le champ mot de passe est désactivé en attendant la base de données.
- **Salon** : liste publique, mise à jour en direct, des parties en attente (rejoignables d'un clic) et en cours. Un lien `/partie/CODE` fait aussi rejoindre directement.
- **Lobby** : une table ronde à 16 places ; chaque joueur y est représenté par un des 16 personnages dessinés (le n-ième arrivé prend le n-ième personnage), l'hôte porte une couronne. Une place libre ou le bouton « Inviter » copie le lien de la partie. Sur la table, « Rôles » ouvre la composition (modifiable par l'hôte) et « Ambiance » fait passer l'hôte de Classique à Halloween puis Hiver (le décor change pour tout le monde).
- **Lancement** : « Lancer la partie » démarre, côté serveur, un décompte de 3 s (phase `Lancement`, `ParametresPartie.DureeLancement`) affiché chez tous les joueurs ; à zéro, la caméra zoome sur le disque central de la table, puis le village apparaît en plein jour et la nuit tombe aussitôt. Les rôles sont distribués à la fin du décompte.
- **Partie** : la place du village (décor jour ou nuit, en fondu ; feux et fumée animés), les joueurs debout avec leur pseudo au-dessus de la tête. Le bandeau dit à tous ce qui se passe (« Le Garde doit protéger quelqu'un ») et à l'acteur ce qu'il doit faire ; il suffit de cliquer sur un villageois (ceux qu'on peut cibler s'illuminent). Le vote du joueur est marqué d'un anneau doré, les voix reçues d'une pastille rouge. Quand plusieurs actions ciblées sont possibles (vote / coup d'État), des boutons sous le village choisissent laquelle.
- **Colonne de droite** : le chat sur toute la hauteur (onglet Village, Meute ou Morts selon le moment). Un second onglet « Mon rôle » donne la description de votre rôle et les rôles en jeu (« Loup-Garou x2 », puis « x1 » quand l'un meurt, grisé à zéro ; le décompte suit la carte reçue au lancement, donc un Héritier ne le fausse pas) ; un point doré signale les messages arrivés pendant ce temps.
- **Décors** : les SVG de la direction artistique contiennent 16 villageois dessinés en dur ; `outils/importer-decors.mjs` les retire (pour y placer les vrais joueurs), anime les flammes, les braises et la fumée, extrait la table du lobby de sa maquette et copie les personnages dans `public/`. À relancer si les SVG changent :
  `node outils/importer-decors.mjs <dossier loup-garou-svg> <Lobby.dc.html>` depuis `src/LG.Client`.

## Limites connues / prochaines étapes

- Les parties ne sont qu'en mémoire (perdues au redémarrage) et les parties terminées ne sont jamais supprimées (elles disparaissent seulement du salon).
- Pas de mode spectateur : une partie en cours est visible dans le salon mais pas rejoignable.
- Un joueur qui ferme son onglet dans la salle d'attente y reste (seul le bouton « Quitter » le retire).
- Sauter l'étape d'un rôle mort raccourcit la nuit, ce qui peut trahir la mort d'un rôle : à remplacer par une fausse attente.
- Pas encore de départ d'un joueur, ni de vote du maire, ni de chat privé entre amoureux.
- Rien n'empêche de mettre deux Cupidons ou deux Dictateurs dans la composition.
- Le minuteur et le décompte du navigateur supposent que son horloge est à l'heure du serveur.
- La maquette « Personnalisation » (choisir peau, coiffure, tenue de son villageois) n'est pas encore branchée : chaque joueur reçoit un des 16 personnages selon son ordre d'arrivée, et un départ dans le lobby décale ceux des suivants.
- Boutons de la maquette du lobby pas encore implémentés : Modes, Événements, Paramètres, « Prêt », partie privée/publique.
- Sur téléphone, le village garde son cadrage paysage (la maquette prévoit un cadrage portrait qui demanderait un décor plus haut).
