using LG.Domain;
using Microsoft.AspNetCore.Identity;

namespace LG.Data;

/// <summary>
/// Compte d'un joueur. Le pseudo est le UserName d'Identity (unique, sans tenir compte de la casse) ;
/// le mot de passe n'est jamais stocké, seulement son hachage.
/// </summary>
public sealed class Compte : IdentityUser<Guid>
{
    public DateTimeOffset InscritLe { get; set; }

    /// <summary>Personnage choisi par le joueur (null = attribué selon sa place à la table).</summary>
    public int? SkinId { get; set; }
    public Skin? Skin { get; set; }

    public List<SkinPossede> SkinsPossedes { get; set; } = [];
}

/// <summary>
/// Catalogue des rôles, recopié depuis le code au démarrage.
/// L'identifiant est le nom de la valeur de <see cref="RoleId"/> (« Sorciere ») : il ne dépend pas de l'ordre de l'énumération.
/// </summary>
public sealed class RoleJeu
{
    public RoleId Id { get; set; }
    public required string Nom { get; set; }
    public Camp Camp { get; set; }
    public required string Description { get; set; }

    /// <summary>Un rôle inactif reste dans l'historique mais ne peut plus être proposé.</summary>
    public bool Actif { get; set; } = true;
}

/// <summary>Décors du village. L'identifiant est le nom de la valeur de <see cref="Domain.Ambiance"/> (« Hiver »).</summary>
public sealed class AmbianceJeu
{
    public Ambiance Id { get; set; }
    public required string Nom { get; set; }
    public bool Actif { get; set; } = true;
}

/// <summary>Apparence d'un personnage (un fichier SVG du front).</summary>
public sealed class Skin
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Nom { get; set; }

    /// <summary>Chemin servi par le front, par exemple /personnages/aldric.svg.</summary>
    public required string Fichier { get; set; }

    /// <summary>Skin propre à une ambiance (costume d'Halloween...) ; null = toutes les ambiances.</summary>
    public Ambiance? AmbianceId { get; set; }
    public AmbianceJeu? Ambiance { get; set; }

    /// <summary>Disponible pour tous ; sinon il faut le posséder.</summary>
    public bool Gratuit { get; set; } = true;
    public bool Actif { get; set; } = true;
}

public sealed class SkinPossede
{
    public Guid CompteId { get; set; }
    public Compte Compte { get; set; } = null!;
    public int SkinId { get; set; }
    public Skin Skin { get; set; } = null!;
    public DateTimeOffset ObtenuLe { get; set; }
}

public enum StatutPartie
{
    Lobby,
    EnCours,
    Terminee,

    /// <summary>Tous les joueurs ont quitté le lobby, ou le serveur s'est arrêté pendant la partie.</summary>
    Abandonnee,
}

/// <summary>Une partie jouée (ou en cours). Le code à 5 lettres peut resservir plus tard : l'identifiant est un Guid.</summary>
public sealed class PartieArchivee
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public Guid? HoteId { get; set; }
    public Compte? Hote { get; set; }
    public Ambiance AmbianceId { get; set; }
    public AmbianceJeu Ambiance { get; set; } = null!;
    public StatutPartie Statut { get; set; }
    public DateTimeOffset CreeeLe { get; set; }
    public DateTimeOffset? CommenceeLe { get; set; }
    public DateTimeOffset? TermineeLe { get; set; }
    public int NombreTours { get; set; }

    /// <summary>Camp gagnant (null = égalité, ou partie pas terminée).</summary>
    public Camp? CampVainqueur { get; set; }

    public List<Participant> Participants { get; set; } = [];
    public List<EvenementArchive> Evenements { get; set; } = [];
}

/// <summary>Un joueur d'une partie lancée : sa place, son personnage, son rôle et son sort.</summary>
public sealed class Participant
{
    public Guid PartieId { get; set; }
    public PartieArchivee Partie { get; set; } = null!;
    public Guid CompteId { get; set; }
    public Compte Compte { get; set; } = null!;

    /// <summary>Pseudo au moment de la partie.</summary>
    public required string Pseudo { get; set; }
    public int Siege { get; set; }
    public int? SkinId { get; set; }
    public Skin? Skin { get; set; }

    /// <summary>Carte reçue au lancement.</summary>
    public RoleId RoleId { get; set; }
    public RoleJeu Role { get; set; } = null!;

    /// <summary>Rôle tel que révélé en fin de partie, par exemple « Héritier (Sorcière) ».</summary>
    public string? RoleRevele { get; set; }
    public Camp? CampFinal { get; set; }
    public bool? Survivant { get; set; }
    public bool? Vainqueur { get; set; }
}

/// <summary>Une ligne du journal de la partie : annonce du narrateur ou message de chat.</summary>
public sealed class EvenementArchive
{
    public long Id { get; set; }
    public Guid PartieId { get; set; }
    public PartieArchivee Partie { get; set; } = null!;

    /// <summary>Numéro dans la partie (1, 2, 3...).</summary>
    public long Numero { get; set; }
    public DateTimeOffset Date { get; set; }
    public int Tour { get; set; }
    public Phase Phase { get; set; }
    public Canal Canal { get; set; }
    public required string Texte { get; set; }

    /// <summary>Auteur d'un message de chat (null pour le narrateur).</summary>
    public Guid? AuteurId { get; set; }
    public Compte? Auteur { get; set; }

    /// <summary>Comptes qui ont reçu un message privé.</summary>
    public List<Guid> Destinataires { get; set; } = [];
}
