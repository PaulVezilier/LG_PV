import { create } from "zustand";
import { redemarrerConnexion } from "../hub";

// Le compte connecté. La session elle-même est un cookie HttpOnly que JavaScript ne peut pas lire :
// au chargement de la page, on demande au serveur qui l'on est.

export interface Utilisateur {
  id: string;
  pseudo: string;
}

interface CompteState {
  utilisateur: Utilisateur | null;
  /** Vrai une fois la première réponse de /api/compte/moi reçue. */
  charge: boolean;
  charger: () => Promise<void>;
  /** Renvoie un message d'erreur, ou null si la connexion a réussi. */
  connecter: (identifiant: string, motDePasse: string, seSouvenir: boolean) => Promise<string | null>;
  inscrire: (pseudo: string, email: string, motDePasse: string) => Promise<string | null>;
  deconnecter: () => Promise<void>;
}

async function envoyer(url: string, corps?: unknown): Promise<Response> {
  return fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: corps === undefined ? undefined : JSON.stringify(corps),
  });
}

/** Message lisible d'une réponse d'erreur (ProblemDetails du serveur). */
async function messageErreur(reponse: Response): Promise<string> {
  if (reponse.status === 429) return "Trop de tentatives : réessayez dans une minute.";
  try {
    const probleme = await reponse.json();
    if (probleme?.detail) return probleme.detail;
  } catch { /* corps vide ou non JSON */ }
  return "Le serveur n'a pas pu traiter la demande.";
}

export const useCompte = create<CompteState>()((set) => {
  /** Après connexion ou déconnexion, le hub doit se reconnecter avec le nouveau cookie. */
  async function changerUtilisateur(utilisateur: Utilisateur | null) {
    await redemarrerConnexion();
    set({ utilisateur });
  }

  async function authentifier(url: string, corps: unknown): Promise<string | null> {
    try {
      const reponse = await envoyer(url, corps);
      if (!reponse.ok) return await messageErreur(reponse);
      await changerUtilisateur(await reponse.json());
      return null;
    } catch {
      return "Impossible de joindre le serveur.";
    }
  }

  return {
    utilisateur: null,
    charge: false,

    charger: async () => {
      try {
        const reponse = await fetch("/api/compte/moi");
        set({ utilisateur: reponse.ok ? await reponse.json() : null, charge: true });
      } catch {
        set({ utilisateur: null, charge: true });
      }
    },

    connecter: (identifiant, motDePasse, seSouvenir) => authentifier("/api/compte/connexion", { identifiant, motDePasse, seSouvenir }),
    inscrire: (pseudo, email, motDePasse) => authentifier("/api/compte/inscription", { pseudo, email, motDePasse }),

    deconnecter: async () => {
      await envoyer("/api/compte/deconnexion").catch(() => undefined);
      await changerUtilisateur(null);
    },
  };
});
