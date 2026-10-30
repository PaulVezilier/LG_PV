import { useJeu } from "../stores/jeu";
import type { ActionDisponible, ActionType, VuePartie } from "../types";

interface BandeauProps {
  etat: VuePartie;
  actionArmee: ActionDisponible | null;
  onArmer: (type: ActionType) => void;
  onRetourSalon: () => void;
}

/** Au-dessus des joueurs : ce qui se passe (pour tous), ce que je dois faire, et les actions sans cible. */
export function Bandeau({ etat, actionArmee, onArmer, onRetourSalon }: BandeauProps) {
  const { agir, demarrer } = useJeu();
  const actionsCiblees = etat.actions.filter((a) => a.cibleRequise);
  const actionsDirectes = etat.actions.filter((a) => !a.cibleRequise);

  return (
    <div className="bandeau">
      <h2 className="narration">{etat.narration}</h2>

      {etat.consigne && <p className="consigne">{etat.consigne}</p>}

      {etat.phase === "Lobby" && (
        <p className="consigne-lobby">
          {etat.joueurs.length} joueur{etat.joueurs.length > 1 ? "s" : ""} autour de la table
          {etat.moi.estHote
            ? etat.obstacleAuDemarrage && <> · <span className="obstacle">{etat.obstacleAuDemarrage}</span></>
            : " · l'hôte lancera la partie"}
        </p>
      )}

      <div className="bandeau-actions">
        {actionsCiblees.length > 1 && actionsCiblees.map((a) => (
          <button
            key={a.type}
            className={"bouton-mode" + (a.type === actionArmee?.type ? " actif" : "")}
            onClick={() => onArmer(a.type)}
          >
            {a.libelle}
          </button>
        ))}
        {actionsDirectes.map((a) => (
          <button key={a.type} className="bouton-secondaire" onClick={() => agir(a.type)}>{a.libelle}</button>
        ))}
        {etat.peutDemarrer && <button className="bouton-principal" onClick={demarrer}>Lancer la partie</button>}
        {etat.phase === "Terminee" && <button className="bouton-principal" onClick={onRetourSalon}>Retour au salon</button>}
      </div>
    </div>
  );
}
