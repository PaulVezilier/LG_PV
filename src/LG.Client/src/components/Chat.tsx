import { useLayoutEffect, useRef, useState, type FormEvent } from "react";
import { useJeu } from "../stores/jeu";
import type { Canal, VueEvenement, VuePartie } from "../types";
import { personnage } from "../village";
import { IconeEnvoyer } from "./Icones";

/** Titre de l'onglet du chat selon le canal où l'on parle. */
export function titreChat(canal: Canal | null): string {
  switch (canal) {
    case "Loups": return "Meute";
    case "Morts": return "Morts";
    default: return "Village";
  }
}

const PLACEHOLDERS: Record<Canal, string> = {
  Public: "Écrire au village…",
  Loups: "Murmurer à la meute…",
  Morts: "Parler aux morts…",
  Prive: "",
};

/** Le chat et les annonces de la partie ; le canal (village, meute, morts) est décidé par le serveur. */
export function Chat({ etat }: { etat: VuePartie }) {
  const canal = etat.canalChat;
  // Pour l'avatar d'un message : le personnage de son auteur.
  const indexParNom = new Map(etat.joueurs.map((j, i) => [j.nom, i]));

  return (
    <>
      <Fil evenements={etat.journal} moi={etat.moi.nom} indexParNom={indexParNom} />
      <Saisie canal={canal} />
    </>
  );
}

function Fil({ evenements, moi, indexParNom }: { evenements: VueEvenement[]; moi: string; indexParNom: Map<string, number> }) {
  const liste = useRef<HTMLOListElement>(null);
  const collerEnBas = useRef(true);

  // Reste collé au dernier message, sauf si le joueur est remonté lire l'historique.
  useLayoutEffect(() => {
    const el = liste.current;
    if (el && collerEnBas.current) el.scrollTop = el.scrollHeight;
  }, [evenements]);

  function suivreDefilement() {
    const el = liste.current!;
    collerEnBas.current = el.scrollHeight - el.scrollTop - el.clientHeight < 30;
  }

  return (
    <ol className="lg-chat-list" ref={liste} onScroll={suivreDefilement}>
      {evenements.map((e) => {
        if (!e.auteur)
          return <li key={e.numero} className={`lg-annonce canal-${e.canal}`}>{e.texte}</li>;
        const index = indexParNom.get(e.auteur);
        return (
          <li key={e.numero} className={`lg-msg canal-${e.canal}` + (e.auteur === moi ? " is-me" : "")}>
            <div className="lg-msg-body">
              <span className="lg-msg-name">{e.auteur}</span>
              <p className="lg-msg-text">{e.texte}</p>
            </div>
            <span className="lg-msg-av" aria-hidden>
              {index !== undefined && <img className="lg-msg-fig" src={personnage(index)} alt="" />}
            </span>
          </li>
        );
      })}
    </ol>
  );
}

function Saisie({ canal }: { canal: Canal | null }) {
  const envoyerMessage = useJeu((s) => s.envoyerMessage);
  const [texte, setTexte] = useState("");

  function envoyer(e: FormEvent) {
    e.preventDefault();
    if (!texte.trim()) return;
    envoyerMessage(texte);
    setTexte("");
  }

  return (
    <form className="lg-chat-form" onSubmit={envoyer}>
      <input
        className="lg-chat-input"
        value={texte}
        onChange={(e) => setTexte(e.target.value)}
        maxLength={300}
        disabled={!canal}
        placeholder={canal ? PLACEHOLDERS[canal] : "Le village dort… silence."}
        aria-label="Message"
      />
      <button className="lg-chat-send" disabled={!canal || !texte.trim()} aria-label="Envoyer le message"><IconeEnvoyer /></button>
    </form>
  );
}
