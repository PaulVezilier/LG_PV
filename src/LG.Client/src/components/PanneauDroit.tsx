import { useEffect, useState } from "react";
import { NOMS_CAMP } from "../roles";
import type { VuePartie } from "../types";
import { personnage } from "../village";
import { Chat, titreChat } from "./Chat";
import { RecapRoles } from "./RecapRoles";

type Onglet = "chat" | "role";

/** Colonne de droite : le chat sur toute la hauteur, et en second onglet mon rôle et les rôles en jeu. */
export function PanneauDroit({ etat }: { etat: VuePartie }) {
  const [onglet, setOnglet] = useState<Onglet>("chat");
  // Dernier message lu : signale ce qui arrive pendant qu'on consulte son rôle.
  const dernier = etat.journal.at(-1)?.numero ?? 0;
  const [lu, setLu] = useState(dernier);
  useEffect(() => { if (onglet === "chat") setLu(dernier); }, [onglet, dernier]);

  const canal = etat.canalChat;

  return (
    <aside className="lg-side">
      <section className={"lg-chat-in" + (canal ? ` canal-${canal}` : "")}>
        <div className="lg-onglets" role="tablist">
          <button role="tab" aria-selected={onglet === "chat"} className="lg-onglet" onClick={() => setOnglet("chat")}>
            {titreChat(canal)}
            {onglet !== "chat" && dernier > lu && <span className="lg-non-lu" aria-label="Nouveaux messages" />}
          </button>
          <button role="tab" aria-selected={onglet === "role"} className="lg-onglet" onClick={() => setOnglet("role")}>
            Mon rôle
          </button>
        </div>
        {/* Le chat reste monté (caché) pour garder le message en cours de saisie. */}
        <div className="lg-onglet-corps" hidden={onglet !== "chat"}>
          <Chat etat={etat} />
        </div>
        {onglet === "role" && (
          <div className="lg-onglet-corps lg-role">
            <CarteRole etat={etat} />
            <h3 className="lg-sous-titre">Rôles en jeu</h3>
            <RecapRoles etat={etat} />
          </div>
        )}
      </section>
    </aside>
  );
}

function CarteRole({ etat }: { etat: VuePartie }) {
  const { moi } = etat;
  const monIndex = etat.joueurs.findIndex((j) => j.id === moi.id);

  return (
    <section className={"carte-role" + (moi.estVivant ? "" : " morte")}>
      <span className="carte-role-av" aria-hidden>
        <img className="lg-msg-fig" src={personnage(Math.max(0, monIndex))} alt="" />
      </span>
      <div className="carte-role-texte">
        <h2>{moi.role ?? "Rôle secret"}</h2>
        {moi.camp && <span className={`camp camp-${moi.camp}`}>Camp : {NOMS_CAMP[moi.camp]}</span>}
        {!moi.estVivant && <span className="ruban-mort">Vous êtes mort</span>}
      </div>
      {moi.descriptionRole && <p className="carte-role-desc">{moi.descriptionRole}</p>}
    </section>
  );
}
