import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import { IconeLune } from "../components/Icones";
import { useCompte } from "../stores/compte";

type Mode = "connexion" | "inscription";

export function PageConnexion() {
  const { utilisateur, charge, connecter, inscrire } = useCompte();
  const [mode, setMode] = useState<Mode>("connexion");
  const [identifiant, setIdentifiant] = useState("");
  const [email, setEmail] = useState("");
  const [motDePasse, setMotDePasse] = useState("");
  const [seSouvenir, setSeSouvenir] = useState(true);
  const [erreur, setErreur] = useState<string | null>(null);
  const [envoi, setEnvoi] = useState(false);
  const navigate = useNavigate();
  const depuis = (useLocation().state as { depuis?: string } | null)?.depuis ?? "/";

  if (!charge) return <div className="chargement">Ouverture des portes du village…</div>;
  if (utilisateur) return <Navigate to={depuis} replace />;

  const inscription = mode === "inscription";
  const valide = identifiant.trim().length > 0 && motDePasse.length > 0 && (!inscription || email.trim().length > 0);

  function changerMode(nouveau: Mode) {
    setMode(nouveau);
    setErreur(null);
  }

  async function valider(e: FormEvent) {
    e.preventDefault();
    if (!valide || envoi) return;
    setEnvoi(true);
    const message = inscription
      ? await inscrire(identifiant.trim(), email.trim(), motDePasse)
      : await connecter(identifiant.trim(), motDePasse, seSouvenir);
    setEnvoi(false);
    if (message) setErreur(message);
    else navigate(depuis, { replace: true });
  }

  return (
    <div className="page-connexion">
      <form className="carte carte-connexion" onSubmit={valider}>
        <div className="embleme"><IconeLune taille={52} /></div>
        <h1>Loup-Garou</h1>
        <p className="sous-titre">La nuit tombe sur le village…</p>

        <div className="lg-onglets" role="tablist">
          <button type="button" role="tab" className="lg-onglet" aria-selected={!inscription} onClick={() => changerMode("connexion")}>Connexion</button>
          <button type="button" role="tab" className="lg-onglet" aria-selected={inscription} onClick={() => changerMode("inscription")}>Inscription</button>
        </div>

        <label className="champ">
          <span>{inscription ? "Pseudo" : "Pseudo ou e-mail"}</span>
          <input
            autoFocus
            maxLength={inscription ? 20 : 256}
            autoComplete="username"
            value={identifiant}
            onChange={(e) => setIdentifiant(e.target.value)}
            placeholder={inscription ? "Votre nom au village" : ""}
          />
        </label>

        {inscription && (
          <label className="champ">
            <span>E-mail</span>
            <input type="email" autoComplete="email" maxLength={256} value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
        )}

        <label className="champ">
          <span>Mot de passe{inscription && <em> — 10 caractères minimum, dont un chiffre</em>}</span>
          <input
            type="password"
            autoComplete={inscription ? "new-password" : "current-password"}
            value={motDePasse}
            onChange={(e) => setMotDePasse(e.target.value)}
          />
        </label>

        {!inscription && (
          <label className="case-a-cocher">
            <input type="checkbox" checked={seSouvenir} onChange={(e) => setSeSouvenir(e.target.checked)} />
            Rester connecté
          </label>
        )}

        {erreur && <p className="erreur-formulaire" role="alert">{erreur}</p>}

        <button className="bouton-principal" disabled={!valide || envoi}>
          {inscription ? "Créer mon compte" : "Entrer au village"}
        </button>
      </form>
    </div>
  );
}
