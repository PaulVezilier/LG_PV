namespace LG.Domain;

/// <summary>Levée quand un joueur tente une action interdite. Le message est destiné au joueur.</summary>
public sealed class ActionInvalideException(string message) : Exception(message);
