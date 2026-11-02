import type { CSSProperties } from "react";
import { useJeu } from "../stores/jeu";
import type { ActionDisponible, VueJoueur, VuePartie } from "../types";
import { decorVillage, personnage, placesVillage, type PlaceVillage } from "../village";
import { CoinsDores } from "./Icones";

interface VillageProps {
  etat: VuePartie;
  actionArmee: ActionDisponible | null;
  /** Affiche le décor de jour quelle que soit la phase (début de partie, avant la tombée de la première nuit). */
  forcerJour?: boolean;
}

/** La place du village : le décor (jour ou nuit) et les joueurs debout, sur lesquels on clique pour agir. */
export function Village({ etat, actionArmee, forcerJour = false }: VillageProps) {
  const agir = useJeu((s) => s.agir);
  const nuit = etat.phase === "Nuit" && !forcerJour;
  const places = placesVillage(etat.joueurs.length);

  function cliquer(joueur: VueJoueur) {
    if (!actionArmee?.cibles.includes(joueur.id)) return;
    if (actionArmee.type === "CoupDEtat" && !confirm(`Faire exécuter ${joueur.nom} ? Votre coup d'État ne servira qu'une fois.`)) return;
    agir(actionArmee.type, joueur.id);
  }

  return (
    <div className="lg-scene">
      {/* Les deux décors sont superposés : le passage jour/nuit est un fondu. */}
      <img className="lg-layer" src={decorVillage(etat.ambiance, false)} alt="" style={{ opacity: nuit ? 0 : 1 }} />
      <img className="lg-layer" src={decorVillage(etat.ambiance, true)} alt="" style={{ opacity: nuit ? 1 : 0 }} />
      <div className={"lg-chars" + (nuit ? " nuit" : "")}>
        {etat.joueurs.map((joueur, i) => (
          <Villageois
            key={joueur.id}
            joueur={joueur}
            index={i}
            place={places[i]}
            estMoi={joueur.id === etat.moi.id}
            estMonVote={joueur.id === etat.monVote}
            action={actionArmee?.cibles.includes(joueur.id) ? actionArmee : null}
            onClick={() => cliquer(joueur)}
          />
        ))}
      </div>
      <CoinsDores />
    </div>
  );
}

interface VillageoisProps {
  joueur: VueJoueur;
  index: number;
  place: PlaceVillage;
  estMoi: boolean;
  estMonVote: boolean;
  /** Action que le joueur courant peut faire sur ce villageois (null s'il n'est pas ciblable). */
  action: ActionDisponible | null;
  onClick: () => void;
}

function Villageois({ joueur, index, place, estMoi, estMonVote, action, onClick }: VillageoisProps) {
  const classes = ["pl"];
  if (action) classes.push("ciblable");
  if (!joueur.estVivant) classes.push("mort");
  if (estMonVote) classes.push("selection");

  const etiquette = ["pl-name"];
  if (estMoi) etiquette.push("moi");
  if (estMonVote) etiquette.push("selection");
  if (!joueur.estVivant) etiquette.push("mort");
  if (joueur.estLoupAllie) etiquette.push("loup-allie");

  // L'arrivée des villageois est échelonnée.
  const delai = `${(0.3 + index * 0.12).toFixed(2)}s`;
  const style = {
    "--lx": `${place.gauche}%`, "--ly": `${place.haut}%`, "--lw": `${place.largeur}%`, "--lz": place.profondeur, "--dl": delai,
  } as CSSProperties;
  const styleEtiquette = { "--nx": `${place.etiquetteX}%`, "--ny": `${place.etiquetteY}%`, "--dl": delai, zIndex: 1010 + place.profondeur } as CSSProperties;

  return (
    <>
      <button
        type="button"
        className={classes.join(" ")}
        style={style}
        disabled={!action}
        onClick={onClick}
        aria-label={action ? `${action.libelle} ${joueur.nom}` : joueur.nom}
      >
        <img className="pl-fig" src={personnage(index)} alt="" />
        {estMonVote && <span className="pl-ring" />}
      </button>
      <span className={etiquette.join(" ")} style={styleEtiquette}>
        <span className="pl-pseudo">
          {joueur.estAmoureux && <span title="Amoureux">💘 </span>}
          {joueur.nom}
          {estMoi && <span className="pl-vous"> (vous)</span>}
          {joueur.aVote && <span className="pl-a-vote" title="A voté"> ✓</span>}
        </span>
        {joueur.role && !estMoi && <span className="pl-role">{joueur.role}</span>}
        {joueur.voix > 0 && <span className="pl-voix" title={`${joueur.voix} voix contre ${joueur.nom}`}>{joueur.voix}</span>}
      </span>
    </>
  );
}
