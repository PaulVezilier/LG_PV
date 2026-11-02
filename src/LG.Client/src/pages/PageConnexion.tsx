import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import { IconeLune } from "../components/Icones";
import { useCompte } from "../stores/compte";

export function PageConnexion() {
  const { pseudo: connecte, connecter } = useCompte();
  const [pseudo, setPseudo] = useState("");
  const navigate = useNavigate();
  const depuis = (useLocation().state as { depuis?: string } | null)?.depuis ?? "/";

  if (connecte) return <Navigate to={depuis} replace />;

  const valide = pseudo.trim().length > 0;

  function entrer(e: FormEvent) {
    e.preventDefault();
    if (!valide) return;
    connecter(pseudo);
    navigate(depuis, { replace: true });
  }

  return (
    <div className="page-connexion">
      <form className="carte carte-connexion" onSubmit={entrer}>
        <div className="embleme"><IconeLune taille={52} /></div>
        <h1>Loup-Garou</h1>
        <p className="sous-titre">La nuit tombe sur le village…</p>

        <label className="champ">
          <span>Pseudo</span>
          <input
            autoFocus
            maxLength={20}
            value={pseudo}
            onChange={(e) => setPseudo(e.target.value)}
            placeholder="Votre nom au village"
          />
        </label>

        <label className="champ">
          <span>Mot de passe <em>— bientôt</em></span>
          <input type="password" disabled placeholder="Disponible avec les comptes joueurs" />
        </label>

        <button className="bouton-principal" disabled={!valide}>Entrer au village</button>
        <p className="discret">Les comptes arriveront avec la base de données. Pour l'instant, un pseudo suffit.</p>
      </form>
    </div>
  );
}
