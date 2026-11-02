import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { Bandeau, BarreActions } from "../components/Bandeau";
import { Marque } from "../components/Icones";
import { DUREE_ZOOM, Lobby } from "../components/Lobby";
import { PanneauDroit } from "../components/PanneauDroit";
import { Village } from "../components/Village";
import { useCompte } from "../stores/compte";
import { useJeu } from "../stores/jeu";
import type { ActionType } from "../types";

/** Zoom vers la table à la fin du décompte : pas commencé, en cours, ou terminé (le village peut s'afficher). */
type Zoom = "non" | "en-cours" | "fini";

/** Après le zoom, le village apparaît en plein jour pendant ce temps (son entrée), puis la nuit tombe. */
const DUREE_JOUR_INITIAL = 1600;

export function PagePartie() {
  const code = useParams().code!.toUpperCase();
  const pseudo = useCompte((s) => s.pseudo)!;
  const { etat, entrer, quitterPartie } = useJeu();
  const navigate = useNavigate();

  // Action ciblée « armée » : un clic sur un joueur l'exécute (utile quand il y en a plusieurs, ex. vote / coup d'État).
  const [typeArme, setTypeArme] = useState<ActionType | null>(null);
  const [zoom, setZoom] = useState<Zoom>("non");
  const [jourInitial, setJourInitial] = useState(false);
  const aVuLeLobby = useRef(false);

  useEffect(() => {
    entrer(code, pseudo).then((ok) => { if (!ok) navigate("/", { replace: true }); });
  }, [code, pseudo, entrer, navigate]);

  const enLobby = etat?.phase === "Lobby" || etat?.phase === "Lancement";

  // Si le serveur lance la partie avant la fin de notre décompte (horloges décalées), on zoome quand même.
  // Un joueur qui revient sur une partie déjà commencée arrive directement au village.
  useEffect(() => {
    if (!etat) return;
    if (enLobby) aVuLeLobby.current = true;
    else if (aVuLeLobby.current) setZoom((z) => (z === "non" ? "en-cours" : z));
  }, [etat, enLobby]);

  useEffect(() => {
    if (zoom === "en-cours") {
      setJourInitial(true);
      const minuterie = setTimeout(() => setZoom("fini"), DUREE_ZOOM);
      return () => clearTimeout(minuterie);
    }
    if (zoom === "fini") {
      const minuterie = setTimeout(() => setJourInitial(false), DUREE_JOUR_INITIAL);
      return () => clearTimeout(minuterie);
    }
  }, [zoom]);

  if (!etat || etat.partieId !== code) {
    return <div className="chargement">Entrée dans la partie {code}…</div>;
  }

  const actionsCiblees = etat.actions.filter((a) => a.cibleRequise);
  const actionArmee = actionsCiblees.find((a) => a.type === typeArme) ?? actionsCiblees[0] ?? null;
  // Le lobby reste affiché pendant le zoom, et tant que le serveur n'a pas distribué les rôles.
  const afficherLobby = enLobby || zoom === "en-cours";

  async function quitter() {
    const enCours = etat!.phase !== "Lobby" && etat!.phase !== "Terminee";
    if (enCours && !confirm("Quitter la table ? La partie continue sans vous ; vous pourrez la reprendre depuis le salon.")) return;
    await quitterPartie();
    navigate("/");
  }

  return (
    <div className={"lg-root" + (afficherLobby && zoom !== "non" ? " is-zoom" : "")}>
      <header className="lg-head">
        <Marque />
        {afficherLobby && (
          <div className="lg-phase">
            <span className="lg-phase-t">Lobby</span>
            <span className="lg-phase-x">— Partie {etat.partieId}</span>
          </div>
        )}
        <button className="lg-link" onClick={quitter}>Quitter</button>
      </header>

      {afficherLobby ? (
        <Lobby etat={etat} zoom={zoom !== "non"} onDecompteFini={() => setZoom((z) => (z === "non" ? "en-cours" : z))} />
      ) : (
        <div className={"lg-body" + (zoom === "fini" ? " lb-game" : "")}>
          <div className="lg-stage">
            <Bandeau etat={etat} />
            <Village etat={etat} actionArmee={actionArmee} forcerJour={jourInitial} />
            <BarreActions etat={etat} actionArmee={actionArmee} onArmer={setTypeArme} onRetourSalon={quitter} />
          </div>
          <PanneauDroit etat={etat} />
        </div>
      )}
    </div>
  );
}
