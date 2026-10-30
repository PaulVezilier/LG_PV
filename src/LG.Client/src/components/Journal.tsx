import { useLayoutEffect, useRef } from "react";
import type { VueEvenement } from "../types";

/** Historique du chat et des annonces, posé en transparence au centre de la table. */
export function Journal({ evenements }: { evenements: VueEvenement[] }) {
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
    <ol className="journal" ref={liste} onScroll={suivreDefilement}>
      {evenements.map((e) => (
        <li key={e.numero} className={`message canal-${e.canal}` + (e.auteur ? "" : " annonce")}>
          {e.auteur && <span className="auteur">{e.auteur}</span>}
          {e.texte}
        </li>
      ))}
    </ol>
  );
}
