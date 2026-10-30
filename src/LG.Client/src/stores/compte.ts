import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

// Connexion provisoire : seul le pseudo est retenu, tant qu'il n'y a pas de base de données.
// sessionStorage (et non localStorage) : chaque onglet est un joueur différent, pratique pour tester seul.

interface CompteState {
  pseudo: string | null;
  connecter: (pseudo: string) => void;
  deconnecter: () => void;
}

export const useCompte = create<CompteState>()(
  persist(
    (set) => ({
      pseudo: null,
      connecter: (pseudo) => set({ pseudo: pseudo.trim() }),
      deconnecter: () => set({ pseudo: null }),
    }),
    { name: "lg.compte", storage: createJSONStorage(() => sessionStorage) },
  ),
);
