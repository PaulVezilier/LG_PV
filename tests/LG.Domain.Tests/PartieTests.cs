using LG.Domain.Roles;
using LG.Domain.Vues;

namespace LG.Domain.Tests;

public class PartieTests
{
    private static readonly RoleId[] CompositionClassique =
        [RoleId.LoupGarou, RoleId.Voyante, RoleId.Sorciere, RoleId.Garde, RoleId.SimpleVillageois, RoleId.SimpleVillageois];

    [Fact]
    public void Les_loups_devorent_leur_victime()
    {
        var t = new PartieDeTest(CompositionClassique);
        var (loup, voyante, sorciere, garde) = (t.Un<LoupGarou>(), t.Un<Voyante>(), t.Un<Sorciere>(), t.Un<Garde>());
        var victime = t.Tous<SimpleVillageois>()[0];

        t.Agir(garde, ActionType.Proteger, voyante);
        t.Agir(voyante, ActionType.Sonder, loup);
        t.Agir(loup, ActionType.Voter, victime);
        t.Agir(sorciere, ActionType.Passer);

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.False(victime.EstVivant);
        Assert.True(t.ARecu(voyante, $"{loup.Nom} est Loup-Garou"));
        Assert.True(t.ARecu(sorciere, $"les loups ont attaqué {victime.Nom}"));
    }

    [Fact]
    public void Le_garde_sauve_la_victime_des_loups()
    {
        var t = new PartieDeTest(CompositionClassique);
        var victime = t.Tous<SimpleVillageois>()[0];

        t.Agir(t.Un<Garde>(), ActionType.Proteger, victime);
        t.Agir(t.Un<Voyante>(), ActionType.Sonder, victime);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, victime);
        t.Agir(t.Un<Sorciere>(), ActionType.Passer);

