using LG.Domain.Roles;
using LG.Domain.Vues;

namespace LG.Domain.Tests;

public class VuePartieTests
{
    [Fact]
    public void Tout_le_village_voit_la_narration_mais_seul_l_acteur_recoit_la_consigne()
    {
        var t = new PartieDeTest(RoleId.Garde, RoleId.LoupGarou, RoleId.SimpleVillageois);

        var vueGarde = VuePartie.Pour(t.Partie, t.Un<Garde>().Id);
        var vueVillageois = VuePartie.Pour(t.Partie, t.Un<SimpleVillageois>().Id);

        Assert.Equal("Le Garde doit protéger quelqu'un", vueVillageois.Narration);
        Assert.Equal(vueVillageois.Narration, vueGarde.Narration);
        Assert.Null(vueVillageois.Consigne);
        Assert.NotNull(vueGarde.Consigne);
    }

    [Fact]
    public void Le_recapitulatif_compte_les_roles_encore_vivants()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var victime = t.Tous<SimpleVillageois>()[0];

        t.Agir(t.Un<LoupGarou>(), ActionType.Voter, victime);
        t.LaisserPasserLaNuit();

        var role = VuePartie.Pour(t.Partie, victime.Id).Composition.Single(r => r.Id == RoleId.SimpleVillageois);
        Assert.Equal(3, role.Nombre);
        Assert.Equal(2, role.Vivants);
    }

    [Fact]
    public void Le_vote_du_joueur_lui_est_renvoye()
    {
        // Deux loups : le premier vote ne termine pas l'étape.
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois);
        var loup = t.Un<LoupGarou>();
        var cible = t.Tous<SimpleVillageois>()[0];

        t.Agir(loup, ActionType.Voter, cible);

        Assert.Equal(cible.Id, VuePartie.Pour(t.Partie, loup.Id).MonVote);
    }

    [Fact]
    public void Quand_l_hote_quitte_le_lobby_le_joueur_suivant_devient_hote()
    {
        var partie = new Partie("TEST");
        var hote = partie.AjouterJoueur("Alice");
        var invite = partie.AjouterJoueur("Bob");

        partie.RetirerJoueur(hote.Id);

        Assert.Equal(invite.Id, partie.HoteId);
        Assert.Single(partie.Joueurs);
    }

    [Fact]
    public void Le_lancement_laisse_un_decompte_avant_la_premiere_nuit()
    {
        var horloge = new HorlogeFactice();
        var parametres = new ParametresPartie { Composition = [RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois, RoleId.SimpleVillageois] };
        var partie = new Partie("TEST", parametres, horloge, new Random(1));
        var hote = partie.AjouterJoueur("Alice");
        foreach (var nom in new[] { "Bob", "Chloé", "David" })
            partie.AjouterJoueur(nom);

        partie.Demarrer(hote.Id);

        Assert.Equal(Phase.Lancement, partie.Phase);
        Assert.Equal(horloge.Maintenant + parametres.DureeLancement, partie.FinPhase);
        Assert.All(partie.Joueurs, j => Assert.Null(j.Role));
        Assert.Throws<ActionInvalideException>(() => partie.AjouterJoueur("Ève"));
        Assert.Throws<ActionInvalideException>(() => partie.RetirerJoueur(hote.Id));

        horloge.Avancer(parametres.DureeLancement);
        Assert.True(partie.Actualiser());

        Assert.Equal(Phase.Nuit, partie.Phase);
        Assert.All(partie.Joueurs, j => Assert.NotNull(j.Role));
    }

    [Fact]
    public void Seul_l_hote_choisit_l_ambiance_et_tout_le_monde_la_voit()
    {
        var partie = new Partie("TEST");
        var hote = partie.AjouterJoueur("Alice");
        var invite = partie.AjouterJoueur("Bob");

        Assert.Throws<ActionInvalideException>(() => partie.DefinirAmbiance(invite.Id, Ambiance.Hiver));
        partie.DefinirAmbiance(hote.Id, Ambiance.Halloween);

        Assert.Equal(Ambiance.Halloween, VuePartie.Pour(partie, invite.Id).Ambiance);
    }

    [Fact]
    public void On_ne_peut_pas_quitter_une_partie_lancee()
    {
        var t = new PartieDeTest(RoleId.LoupGarou, RoleId.SimpleVillageois, RoleId.SimpleVillageois);

        Assert.Throws<ActionInvalideException>(() => t.Partie.RetirerJoueur(t.Un<LoupGarou>().Id));
    }
}
