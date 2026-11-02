# Loup-Garou en ligne

Jeu de Loup-Garou multijoueur en temps réel : back-end .NET (C#, ASP.NET Core + SignalR), front React + Vite + TypeScript + Zustand.

## Lancer le projet

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), [Node.js](https://nodejs.org/) (LTS)
et [Docker Desktop](https://www.docker.com/products/docker-desktop/) pour la base PostgreSQL.

### Base de données (une seule fois)

```bash
cp .env.exemple .env
```

Remplacer les deux mots de passe dans `.env` (des valeurs longues et aléatoires, par exemple `openssl rand -hex 24`), puis :

```bash
docker compose up -d
dotnet user-secrets set "ConnectionStrings:LG" "Host=localhost;Port=5432;Database=lg;Username=lg_app;Password=<LG_APP_PASSWORD>" --project src/LG.Web
```

Le conteneur crée l'utilisateur `lg_app`, propriétaire de la base `lg` mais sans droit d'administration, et n'écoute que sur `127.0.0.1`.
Au démarrage, le serveur applique les migrations et recopie le catalogue (rôles, ambiances, personnages) : il n'y a rien d'autre à lancer.
La chaîne de connexion n'est jamais dans un fichier versionné : user-secrets en développement, variable d'environnement `ConnectionStrings__LG` en production.

Pour consulter la base : pgAdmin, lancé par le même `docker compose up -d`, sur http://localhost:5050.
Se connecter avec `PGADMIN_EMAIL` / `PGADMIN_PASSWORD` (dans `.env`) ; le serveur « Loup-Garou (dev) » est déjà déclaré,
son mot de passe est `LG_APP_PASSWORD`. Les tables sont sous Schemas › public › Tables (clic droit › View/Edit Data pour les lignes,
clic droit sur la base › ERD For Database pour le schéma).

Après une modification des entités (`src/LG.Data`), créer une migration :

```bash
dotnet ef migrations add <NomDeLaMigration> -p src/LG.Data -s src/LG.Web -o Migrations
```

(`dotnet tool restore` installe l'outil `dotnet ef` déclaré dans `dotnet-tools.json`.)

### Serveur et front

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
`npm run dev` depuis `src/LG.Client`, et ouvrir http://localhost:5173. Vite relaie le hub `/hub` et l'API `/api` vers le serveur .NET.

Pour jouer seul en test, il faut plusieurs comptes connectés en même temps : la session est un cookie, partagé par tous les onglets
d'un même navigateur. Utiliser une fenêtre de navigation privée par joueur, ou plusieurs profils de navigateur.

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
  LG.Data/              Base PostgreSQL (EF Core) : entités, LgDbContext, catalogue, migrations
  LG.Web/
    Program.cs          Configuration : base, comptes (Identity), limites de débit, hub
    Comptes.cs          API /api/compte : inscription, connexion, déconnexion, moi
    PartieHub.cs        Hub SignalR (comptes connectés seulement) : salon / créer / rejoindre / quitter / agir / chatter
    GestionnaireParties Parties en mémoire, compte de chaque joueur, résumés pour le salon
    Diffuseur.cs        Envoie à chaque joueur sa propre vue, et la liste des parties au salon quand elle change
    Archiviste.cs       Enregistre en base chaque partie, ses participants et son journal
    EntetesDeSecurite   En-têtes HTTP (CSP, anti-iframe, nosniff)
    HorlogeParties.cs   Fait avancer les phases quand le temps est écoulé
    wwwroot/            Front compilé (généré, ne pas modifier à la main)
  LG.Client/            Front React
    src/hub.ts          Connexion SignalR unique
    src/stores/         Zustand : compte (utilisateur connecté) et jeu (session, VuePartie, liste des parties)
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
- **La partie vit en mémoire, la base garde l'historique.** Après chaque changement, l'`Archiviste` écrit ce qui est nouveau (partie, participants au lancement et en fin de partie, lignes du journal). Une panne de la base est journalisée sans interrompre le jeu. Au redémarrage, les parties restées ouvertes en base sont marquées « Abandonnee ».

## Base de données

Tables (noms en snake_case) :

| Table | Contenu |
|---|---|
| `comptes` (+ `comptes_claims`, `comptes_connexions_externes`, `comptes_jetons`) | Comptes ASP.NET Core Identity : pseudo (`user_name`), e-mail, hachage du mot de passe, verrouillage, date d'inscription, skin choisi |
| `roles` | Catalogue des rôles, identifiés par leur nom de code (`Sorciere`) : nom, camp, description, actif |
| `ambiances` | Classique, Halloween, Hiver |
| `skins` | Les personnages (code, nom, fichier SVG, ambiance éventuelle, gratuit, actif) |
| `skins_possedes` | Skins débloqués par chaque compte |
| `parties` | Une ligne par partie : code, hôte, ambiance, statut (Lobby, EnCours, Terminee, Abandonnee), dates, nombre de tours, camp vainqueur |
| `participants` | Joueurs d'une partie lancée : siège, skin, carte reçue, rôle révélé (« Héritier (Sorcière) »), camp final, survivant, vainqueur |
| `evenements_partie` | Le journal : numéro, date, tour, phase, canal, texte, auteur, destinataires des messages privés |

Sécurité :

- Mots de passe hachés par Identity (PBKDF2), 10 caractères minimum dont un chiffre ; compte bloqué 5 minutes après 5 échecs.
- Inscription et connexion limitées à 10 essais par minute et par adresse IP.
- Session par cookie `HttpOnly`, `SameSite=Strict` (pas de CSRF possible depuis un autre site), `Secure` hors développement.
- Le message d'échec de connexion ne dit pas si le pseudo existe.
- Requêtes SQL toujours paramétrées (EF Core).


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

- **Connexion** : onglets Connexion (pseudo ou e-mail, mot de passe, « Rester connecté ») et Inscription (pseudo, e-mail, mot de passe). Le pseudo du compte est celui affiché en jeu.
- **Salon** : liste publique, mise à jour en direct, des parties en attente (rejoignables d'un clic) et en cours. Un lien `/partie/CODE` fait aussi rejoindre directement.
- **Lobby** : une table ronde à 16 places ; chaque joueur y est représenté par un des 16 personnages dessinés (le n-ième arrivé prend le n-ième personnage), l'hôte porte une couronne. Une place libre ou le bouton « Inviter » copie le lien de la partie. Sur la table, « Rôles » ouvre la composition (modifiable par l'hôte) et « Ambiance » fait passer l'hôte de Classique à Halloween puis Hiver (le décor change pour tout le monde).
- **Lancement** : « Lancer la partie » démarre, côté serveur, un décompte de 3 s (phase `Lancement`, `ParametresPartie.DureeLancement`) affiché chez tous les joueurs ; à zéro, la caméra zoome sur le disque central de la table, puis le village apparaît en plein jour et la nuit tombe aussitôt. Les rôles sont distribués à la fin du décompte.
- **Partie** : la place du village (décor jour ou nuit, en fondu ; feux et fumée animés), les joueurs debout avec leur pseudo au-dessus de la tête. Le bandeau dit à tous ce qui se passe (« Le Garde doit protéger quelqu'un ») et à l'acteur ce qu'il doit faire ; il suffit de cliquer sur un villageois (ceux qu'on peut cibler s'illuminent). Le vote du joueur est marqué d'un anneau doré, les voix reçues d'une pastille rouge. Quand plusieurs actions ciblées sont possibles (vote / coup d'État), des boutons sous le village choisissent laquelle.
- **Colonne de droite** : le chat sur toute la hauteur (onglet Village, Meute ou Morts selon le moment). Un second onglet « Mon rôle » donne la description de votre rôle et les rôles en jeu (« Loup-Garou x2 », puis « x1 » quand l'un meurt, grisé à zéro ; le décompte suit la carte reçue au lancement, donc un Héritier ne le fausse pas) ; un point doré signale les messages arrivés pendant ce temps.
- **Décors** : les SVG de la direction artistique contiennent 16 villageois dessinés en dur ; `outils/importer-decors.mjs` les retire (pour y placer les vrais joueurs), anime les flammes, les braises et la fumée, extrait la table du lobby de sa maquette et copie les personnages dans `public/`. À relancer si les SVG changent :
  `node outils/importer-decors.mjs <dossier loup-garou-svg> <Lobby.dc.html>` depuis `src/LG.Client`.

## Limites connues / prochaines étapes

- Les parties en cours ne sont qu'en mémoire : un redémarrage les interrompt (elles restent dans l'historique, statut « Abandonnee »). Les parties terminées ne sont jamais retirées de la mémoire (elles disparaissent seulement du salon).
- Mise en ligne à prévoir : HTTPS (Caddy devant l'application, certificat Let's Encrypt automatique), en-tête HSTS, et `ForwardedHeaders` pour que la limite de débit voie la vraie adresse IP des joueurs.
- Comptes : pas encore de confirmation d'e-mail ni de mot de passe oublié (il faut un service d'envoi d'e-mails), ni de suppression de compte depuis le site.
- RGPD : le journal conserve les messages de chat, y compris ceux de la meute et des morts. Durée de conservation à fixer, puis purge automatique.
- Les tables `skins`, `skins_possedes` et `comptes.skin_id` existent mais ne sont pas encore utilisées par le jeu.
- Pas de mode spectateur : une partie en cours est visible dans le salon mais pas rejoignable.
- Un joueur qui ferme son onglet dans la salle d'attente y reste (seul le bouton « Quitter » le retire).
- Sauter l'étape d'un rôle mort raccourcit la nuit, ce qui peut trahir la mort d'un rôle : à remplacer par une fausse attente.
- Pas encore de départ d'un joueur, ni de vote du maire, ni de chat privé entre amoureux.
- Rien n'empêche de mettre deux Cupidons ou deux Dictateurs dans la composition.
- Le minuteur et le décompte du navigateur supposent que son horloge est à l'heure du serveur.
- La maquette « Personnalisation » (choisir peau, coiffure, tenue de son villageois) n'est pas encore branchée : chaque joueur reçoit un des 16 personnages selon son ordre d'arrivée, et un départ dans le lobby décale ceux des suivants.
- Boutons de la maquette du lobby pas encore implémentés : Modes, Événements, Paramètres, « Prêt », partie privée/publique.
- Sur téléphone, le village garde son cadrage paysage (la maquette prévoit un cadrage portrait qui demanderait un décor plus haut).
