using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LG.Domain.Roles;

public sealed class LoupBavard : LoupGarou
{
    private static readonly string[] Mots =
    [
        "parapluie", "citrouille", "girafe", "trampoline", "moustache", "pamplemousse", "aspirateur", "kangourou",
        "chaussette", "volcan", "trompette", "ananas", "escargot", "boussole", "papillon", "crocodile",
        "cerise", "lampadaire", "pingouin", "tournevis", "fromage", "hamac", "igloo", "jongleur",
        "licorne", "marmite", "orchestre", "perroquet", "raquette", "sardine", "tambour", "violon",
        "brouette", "cactus", "dinosaure", "fusée", "grenouille", "hibou", "spaghetti", "bretelles",
    ];

    public override RoleId Id => RoleId.LoupBavard;
    public override string Nom => "Loup-Garou Bavard";
    public override string Description => "Vous chassez avec la meute. Chaque matin, vous recevez un mot à placer dans le chat du village avant la fin de la journée. Sinon, vous mourez.";

    public string? MotImpose { get; private set; }
    public bool MotPlace { get; private set; }

    internal void TirerMot(Random aleatoire)
    {
        MotImpose = Mots[aleatoire.Next(Mots.Length)];
        MotPlace = false;
    }

    /// <summary>Vrai si ce message place le mot pour la première fois (casse, accents et pluriel ignorés).</summary>
    internal bool EssayerDePlacer(string message)
    {
        if (MotImpose is null || MotPlace)
            return false;
        MotPlace = Regex.IsMatch(Normaliser(message), $@"\b{Regex.Escape(Normaliser(MotImpose))}[sx]?\b");
        return MotPlace;
    }

    internal override void DebutDeJournee(Partie partie, Joueur moi)
    {
        TirerMot(partie.Aleatoire);
        partie.JournaliserPrive(moi, $"🗣️ Votre mot du jour : « {MotImpose} ». Placez-le dans le chat du village avant ce soir, sinon vous mourrez.");
    }

    internal override void ApresMessageDuJour(Partie partie, Joueur moi, string texte)
    {
        if (EssayerDePlacer(texte))
            partie.JournaliserPrive(moi, "✅ Mot placé : vous êtes sauf pour aujourd'hui.");
    }

    internal override void FinDeJournee(Partie partie, Joueur moi)
    {
        if (MotImpose is not null && !MotPlace)
            partie.Tuer(moi, "n'a pas réussi à placer son mot et s'est étranglé de rage");
    }

    private static string Normaliser(string texte)
    {
        var decompose = texte.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decompose.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
