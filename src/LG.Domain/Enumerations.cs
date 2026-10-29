namespace LG.Domain;

/// <summary>Camp auquel appartient un joueur, qui détermine sa condition de victoire.</summary>
public enum Camp
{
    Village,
    LoupsGarous,
}

/// <summary>Identifiant de chaque rôle existant (utilisé pour les compositions de partie).</summary>
public enum RoleId
{
    SimpleVillageois,
    LoupGarou,
    LoupGarouNoir,
    Sorciere,
    Voyante,
    Garde,
}

/// <summary>Grandes phases d'une partie.</summary>
public enum Phase
{
    Lobby,
    Nuit,
    Jour,
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
