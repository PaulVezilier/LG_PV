import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { Bandeau } from "../components/Bandeau";
import { Minuteur } from "../components/Minuteur";
import { PanneauDroit } from "../components/PanneauDroit";
import { Plateau } from "../components/Plateau";
import { SaisieChat } from "../components/SaisieChat";
import { titrePhase } from "../roles";
import { useCompte } from "../stores/compte";
import { useJeu } from "../stores/jeu";
import type { ActionType } from "../types";

export function PagePartie() {
  const code = useParams().code!.toUpperCase();
  const pseudo = useCompte((s) => s.pseudo)!;
  const { etat, entrer, quitterPartie } = useJeu();
  const navigate = useNavigate();

  // Action ciblée « armée » : un clic sur un joueur l'exécute (utile quand il y en a plusieurs, ex. vote / coup d'État).
  const [typeArme, setTypeArme] = useState<ActionType | null>(null);

  useEffect(() => {
    entrer(code, pseudo).then((ok) => { if (!ok) navigate("/", { replace: true }); });
  }, [code, pseudo, entrer, navigate]);

  if (!etat || etat.partieId !== code) {
    return <div className="chargement">Entrée dans la partie {code}…</div>;
  }

  const actionsCiblees = etat.actions.filter((a) => a.cibleRequise);
  const actionArmee = actionsCiblees.find((a) => a.type === typeArme) ?? actionsCiblees[0] ?? null;

  async function quitter() {
    const enCours = etat!.phase !== "Lobby" && etat!.phase !== "Terminee";
    if (enCours && !confirm("Quitter la table ? La partie continue sans vous ; vous pourrez la reprendre depuis le salon.")) return;
    await quitterPartie();
    navigate("/");
  }

  return (
    <div className="page-partie" data-phase={etat.phase}>
      <header className="barre">
        <span className="marque">Loup-Garou</span>
        <span className="titre-phase">{titrePhase(etat.phase, etat.numeroTour)}</span>
        <Minuteur fin={etat.finPhase} />
        <span className="barre-espace" />
        <span className="discret">Partie <span className="code">{etat.partieId}</span></span>
        <button className="bouton-discret" onClick={quitter}>Quitter</button>
      </header>

      <main className="table-de-jeu">
        <section className="zone-plateau">
          <Bandeau etat={etat} actionArmee={actionArmee} onArmer={setTypeArme} onRetourSalon={quitter} />
          <Plateau etat={etat} actionArmee={actionArmee} />
          <SaisieChat canal={etat.canalChat} />
        </section>
        <PanneauDroit etat={etat} />
      </main>
    </div>
  );
}
