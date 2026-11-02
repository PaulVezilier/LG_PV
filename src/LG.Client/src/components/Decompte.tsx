import { useEffect, useRef, useState } from "react";

const secondesRestantes = (fin: string) => Math.ceil((new Date(fin).getTime() - Date.now()) / 1000);

/** 3… 2… 1… au centre de la scène, puis prévient quand le décompte est terminé. */
export function Decompte({ fin, onFini }: { fin: string; onFini: () => void }) {
  const [reste, setReste] = useState(() => secondesRestantes(fin));
  const surFin = useRef(onFini);
  surFin.current = onFini;

  useEffect(() => {
    const minuterie = setInterval(() => {
      const r = secondesRestantes(fin);
      setReste(r);
      if (r <= 0) {
        clearInterval(minuterie);
        surFin.current();
      }
    }, 100);
    return () => clearInterval(minuterie);
  }, [fin]);

  if (reste <= 0) return null;
  return (
    <div className="decompte" role="timer" aria-live="assertive">
      <span className="decompte-titre">La partie commence</span>
      {/* La clé relance l'animation à chaque seconde. */}
      <span key={reste} className="decompte-chiffre">{reste}</span>
    </div>
  );
}
