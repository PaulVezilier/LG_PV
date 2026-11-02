using LG.Domain.Roles;
using LG.Domain.Vues;

namespace LG.Domain.Tests;

public class NouveauxRolesTests
{
    // ---------------------------------------------------------------- Cupidon

    [Fact]
    public void Les_amoureux_meurent_ensemble()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Cupidon, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (a, b) = (t.Tous<SimpleVillageois>()[0], t.Tous<SimpleVillageois>()[1]);

        t.Agir(t.Un<Cupidon>(), ActionType.Unir, a);
        t.Agir(t.Un<Cupidon>(), ActionType.Unir, b);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, a);

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.False(a.EstVivant);
        Assert.False(b.EstVivant);
        Assert.True(t.JournalPublicContient(b.Nom, "chagrin"));
    }

    [Fact]
    public void Un_couple_loup_villageois_gagne_s_il_est_le_dernier_en_vie()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Cupidon, RoleId.SimpleVillageois);
        var (loup, cupidon, villageois) = (t.Un<LoupGarou>(), t.Un<Cupidon>(), t.Un<SimpleVillageois>());

        t.Agir(cupidon, ActionType.Unir, loup);
        t.Agir(cupidon, ActionType.Unir, villageois);
        Assert.True(t.ARecu(loup, "Vos camps sont opposés"));
        t.Agir(loup, ActionType.Voter, cupidon);

        Assert.Equal(Phase.Terminee, t.Partie.Phase);
        Assert.Equal(Camp.Amoureux, t.Partie.Vainqueur);
    }

    // ---------------------------------------------------------------- Chasseur / Loup Vengeur

    [Fact]
    public void Le_chasseur_tue_par_les_loups_abat_un_joueur()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Chasseur, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (loup, chasseur) = (t.Un<LoupGarou>(), t.Un<Chasseur>());

        t.Agir(loup, ActionType.Voter, chasseur);

        Assert.Equal(Phase.DernieresVolontes, t.Partie.Phase);
        Assert.Contains(t.Partie.ActionsDisponibles(chasseur.Id), a => a.Type == ActionType.Tirer);
        t.Agir(chasseur, ActionType.Tirer, loup);

        Assert.False(loup.EstVivant);
        Assert.Equal(Camp.Village, t.Partie.Vainqueur);
    }

    [Fact]
    public void Si_le_chasseur_ne_tire_pas_a_temps_la_partie_reprend()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Chasseur, RoleId.SimpleVillageois, RoleId.SimpleVillageois);

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, t.Un<Chasseur>());
        t.Horloge.Avancer(t.Partie.Parametres.DureeDerniereVolonte);
        t.Partie.Actualiser();

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.Equal(3, t.Partie.Vivants.Count());
    }

    [Fact]
    public void Le_loup_vengeur_elimine_au_vote_emporte_un_villageois()
    {
        var t = new PartieDeTest(RoleId.LoupVengeur, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var vengeur = t.Un<LoupVengeur>();
        var villageois = t.Tous<SimpleVillageois>();
        t.LaisserPasserLaNuit();

        t.Agir(vengeur, ActionType.Voter, villageois[0]);
        foreach (var v in villageois)
            t.Agir(v, ActionType.Voter, vengeur);
        t.Agir(vengeur, ActionType.Tirer, villageois[0]);

        Assert.False(villageois[0].EstVivant);
        Assert.Equal(Camp.Village, t.Partie.Vainqueur);
    }

    // ---------------------------------------------------------------- Dictateur

    [Fact]
    public void Le_coup_d_etat_execute_la_cible_et_fait_tomber_la_nuit()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Dictateur, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var dictateur = t.Un<Dictateur>();
        var cible = t.Tous<SimpleVillageois>()[0];
        t.LaisserPasserLaNuit();

        t.Agir(dictateur, ActionType.CoupDEtat, cible);

        Assert.False(cible.EstVivant);
        Assert.Equal(Phase.Nuit, t.Partie.Phase);
        Assert.Equal(2, t.Partie.NumeroTour);
        Assert.False(((Dictateur)dictateur.Role!).CoupDisponible);
    }

    // ---------------------------------------------------------------- Héritier

    [Fact]
    public void L_heritier_reste_heritier_mais_recupere_les_pouvoirs_et_les_potions_restantes()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Heritier, RoleId.Sorciere, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (loup, heritier, sorciere) = (t.Un<LoupGarou>(), t.Un<Heritier>(), t.Un<Sorciere>());
        var villageois = t.Tous<SimpleVillageois>();

        t.Agir(heritier, ActionType.Heriter, sorciere);
        t.Agir(loup, ActionType.Voter, sorciere);
        t.Agir(sorciere, ActionType.Empoisonner, villageois[0]);
        t.Agir(sorciere, ActionType.Passer);

        Assert.False(sorciere.EstVivant);
        Assert.IsType<Heritier>(heritier.Role);
        var pouvoir = Assert.IsType<Sorciere>(heritier.Pouvoir);
        Assert.True(pouvoir.PotionVieDisponible);
        Assert.False(pouvoir.PotionMortDisponible);
        Assert.Equal("Héritier (Sorcière)", VuePartie.Pour(t.Partie, heritier.Id).Moi.Role);

        // La nuit suivante, c'est lui qui joue l'étape de la Sorcière.
        t.LaisserPasserLeJour();
        t.Agir(loup, ActionType.Voter, villageois[1]);
        Assert.Contains(t.Partie.ActionsDisponibles(heritier.Id), a => a.Type == ActionType.Soigner);
    }

    [Fact]
    public void L_heritier_d_un_loup_rejoint_la_meute()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Heritier, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (loup, heritier) = (t.Un<LoupGarou>(), t.Un<Heritier>());
        var villageois = t.Tous<SimpleVillageois>();

        t.Agir(heritier, ActionType.Heriter, loup);
        t.LaisserPasserLaNuit();
        t.Agir(loup, ActionType.Voter, villageois[0]);
        t.Agir(heritier, ActionType.Voter, loup);
        t.Agir(villageois[0], ActionType.Voter, loup);
        t.Agir(villageois[1], ActionType.Voter, loup);

        Assert.False(loup.EstVivant);
        Assert.True(heritier.EstLoup);
        Assert.Equal(Phase.Nuit, t.Partie.Phase);
    }

    // ---------------------------------------------------------------- Chaperon Rouge

    [Fact]
    public void Le_chaperon_rouge_survit_aux_loups_tant_que_le_chasseur_vit()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.ChaperonRouge, RoleId.Chasseur, RoleId.SimpleVillageois);
        var chaperon = t.Un<ChaperonRouge>();

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, chaperon);

        Assert.True(chaperon.EstVivant);
    }

    [Fact]
    public void Sans_chasseur_le_chaperon_rouge_meurt_normalement()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.ChaperonRouge, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var chaperon = t.Un<ChaperonRouge>();

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, chaperon);

        Assert.False(chaperon.EstVivant);
    }

    // ---------------------------------------------------------------- Croque-Mort

    [Fact]
    public void Le_croque_mort_revele_un_duo_contenant_un_loup()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.CroqueMort, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (loup, croqueMort) = (t.Un<LoupGarou>(), t.Un<CroqueMort>());
        var designe = t.Tous<SimpleVillageois>()[0];

        t.Agir(loup, ActionType.Voter, croqueMort);
        t.Agir(croqueMort, ActionType.Designer, designe);

        // Le désigné est villageois : l'autre joueur est forcément le seul loup.
        Assert.True(t.JournalPublicContient(designe.Nom, loup.Nom, "il y a un loup-garou"));
        Assert.Equal(Phase.Jour, t.Partie.Phase);
    }

    [Fact]
    public void Le_croque_mort_entend_les_morts_la_nuit()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.CroqueMort, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var croqueMort = t.Un<CroqueMort>();
        var (mort, vivant) = (t.Tous<SimpleVillageois>()[0], t.Tous<SimpleVillageois>()[1]);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, mort);
        t.LaisserPasserLeJour();

        t.Dire(mort, "Méfiez-vous de tout le monde");
        t.Dire(croqueMort, "Je vous entends");

        Assert.Equal(Phase.Nuit, t.Partie.Phase);
        Assert.Contains(VuePartie.Pour(t.Partie, croqueMort.Id).Journal, e => e.Texte == "Méfiez-vous de tout le monde");
        Assert.Contains(VuePartie.Pour(t.Partie, mort.Id).Journal, e => e.Texte == "Je vous entends");
        Assert.DoesNotContain(VuePartie.Pour(t.Partie, vivant.Id).Journal, e => e.Canal == Canal.Morts);
    }

    // ---------------------------------------------------------------- Vétéran

    [Fact]
    public void Le_veteran_mordu_survit_une_journee_puis_succombe()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.Veteran, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var veteran = t.Un<Veteran>();

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, veteran);

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.True(veteran.EstVivant);
        Assert.True(t.JournalPublicContient(veteran.Nom, "a survécu"));
        Assert.Contains(t.Partie.ActionsDisponibles(veteran.Id), a => a.Type == ActionType.Voter);

        t.LaisserPasserLeJour();

        Assert.False(veteran.EstVivant);
        Assert.True(t.JournalPublicContient(veteran.Nom, "blessures"));
    }

    // ---------------------------------------------------------------- Loup Bavard

    [Fact]
    public void Le_loup_bavard_qui_place_son_mot_survit()
    {
        var t = new PartieDeTest(RoleId.LoupBavard, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var bavard = t.Un<LoupBavard>();
        t.LaisserPasserLaNuit();
        var mot = ((LoupBavard)bavard.Role!).MotImpose!;

        t.Dire(bavard, $"Bizarre, j'ai rêvé d'un {mot.ToUpperInvariant()} cette nuit.");
        t.LaisserPasserLeJour();

        Assert.True(bavard.EstVivant);
        Assert.True(t.ARecu(bavard, "Mot placé"));
    }

    [Fact]
    public void Le_loup_bavard_qui_ne_place_pas_son_mot_meurt_le_soir()
    {
        var t = new PartieDeTest(RoleId.LoupBavard, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var bavard = t.Un<LoupBavard>();
        t.LaisserPasserLaNuit();

        t.Dire(bavard, "Bonjour tout le monde !");
        t.LaisserPasserLeJour();

        Assert.False(bavard.EstVivant);
        Assert.Equal(Camp.Village, t.Partie.Vainqueur);
    }

    // ---------------------------------------------------------------- Loup Perfide

    [Fact]
    public void La_voyante_voit_le_loup_perfide_comme_simple_villageois()
    {
        var t = new PartieDeTest(RoleId.LoupPerfide, RoleId.Voyante, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (perfide, voyante) = (t.Un<LoupPerfide>(), t.Un<Voyante>());

        t.Agir(voyante, ActionType.Sonder, perfide);

        Assert.True(t.ARecu(voyante, $"{perfide.Nom} est Simple Villageois"));
    }

    // ---------------------------------------------------------------- Loup Amnésique

    [Fact]
    public void Le_loup_amnesique_se_croit_villageois_puis_rejoint_la_meute_une_fois_mordu()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.LoupAmnesique, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var (loup, amnesique) = (t.Un<LoupGarou>(), t.Un<LoupAmnesique>());

        var avant = VuePartie.Pour(t.Partie, amnesique.Id);
        Assert.Equal("Simple Villageois", avant.Moi.Role);
        Assert.Equal(Camp.Village, avant.Moi.Camp);
        Assert.False(VuePartie.Pour(t.Partie, loup.Id).Joueurs.Single(j => j.Id == amnesique.Id).EstLoupAllie);

        t.Agir(loup, ActionType.Voter, amnesique);

        Assert.True(amnesique.EstVivant);
        Assert.True(amnesique.EstLoup);
        Assert.Equal("Loup Amnésique", VuePartie.Pour(t.Partie, amnesique.Id).Moi.Role);
        Assert.True(VuePartie.Pour(t.Partie, loup.Id).Joueurs.Single(j => j.Id == amnesique.Id).EstLoupAllie);
    }

    [Fact]
    public void Le_village_peut_gagner_avec_un_loup_amnesique_encore_endormi()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.LoupAmnesique, RoleId.SimpleVillageois);
        var (loup, amnesique, villageois) = (t.Un<LoupGarou>(), t.Un<LoupAmnesique>(), t.Un<SimpleVillageois>());
        t.LaisserPasserLaNuit();

        t.Agir(loup, ActionType.Voter, villageois);
        t.Agir(amnesique, ActionType.Voter, loup);
        t.Agir(villageois, ActionType.Voter, loup);

        Assert.Equal(Camp.Village, t.Partie.Vainqueur);
    }

    // ---------------------------------------------------------------- Composition

    [Fact]
    public void Seul_l_hote_choisit_la_composition_et_elle_doit_correspondre_aux_joueurs()
    {
        var partie = new Partie("TEST");
        var hote = partie.AjouterJoueur("Hôte");
        var invite = partie.AjouterJoueur("Invité");
        partie.AjouterJoueur("C");
        partie.AjouterJoueur("D");

        Assert.Throws<ActionInvalideException>(() => partie.DefinirComposition(invite.Id, [RoleId.LoupGarou]));

        partie.DefinirComposition(hote.Id, [RoleId.LoupGarou, RoleId.Chasseur, RoleId.Cupidon]);
        Assert.NotNull(partie.ObstacleAuDemarrage());
        Assert.Throws<ActionInvalideException>(() => partie.Demarrer(hote.Id));

        partie.DefinirComposition(hote.Id, [RoleId.LoupGarou, RoleId.Chasseur, RoleId.Cupidon, RoleId.Dictateur]);
        partie.Demarrer(hote.Id);
        Assert.Equal(Phase.Lancement, partie.Phase);
    }
}
