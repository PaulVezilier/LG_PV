namespace LG.Domain;

/// <summary>Camp auquel appartient un joueur, qui détermine sa condition de victoire.</summary>
public enum Camp
{
    Village,
    LoupsGarous,

    /// <summary>Couple formé par Cupidon entre deux camps opposés : il gagne s'il est le dernier en vie.</summary>
    Amoureux,
}

/// <summary>Identifiant de chaque rôle existant (utilisé pour les compositions de partie). L'ordre sert à l'affichage.</summary>
public enum RoleId
{
    SimpleVillageois,
    Voyante,
    Sorciere,
    Garde,
    Chasseur,
    Cupidon,
    Dictateur,
    Heritier,
    ChaperonRouge,
    CroqueMort,
    Veteran,
    LoupGarou,
    LoupGarouNoir,
    LoupBavard,
    LoupVengeur,
    LoupPerfide,
    LoupAmnesique,
}

/// <summary>Grandes phases d'une partie.</summary>
public enum Phase
{
    Lobby,
    Nuit,
    Jour,

    /// <summary>Des joueurs qui viennent de mourir utilisent leur pouvoir (Chasseur, Croque-Mort...).</summary>
    DernieresVolontes,

    Terminee,
}

/// <summary>Types d'actions qu'un joueur peut effectuer.</summary>
public enum ActionType
{
    Voter,
    Proteger,
    Sonder,
    Infecter,
    Soigner,
    Empoisonner,
    Unir,
    Heriter,
    CoupDEtat,
    Tirer,
    Designer,
    Passer,
}

/// <summary>Qui peut voir un événement du journal / un message de chat.</summary>
public enum Canal
{
    Public,
    Loups,
    Morts,
    Prive,
}
