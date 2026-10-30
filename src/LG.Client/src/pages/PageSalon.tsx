import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router";
import { titrePhase } from "../roles";
import { useCompte } from "../stores/compte";
import { useJeu } from "../stores/jeu";
import type { ResumePartie } from "../types";

/** Salon public : toutes les parties en attente ou en cours, rejoignables d'un clic. */
export function PageSalon() {
  const { pseudo, deconnecter } = useCompte();
  const { parties, session, observerSalon, quitterSalon, creerPartie } = useJeu();
  const [code, setCode] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    observerSalon();
    return () => { quitterSalon(); };
  }, [observerSalon, quitterSalon]);

  const enAttente = parties.filter((p) => p.phase === "Lobby");
  const enCours = parties.filter((p) => p.phase !== "Lobby");

  async function creer() {
    const id = await creerPartie(pseudo!);
    if (id) navigate(`/partie/${id}`);
  }

  function rejoindreParCode(e: FormEvent) {
    e.preventDefault();
    if (code.trim()) navigate(`/partie/${code.trim().toUpperCase()}`);
  }

  return (
    <div className="page-salon">
      <header className="barre">
        <span className="marque">Loup-Garou</span>
        <span className="barre-espace" />
        <span className="discret">Connecté en tant que <strong>{pseudo}</strong></span>
        <button className="bouton-discret" onClick={deconnecter}>Se déconnecter</button>
      </header>

      <main className="salon">
        <section className="carte salon-accueil">
          <h1>Le salon du village</h1>
          <p className="discret">Rejoignez une partie en attente ou créez la vôtre : elle apparaîtra ici pour tout le monde.</p>
          <div className="salon-actions">
            <button className="bouton-principal" onClick={creer}>Créer une partie</button>
            <form className="code-rapide" onSubmit={rejoindreParCode}>
              <input value={code} onChange={(e) => setCode(e.target.value)} placeholder="Code" maxLength={5} />
              <button className="bouton-secondaire" disabled={!code.trim()}>Rejoindre</button>
            </form>
          </div>
          {session && (
            <div className="reprise">
              Vous avez une partie en cours ({session.partieId}).
              <button className="bouton-secondaire" onClick={() => navigate(`/partie/${session.partieId}`)}>Y retourner</button>
            </div>
          )}
        </section>

        <ListeParties
          titre="En attente de joueurs"
          vide="Aucune partie en attente. Créez la première !"
          parties={enAttente}
          maPartie={session?.partieId}
          onRejoindre={(id) => navigate(`/partie/${id}`)}
        />
        <ListeParties
          titre="Parties en cours"
          vide="Aucune partie en cours."
          parties={enCours}
          maPartie={session?.partieId}
          onRejoindre={(id) => navigate(`/partie/${id}`)}
        />
      </main>
    </div>
  );
}

interface ListePartiesProps {
  titre: string;
  vide: string;
  parties: ResumePartie[];
  maPartie?: string;
  onRejoindre: (id: string) => void;
}

function ListeParties({ titre, vide, parties, maPartie, onRejoindre }: ListePartiesProps) {
  return (
    <section className="liste-parties">
      <h2>{titre} <span className="compteur">{parties.length}</span></h2>
      {parties.length === 0 && <p className="discret">{vide}</p>}
      <ul>
        {parties.map((p) => {
          const lobby = p.phase === "Lobby";
          const mienne = p.id === maPartie;
          const complete = p.nombreJoueurs >= p.joueursMaximum;
          return (
            <li key={p.id} className="carte ligne-partie">
              <div className="ligne-partie-titre">
                <strong>Partie de {p.hote}</strong>
                <span className="code">{p.id}</span>
              </div>
              <div className="discret">
                {p.nombreJoueurs}/{p.joueursMaximum} joueurs · {titrePhase(p.phase, p.numeroTour)}
              </div>
              {mienne ? (
                <button className="bouton-principal" onClick={() => onRejoindre(p.id)}>Y retourner</button>
              ) : lobby ? (
                <button className="bouton-principal" disabled={complete} onClick={() => onRejoindre(p.id)}>
                  {complete ? "Complète" : "Rejoindre"}
                </button>
              ) : (
                <button className="bouton-secondaire" disabled title="Le mode spectateur arrivera plus tard">Déjà commencée</button>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
