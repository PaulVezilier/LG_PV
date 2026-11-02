import { titrePhase } from "../roles";
import { useJeu } from "../stores/jeu";
import type { ActionDisponible, ActionType, VuePartie } from "../types";
import { Minuteur } from "./Minuteur";

/** Au-dessus du village : la phase, ce qui se passe (pour tous) et ce que je dois faire. */
export function Bandeau({ etat }: { etat: VuePartie }) {
  return (
    <div className="lg-phase">
      <span className="lg-phase-t">{titrePhase(etat.phase, etat.numeroTour)}</span>
      <span className="lg-phase-x">{etat.narration}</span>
      <Minuteur fin={etat.finPhase} />
      {etat.consigne && <span className="lg-phase-h">{etat.consigne}</span>}
    </div>
  );
}

interface BarreActionsProps {
  etat: VuePartie;
  actionArmee: ActionDisponible | null;
  onArmer: (type: ActionType) => void;
  onRetourSalon: () => void;
}

/** Sous le village : choix de l'action ciblée (vote / coup d'État), actions sans cible, retour au salon. */
export function BarreActions({ etat, actionArmee, onArmer, onRetourSalon }: BarreActionsProps) {
  const agir = useJeu((s) => s.agir);
  const actionsCiblees = etat.actions.filter((a) => a.cibleRequise);
  const actionsDirectes = etat.actions.filter((a) => !a.cibleRequise);

  return (
    <div className="lg-info">
      {actionsCiblees.length > 1 && actionsCiblees.map((a) => (
        <button
          key={a.type}
          className={"bouton-mode" + (a.type === actionArmee?.type ? " actif" : "")}
          aria-pressed={a.type === actionArmee?.type}
          onClick={() => onArmer(a.type)}
        >
          {a.libelle}
        </button>
      ))}
      {actionsDirectes.map((a) => (
        <button key={a.type} className="bouton-secondaire" onClick={() => agir(a.type)}>{a.libelle}</button>
      ))}
      {etat.phase === "Terminee" && <button className="bouton-principal" onClick={onRetourSalon}>Retour au salon</button>}
    </div>
  );
}
