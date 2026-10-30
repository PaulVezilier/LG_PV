import { useState } from "react";

interface Position {
  droite: number;
  milieu: number;
}

/**
 * Petit « ? » qui ouvre une bulle au survol (ou au focus clavier).
 * La bulle est en position fixe, à gauche du « ? », pour ne pas être coupée par un panneau qui défile.
 */
export function Infobulle({ titre, texte }: { titre: string; texte: string }) {
  const [position, setPosition] = useState<Position | null>(null);

  function ouvrir(e: { currentTarget: HTMLElement }) {
    const rect = e.currentTarget.getBoundingClientRect();
    setPosition({ droite: window.innerWidth - rect.left + 8, milieu: rect.top + rect.height / 2 });
  }

  return (
    <>
      <button
        type="button"
        className="infobulle-declencheur"
        aria-label={`Description : ${titre}`}
        onMouseEnter={ouvrir}
        onMouseLeave={() => setPosition(null)}
        onFocus={ouvrir}
        onBlur={() => setPosition(null)}
      >
        ?
      </button>
      {position && (
        <div className="infobulle" role="tooltip" style={{ right: position.droite, top: position.milieu }}>
          <strong>{titre}</strong>
          <p>{texte}</p>
        </div>
      )}
    </>
  );
}