        Assert.True(victime.EstVivant);
        Assert.Contains(t.Partie.Journal, e => e.Texte == "Personne n'est mort cette nuit.");
    }

    [Fact]
    public void Le_garde_ne_peut_pas_proteger_deux_fois_de_suite_la_meme_personne()
    {
        var t = new PartieDeTest(CompositionClassique);
        var garde = t.Un<Garde>();
        var protege = t.Tous<SimpleVillageois>()[0];

        t.Agir(garde, ActionType.Proteger, protege);
        t.LaisserPasserLaNuit();
        t.LaisserPasserLeJour();

        Assert.Equal(2, t.Partie.NumeroTour);
        Assert.DoesNotContain(protege.Id, t.Partie.ActionsDisponibles(garde.Id).Single().Cibles);
        Assert.Throws<ActionInvalideException>(() => t.Agir(garde, ActionType.Proteger, protege));
    }

    [Fact]
    public void La_sorciere_sauve_la_victime_avec_sa_potion_de_vie()
    {
        var t = new PartieDeTest(CompositionClassique);
        var sorciere = t.Un<Sorciere>();
        var victime = t.Tous<SimpleVillageois>()[0];

        t.Agir(t.Un<Garde>(), ActionType.Proteger, sorciere);
        t.Agir(t.Un<Voyante>(), ActionType.Sonder, victime);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, victime);
        t.Agir(sorciere, ActionType.Soigner);
        t.Agir(sorciere, ActionType.Passer);

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.True(victime.EstVivant);
        Assert.False(((Sorciere)sorciere.Role!).PotionVieDisponible);
    }

    [Fact]
    public void La_sorciere_empoisonne_un_joueur()
    {
        var t = new PartieDeTest(CompositionClassique);
        var sorciere = t.Un<Sorciere>();
        var (victime, empoisonne) = (t.Tous<SimpleVillageois>()[0], t.Tous<SimpleVillageois>()[1]);

        t.Agir(t.Un<Garde>(), ActionType.Proteger, sorciere);
        t.Agir(t.Un<Voyante>(), ActionType.Sonder, victime);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, victime);
        t.Agir(sorciere, ActionType.Empoisonner, empoisonne);
        t.Agir(sorciere, ActionType.Passer);

        Assert.False(victime.EstVivant);
        Assert.False(empoisonne.EstVivant);
    }

    [Fact]
    public void Le_loup_noir_infecte_la_victime()
    {
        var t = new PartieDeTest(
            RoleId.LoupGarouNoir, RoleId.LoupGarou, RoleId.Voyante, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var noir = t.Un<LoupGarouNoir>();
        var victime = t.Tous<SimpleVillageois>()[0];

        t.Agir(t.Un<Voyante>(), ActionType.Sonder, victime);
        t.Agir(noir, ActionType.Voter, victime);
        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, victime);
        t.Agir(noir, ActionType.Infecter);

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.True(victime.EstVivant);
        Assert.True(victime.EstLoup);
        Assert.False(((LoupGarouNoir)noir.Role!).InfectionDisponible);
    }

    [Fact]
    public void Un_joueur_ne_peut_pas_agir_hors_de_son_tour()
    {
        var t = new PartieDeTest(CompositionClassique);

        // La nuit commence par le Garde : les loups ne peuvent pas encore voter.
        Assert.Throws<ActionInvalideException>(() => t.Agir(t.Un<LoupGarou>(), ActionType.Voter, t.Un<Voyante>()));
    }

    [Fact]
    public void Le_temps_ecoule_fait_avancer_la_nuit_sans_victime()
    {
        var t = new PartieDeTest(CompositionClassique);

        t.LaisserPasserLaNuit();

        Assert.Equal(Phase.Jour, t.Partie.Phase);
        Assert.All(t.Partie.Joueurs, j => Assert.True(j.EstVivant));
    }

    [Fact]
    public void Le_village_gagne_en_eliminant_le_dernier_loup()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var loup = t.Un<LoupGarou>();
        var (a, b) = (t.Tous<SimpleVillageois>()[0], t.Tous<SimpleVillageois>()[1]);
        t.LaisserPasserLaNuit();

        t.Agir(loup, ActionType.Voter, a);
        t.Agir(a, ActionType.Voter, loup);
        t.Agir(b, ActionType.Voter, loup);

        Assert.Equal(Phase.Terminee, t.Partie.Phase);
        Assert.Equal(Camp.Village, t.Partie.Vainqueur);
    }

    [Fact]
    public void Les_loups_gagnent_quand_il_ne_reste_plus_de_villageois()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.SimpleVillageois);

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, t.Un<SimpleVillageois>());

        Assert.Equal(Phase.Terminee, t.Partie.Phase);
        Assert.Equal(Camp.LoupsGarous, t.Partie.Vainqueur);
    }

    [Fact]
    public void Une_egalite_au_vote_du_jour_n_elimine_personne()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var loup = t.Un<LoupGarou>();
        var villageois = t.Tous<SimpleVillageois>();
        t.LaisserPasserLaNuit();

        t.Agir(loup, ActionType.Voter, villageois[0]);
        t.Agir(villageois[0], ActionType.Voter, loup);
        t.Agir(villageois[1], ActionType.Voter, loup);
        t.Agir(villageois[2], ActionType.Voter, villageois[0]);

        Assert.Equal(Phase.Nuit, t.Partie.Phase);
        Assert.All(t.Partie.Joueurs, j => Assert.True(j.EstVivant));
    }

    [Fact]
    public void Les_vues_ne_revelent_pas_les_roles_caches()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.Voyante);
        var (loup1, loup2) = (t.Tous<LoupGarou>()[0], t.Tous<LoupGarou>()[1]);
        var villageois = t.Un<SimpleVillageois>();

        var vueVillageois = VuePartie.Pour(t.Partie, villageois.Id);
        var vueLoup = VuePartie.Pour(t.Partie, loup1.Id);

        Assert.All(vueVillageois.Joueurs.Where(j => j.Id != villageois.Id), j => Assert.Null(j.Role));
        Assert.DoesNotContain(vueVillageois.Journal, e => e.Canal is Canal.Loups or Canal.Prive && !e.Texte.Contains("Simple Villageois"));
        Assert.True(vueLoup.Joueurs.Single(j => j.Id == loup2.Id).EstLoupAllie);
    }
}
