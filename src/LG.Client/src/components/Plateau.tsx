import { useJeu } from "../stores/jeu";
import type { ActionDisponible, VueJoueur, VuePartie } from "../types";
import { Journal } from "./Journal";

interface PlateauProps {
  etat: VuePartie;
  actionArmee: ActionDisponible | null;
}

// Rayons de l'ellipse des joueurs, en % de la taille du plateau.
const RAYON_X = 43;
const RAYON_Y = 40;

/** Les joueurs assis en cercle (le joueur courant en bas, face à la table), le chat au centre. */
export function Plateau({ etat, actionArmee }: PlateauProps) {
  const agir = useJeu((s) => s.agir);
  const n = etat.joueurs.length;
  const indexMoi = Math.max(0, etat.joueurs.findIndex((j) => j.id === etat.moi.id));

  function cliquer(joueur: VueJoueur) {
    if (!actionArmee) return;
    if (actionArmee.type === "CoupDEtat" && !confirm(`Faire exécuter ${joueur.nom} ? Votre coup d'État ne servira qu'une fois.`)) return;
    agir(actionArmee.type, joueur.id);
  }

  return (
    <div className="plateau">
      {etat.joueurs.map((joueur, i) => {
        const angle = Math.PI / 2 + ((i - indexMoi) * 2 * Math.PI) / n;
        const ciblable = !!actionArmee?.cibles.includes(joueur.id);
        return (
          <Siege
            key={joueur.id}
            joueur={joueur}
            estMoi={joueur.id === etat.moi.id}
            estMonVote={joueur.id === etat.monVote}
            ciblable={ciblable}
            libelle={ciblable ? actionArmee!.libelle : undefined}
            x={50 + RAYON_X * Math.cos(angle)}
            y={50 + RAYON_Y * Math.sin(angle)}
            onClick={() => cliquer(joueur)}
          />
        );
      })}
      <Journal evenements={etat.journal} />
    </div>
  );
}

interface SiegeProps {
  joueur: VueJoueur;
  estMoi: boolean;
  estMonVote: boolean;
  ciblable: boolean;
  libelle?: string;
  x: number;
  y: number;
  onClick: () => void;
}

/** Une place à la table. Placeholder : un simple cadre avec le pseudo, en attendant les avatars. */
function Siege({ joueur, estMoi, estMonVote, ciblable, libelle, x, y, onClick }: SiegeProps) {
  const classes = ["siege"];
  if (estMoi) classes.push("moi");
  if (!joueur.estVivant) classes.push("mort");
  if (ciblable) classes.push("ciblable");
  if (estMonVote) classes.push("mon-vote");
  if (joueur.estLoupAllie) classes.push("loup-allie");

  return (
    <button
      className={classes.join(" ")}
      style={{ left: `${x}%`, top: `${y}%` }}
      disabled={!ciblable}
      onClick={onClick}
      title={libelle ? `${libelle} ${joueur.nom}` : undefined}
    >
      <span className="siege-nom">
        {!joueur.estVivant && "✝ "}
        {joueur.nom}
      </span>
      <span className="siege-badges">
        {estMoi && <span className="badge">vous</span>}
        {joueur.estHote && <span className="badge">hôte</span>}
        {joueur.estLoupAllie && <span className="badge badge-loup">meute</span>}
        {joueur.estAmoureux && <span className="badge">💘</span>}
        {joueur.role && !estMoi && <span className="badge badge-role">{joueur.role}</span>}
      </span>
      {joueur.voix > 0 && <span className="siege-voix" title={`${joueur.voix} voix`}>{joueur.voix}</span>}
      {joueur.aVote && <span className="siege-a-vote" title="A voté">✓</span>}
    </button>
  );
}
