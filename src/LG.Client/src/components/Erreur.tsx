import { useEffect } from "react";
import { useJeu } from "../stores/jeu";

/** Message d'erreur du serveur, affiché quelques secondes en bas de l'écran. */
export function Erreur() {
  const erreur = useJeu((s) => s.erreur);
  const signalerErreur = useJeu((s) => s.signalerErreur);

  useEffect(() => {
    if (!erreur) return;
    const minuterie = setTimeout(() => signalerErreur(null), 4000);
    return () => clearTimeout(minuterie);
  }, [erreur, signalerErreur]);

  if (!erreur) return null;
  return <div className="toast-erreur" role="alert" onClick={() => signalerErreur(null)}>{erreur}</div>;
}
