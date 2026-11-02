using LG.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LG.Data;

/// <summary>
/// Base PostgreSQL du jeu. Les tables sont en snake_case (comptes, parties, evenements_partie...).
/// IdentityUserContext n'apporte que les tables des comptes : les « rôles » d'Identity (droits d'accès)
/// ne servent pas, et le nom <c>roles</c> est réservé aux rôles du jeu.
/// </summary>
public sealed class LgDbContext(DbContextOptions<LgDbContext> options) : IdentityUserContext<Compte, Guid>(options)
{
    public DbSet<RoleJeu> Roles => Set<RoleJeu>();
    public DbSet<AmbianceJeu> Ambiances => Set<AmbianceJeu>();
    public DbSet<Skin> Skins => Set<Skin>();
    public DbSet<SkinPossede> SkinsPossedes => Set<SkinPossede>();
    public DbSet<PartieArchivee> Parties => Set<PartieArchivee>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<EvenementArchive> Evenements => Set<EvenementArchive>();

    protected override void ConfigureConventions(ModelConfigurationBuilder conventions)
    {
        // Les énumérations sont stockées par leur nom : lisible en SQL, et insensible à l'ordre des valeurs.
        conventions.Properties<RoleId>().HaveConversion<string>().HaveMaxLength(40);
        conventions.Properties<Ambiance>().HaveConversion<string>().HaveMaxLength(40);
        conventions.Properties<Camp>().HaveConversion<string>().HaveMaxLength(20);
        conventions.Properties<Phase>().HaveConversion<string>().HaveMaxLength(20);
        conventions.Properties<Canal>().HaveConversion<string>().HaveMaxLength(20);
        conventions.Properties<StatutPartie>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modele)
    {
        base.OnModelCreating(modele);

        modele.Entity<Compte>(compte =>
        {
            compte.ToTable("comptes");
            compte.HasOne(c => c.Skin).WithMany().OnDelete(DeleteBehavior.SetNull);
        });
        modele.Entity<IdentityUserClaim<Guid>>().ToTable("comptes_claims");
        modele.Entity<IdentityUserLogin<Guid>>().ToTable("comptes_connexions_externes");
        modele.Entity<IdentityUserToken<Guid>>().ToTable("comptes_jetons");

        modele.Entity<RoleJeu>(role =>
        {
            role.ToTable("roles");
            role.Property(r => r.Nom).HasMaxLength(60);
            role.Property(r => r.Description).HasMaxLength(1000);
        });

        modele.Entity<AmbianceJeu>(ambiance =>
        {
            ambiance.ToTable("ambiances");
            ambiance.Property(a => a.Nom).HasMaxLength(60);
        });

        modele.Entity<Skin>(skin =>
        {
            skin.ToTable("skins");
            skin.HasIndex(s => s.Code).IsUnique();
            skin.Property(s => s.Code).HasMaxLength(40);
            skin.Property(s => s.Nom).HasMaxLength(60);
            skin.Property(s => s.Fichier).HasMaxLength(200);
            skin.HasOne(s => s.Ambiance).WithMany().HasForeignKey(s => s.AmbianceId).OnDelete(DeleteBehavior.Restrict);
        });

        modele.Entity<SkinPossede>(possession =>
        {
            possession.ToTable("skins_possedes");
            possession.HasKey(p => new { p.CompteId, p.SkinId });
            possession.HasOne(p => p.Compte).WithMany(c => c.SkinsPossedes).OnDelete(DeleteBehavior.Cascade);
        });

        modele.Entity<PartieArchivee>(partie =>
        {
            partie.ToTable("parties");
            partie.Property(p => p.Code).HasMaxLength(5);
            partie.HasIndex(p => p.Code);
            partie.HasIndex(p => p.Statut);
            // Supprimer un compte ne supprime pas les parties qu'il a hébergées.
            partie.HasOne(p => p.Hote).WithMany().OnDelete(DeleteBehavior.SetNull);
            partie.HasOne(p => p.Ambiance).WithMany().HasForeignKey(p => p.AmbianceId).OnDelete(DeleteBehavior.Restrict);
        });

        modele.Entity<Participant>(participant =>
        {
            participant.ToTable("participants");
            participant.HasKey(p => new { p.PartieId, p.CompteId });
            participant.HasIndex(p => p.CompteId);
            participant.Property(p => p.Pseudo).HasMaxLength(20);
            participant.Property(p => p.RoleRevele).HasMaxLength(100);
            participant.HasOne(p => p.Partie).WithMany(p => p.Participants).OnDelete(DeleteBehavior.Cascade);
            participant.HasOne(p => p.Compte).WithMany().OnDelete(DeleteBehavior.Cascade);
            participant.HasOne(p => p.Role).WithMany().HasForeignKey(p => p.RoleId).OnDelete(DeleteBehavior.Restrict);
            participant.HasOne(p => p.Skin).WithMany().OnDelete(DeleteBehavior.SetNull);
        });

        modele.Entity<EvenementArchive>(evenement =>
        {
            evenement.ToTable("evenements_partie");
            evenement.HasIndex(e => new { e.PartieId, e.Numero }).IsUnique();
            evenement.HasIndex(e => e.AuteurId);
            evenement.Property(e => e.Texte).HasMaxLength(2000);
            evenement.HasOne(e => e.Partie).WithMany(p => p.Evenements).OnDelete(DeleteBehavior.Cascade);
            // Le message d'un compte supprimé reste dans l'historique, sans auteur.
            evenement.HasOne(e => e.Auteur).WithMany().OnDelete(DeleteBehavior.SetNull);
        });
    }
}
