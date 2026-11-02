import { useEffect, useRef, useState, type CSSProperties } from "react";
import { useJeu } from "../stores/jeu";
import type { VueJoueur, VuePartie } from "../types";
import { AMBIANCES, decorLobby, personnage, SIEGES_LOBBY, type SiegeLobby } from "../village";
import { Decompte } from "./Decompte";
import { CoinsDores, IconeCartes, IconeCouronne, IconeInviter, IconeLancer, IconeLune, IconePlaceLibre } from "./Icones";
import { RecapRoles } from "./RecapRoles";

/** Durée du zoom vers la table (transition CSS de .lb-world), avant d'afficher le village. */
export const DUREE_ZOOM = 1900;

interface LobbyProps {
  etat: VuePartie;
  /** Vrai pendant le zoom qui suit le décompte. */
  zoom: boolean;
  onDecompteFini: () => void;
}

/** La table du lobby : les joueurs assis autour, la composition, l'ambiance, et le lancement. */
export function Lobby({ etat, zoom, onDecompteFini }: LobbyProps) {
  const { demarrer, definirAmbiance } = useJeu();
  const [compositionOuverte, setCompositionOuverte] = useState(false);
  const [lienCopie, setLienCopie] = useState(false);
  const hote = etat.moi.estHote;
  const lancement = etat.phase === "Lancement";
  const ambianceModifiable = hote && !lancement;

  async function inviter() {
    await navigator.clipboard?.writeText(location.href).catch(() => undefined);
    setLienCopie(true);
    setTimeout(() => setLienCopie(false), 2000);
  }

  function changerAmbiance() {
    definirAmbiance(AMBIANCES[(AMBIANCES.indexOf(etat.ambiance) + 1) % AMBIANCES.length]);
  }

  const libelleAmbiance = etat.ambiance === "Classique" ? "Ambiance" : etat.ambiance;
  const titreAmbiance = ambianceModifiable ? "Changer l'ambiance du village" : `Ambiance : ${etat.ambiance}`;

  return (
    <div className={"lobby" + (zoom ? " is-zoom" : "")}>
      <div className="lb-scene">
        <div className="lb-world">
          <img className="lg-layer" src={decorLobby(etat.ambiance)} alt="" />
          {SIEGES_LOBBY.map((siege, i) => (
            <Siege key={i} siege={siege} joueur={etat.joueurs[i]} index={i} estMoi={etat.joueurs[i]?.id === etat.moi.id} onInviter={inviter} />
          ))}
          {SIEGES_LOBBY.map((siege, i) => (
            <span key={i} className={"lb-plaque" + (etat.joueurs[i] ? "" : " libre")} style={varsSiege(siege)}>
              {etat.joueurs[i]?.nom ?? "Libre"}
            </span>
          ))}
          <button type="button" className="lb-label" style={{ left: "11.88%", top: "32.89%" }} onClick={() => setCompositionOuverte(true)}>
            <IconeCartes /><span>Rôles</span>
          </button>
          <button type="button" className="lb-label" style={{ left: "8.12%", top: "84.44%" }} disabled={!ambianceModifiable} title={titreAmbiance} onClick={changerAmbiance}>
            <IconeLune /><span>{libelleAmbiance}</span>
          </button>
        </div>
        <CoinsDores />
        {lancement && !zoom && etat.finPhase && <Decompte fin={etat.finPhase} onFini={onDecompteFini} />}
        <div className="lb-fade" />
      </div>

      {/* Sur mobile, les étiquettes posées sur la table deviennent des boutons sous la scène. */}
      <div className="lb-props">
        <button type="button" className="lb-chip" onClick={() => setCompositionOuverte(true)}><IconeCartes /><span>Rôles</span></button>
        <button type="button" className="lb-chip" disabled={!ambianceModifiable} title={titreAmbiance} onClick={changerAmbiance}>
          <IconeLune /><span>{libelleAmbiance}</span>
        </button>
      </div>

      <div className="lb-bar">
        <span className="lb-count" aria-live="polite">{etat.joueurs.length} / {SIEGES_LOBBY.length} joueurs</span>
        <button type="button" className="lb-btn" onClick={inviter} disabled={lancement}>
          <IconeInviter />{lienCopie ? "Lien copié !" : "Inviter"}
        </button>
        {hote ? (
          <button type="button" className="lb-go" onClick={demarrer} disabled={!etat.peutDemarrer} title={etat.obstacleAuDemarrage ?? undefined}>
            <IconeLancer />Lancer la partie
          </button>
        ) : (
          <span className="lb-attente">{lancement ? "La partie commence…" : "L'hôte lancera la partie"}</span>
        )}
      </div>
      {hote && etat.obstacleAuDemarrage && <p className="lb-obstacle">{etat.obstacleAuDemarrage}</p>}

      <FenetreComposition etat={etat} ouverte={compositionOuverte} onFermer={() => setCompositionOuverte(false)} />
    </div>
  );
}

function varsSiege(s: SiegeLobby): CSSProperties {
  return {
    "--lx": `${s.lx}%`, "--ly": `${s.ly}%`, "--lw": `${s.lw}%`,
    "--px": `${s.px}%`, "--py": `${s.py}%`, "--pw": `${s.pw}%`,
    "--plx": `${s.plx}%`, "--ply": `${s.ply}%`, "--plt": s.plt,
  } as CSSProperties;
}

interface SiegeProps {
  siege: SiegeLobby;
  joueur: VueJoueur | undefined;
  index: number;
  estMoi: boolean;
  onInviter: () => void;
}

function Siege({ siege, joueur, index, estMoi, onInviter }: SiegeProps) {
  return (
    <div className="lb-seat" style={varsSiege(siege)}>
      {joueur ? (
        <div className={"lb-medal" + (estMoi ? " moi" : "")} title={estMoi ? "Vous" : joueur.nom}>
          <img className="lb-fig" src={personnage(index)} alt="" />
        </div>
      ) : (
        <button type="button" className="lb-medal lb-empty" onClick={onInviter} aria-label="Inviter un joueur sur cette place">
          <IconePlaceLibre />
        </button>
      )}
      {joueur?.estHote && <span className="lb-hote" title="Hôte de la partie"><IconeCouronne /></span>}
    </div>
  );
}

/** La composition, dans une fenêtre ouverte depuis l'étiquette « Rôles » de la table. */
function FenetreComposition({ etat, ouverte, onFermer }: { etat: VuePartie; ouverte: boolean; onFermer: () => void }) {
  const fenetre = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const el = fenetre.current!;
    if (ouverte && !el.open) el.showModal();
    if (!ouverte && el.open) el.close();
  }, [ouverte]);

  return (
    <dialog
      ref={fenetre}
      className="fenetre"
      onClose={onFermer}
      // Un clic sur le fond assombri ferme la fenêtre.
      onClick={(e) => { if (e.target === e.currentTarget) onFermer(); }}
    >
      <div className="fenetre-contenu">
        <header className="fenetre-entete">
          <h2>Composition</h2>
          <button type="button" className="bouton-discret" onClick={onFermer} aria-label="Fermer">✕</button>
        </header>
        {!etat.compositionModifiable && <p className="discret">Seul l'hôte peut modifier la composition.</p>}
        <RecapRoles etat={etat} />
      </div>
    </dialog>
  );
}
