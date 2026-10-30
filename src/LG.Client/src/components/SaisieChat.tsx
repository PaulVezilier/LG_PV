import { useState, type FormEvent } from "react";
import { useJeu } from "../stores/jeu";
import type { Canal } from "../types";

const PLACEHOLDERS: Record<Canal, string> = {
  Public: "Parler au village…",
  Loups: "Murmurer à la meute…",
  Morts: "Parler aux morts…",
  Prive: "",
};

/** Champ de saisie en bas de l'écran ; le canal (village, meute, morts) est décidé par le serveur. */
export function SaisieChat({ canal }: { canal: Canal | null }) {
  const envoyerMessage = useJeu((s) => s.envoyerMessage);
  const [texte, setTexte] = useState("");

  function envoyer(e: FormEvent) {
    e.preventDefault();
    if (!texte.trim()) return;
    envoyerMessage(texte);
    setTexte("");
  }

  return (
    <form className={"saisie-chat" + (canal ? ` canal-${canal}` : "")} onSubmit={envoyer}>
      <input
        value={texte}
        onChange={(e) => setTexte(e.target.value)}
        maxLength={300}
        disabled={!canal}
        placeholder={canal ? PLACEHOLDERS[canal] : "Le village dort… silence."}
      />
      <button className="bouton-principal" disabled={!canal || !texte.trim()}>Envoyer</button>
    </form>
  );
}
