# Loup-Garou en ligne

Jeu de Loup-Garou multijoueur en temps réel : back-end .NET (C#, ASP.NET Core + SignalR), front HTML/CSS/JS sans framework.

## Lancer le projet

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/LG.Web
```

Puis ouvrir http://localhost:5080. Pour jouer seul en test, ouvrir plusieurs fenêtres **en navigation privée**
(chaque fenêtre garde sa session dans le `localStorage`, donc deux onglets normaux partageraient le même joueur).

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
    PartieHub.cs        Hub SignalR : créer / rejoindre / agir / chatter
    GestionnaireParties Parties et sessions en mémoire
    Diffuseur.cs        Envoie à chaque joueur sa propre vue après chaque changement
    HorlogeParties.cs   Fait avancer les phases quand le temps est écoulé
    wwwroot/            Front provisoire (placeholders)
tests/
  LG.Domain.Tests/      Tests xUnit des règles
```

Principes :

- **Le serveur fait autorité.** Le navigateur ne reçoit que sa `VuePartie` (jamais les rôles cachés) et n'envoie que des actions (`Agir(type, cible)`). `Partie` vérifie que l'action est proposée au joueur et que la cible est autorisée avant de l'exécuter.
- **Rôles et étapes de nuit sont séparés.** Un rôle (`Roles/`) porte l'identité et l'état de son pouvoir (potions, dernière protection…). Une étape (`Nuit/`) orchestre le moment où ce pouvoir s'utilise. Une Voyante infectée participe ainsi à la fois à l'étape Voyante et à l'étape Loups.
- **Le domaine est testable sans attendre** : l'heure est injectée (`TimeProvider`) ainsi que le hasard (`Random`).

### Ajouter un rôle

1. Ajouter la valeur dans `RoleId` (`Enumerations.cs`) et la classe dans `Roles/`, puis l'enregistrer dans `FabriqueRoles`.
2. S'il agit la nuit : créer une classe `EtapeXxx : EtapeNuit` et l'insérer au bon endroit de `Partie.OrdreNuit`.
3. Si son effet se résout à l'aube : ajouter l'info dans `EtatNuit` et la traiter dans `Partie.ResoudreNuit`.

## Règles implémentées (choix à valider)

- **Composition automatique** : 1 loup pour 4 joueurs (le premier est Noir dès 2 loups), puis Voyante, Sorcière, Garde, puis Simples Villageois. Minimum 4 joueurs.
- **Nuit** : étapes successives de 30 s, sautées si personne ne peut agir. Une étape se termine dès que tous ses acteurs ont joué.
- **Loups** : chaque loup vote ; majorité, égalité départagée au hasard ; aucun vote = aucune victime.
- **Loup-Garou Noir** : une fois par partie, après le vote, peut infecter la victime. L'infecté garde son rôle et son pouvoir mais joue avec les loups. Si la victime est protégée par le Garde, l'infection échoue et elle est quand même consommée.
- **Garde** : peut se protéger lui-même, jamais la même personne deux nuits de suite. Ne protège pas du poison.
- **Sorcière** : voit la victime (sauf si infectée), peut se sauver elle-même, peut utiliser ses deux potions la même nuit.
- **Jour** : 3 min de débat + vote (modifiable). Fin anticipée quand tout le monde a voté. Égalité = personne n'est éliminé.
- **Victoire** : le village gagne quand il n'y a plus de loups ; les loups gagnent quand il n'y a plus de villageois.
- **Chat** : public le jour et dans le lobby, meute la nuit, canal des morts pour les morts. Tout est révélé en fin de partie.

## Limites connues / prochaines étapes

- Les parties ne sont qu'en mémoire (perdues au redémarrage) et jamais nettoyées.
- Sauter l'étape d'un rôle mort raccourcit la nuit, ce qui peut trahir la mort d'un rôle : à remplacer par une fausse attente.
- Pas encore de choix de composition par l'hôte, ni de départ d'un joueur, ni de vote du maire.
- Le minuteur du navigateur suppose que son horloge est à l'heure du serveur.
