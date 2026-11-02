using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LG.Data;

public static class OptionsLg
{
    /// <summary>Réglages communs à l'application et aux outils : PostgreSQL et noms en snake_case.</summary>
    public static T UseLg<T>(this T options, string chaineConnexion) where T : DbContextOptionsBuilder
    {
        options.UseNpgsql(chaineConnexion).UseSnakeCaseNamingConvention();
        return options;
    }
}

/// <summary>Utilisée seulement par `dotnet ef migrations add` : aucune connexion n'est ouverte.</summary>
internal sealed class FabriqueDesignTime : IDesignTimeDbContextFactory<LgDbContext>
{
    public LgDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<LgDbContext>().UseLg("Host=localhost;Database=lg").Options);
}
