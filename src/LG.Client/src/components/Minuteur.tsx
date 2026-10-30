import { useEffect, useState } from "react";

/** Temps restant avant la fin de la phase (suppose l'horloge du navigateur à l'heure du serveur). */
export function Minuteur({ fin }: { fin: string | null }) {
  const [maintenant, setMaintenant] = useState(() => Date.now());

  useEffect(() => {
    if (!fin) return;
    const minuterie = setInterval(() => setMaintenant(Date.now()), 250);
    return () => clearInterval(minuterie);
  }, [fin]);

  if (!fin) return null;
  const reste = Math.max(0, Math.ceil((new Date(fin).getTime() - maintenant) / 1000));
  return (
    <span className={"minuteur" + (reste <= 10 ? " urgent" : "")}>
      {Math.floor(reste / 60)}:{String(reste % 60).padStart(2, "0")}
    </span>
  );
}
