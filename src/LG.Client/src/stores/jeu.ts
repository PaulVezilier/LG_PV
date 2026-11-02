import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import { HubConnectionState } from "@microsoft/signalr";
import { appeler, connexion } from "../hub";
import type { ActionType, Ambiance, ResumePartie, RoleId, Session, VuePartie } from "../types";

// L'état de la partie vient entièrement du serveur (VuePartie) : ce store ne fait que le stocker
// et exposer les appels au hub. Seule la session (la partie où l'on joue) est conservée entre deux rechargements ;
// le serveur reconnaît le joueur à son compte.

interface JeuState {
  session: Session | null;
  etat: VuePartie | null;
  parties: ResumePartie[];
  erreur: string | null;

  observerSalon: () => Promise<void>;
  quitterSalon: () => Promise<void>;
  /** Partie non terminée où l'on a une place, d'après le serveur (utile dans un nouvel onglet). */
  chercherPartieEnCours: () => Promise<void>;
  /** Crée une partie et renvoie son code (null en cas d'erreur). */
  creerPartie: () => Promise<string | null>;
  /** Rejoint la partie, ou y reprend sa place si on y est déjà. */
  entrer: (code: string) => Promise<boolean>;
  /** Oublie la partie suivie (déconnexion). */
  oublier: () => void;
  quitterPartie: () => Promise<void>;
  demarrer: () => Promise<void>;
  definirComposition: (roles: RoleId[] | null) => Promise<void>;
  definirAmbiance: (ambiance: Ambiance) => Promise<void>;
  agir: (type: ActionType, cibleId?: string | null) => Promise<void>;
  envoyerMessage: (texte: string) => Promise<void>;
  signalerErreur: (message: string | null) => void;
}

let salonObserve = false;
// Le serveur envoie la première vue avant de répondre à Creer/Rejoindre/Reprendre : on la garde de côté.
let derniereVue: VuePartie | null = null;
// Évite de rejoindre deux fois la même partie quand l'effet React est rejoué (StrictMode).
const entreesEnCours = new Map<string, Promise<boolean>>();

export const useJeu = create<JeuState>()(
  persist(
    (set, get) => {
      /** Exécute un appel au hub ; une erreur est affichée au joueur au lieu d'être levée. */
      async function tenter<T>(appel: () => Promise<T>): Promise<T | null> {
        try {
          return await appel();
        } catch (e) {
          set({ erreur: e instanceof Error ? e.message : "Erreur inconnue." });
          return null;
        }
      }

      function suivre(session: Session) {
        set({ session, etat: derniereVue?.partieId === session.partieId ? derniereVue : null });
      }

      async function quitterSessionActuelle() {
        if (!get().session) return;
        await tenter(() => appeler("QuitterPartie"));
        set({ session: null, etat: null });
      }

      return {
        session: null,
        etat: null,
        parties: [],
        erreur: null,

        observerSalon: async () => {
          salonObserve = true;
          const parties = await tenter(() => appeler<ResumePartie[]>("ObserverSalon"));
          if (parties) set({ parties });
        },

        quitterSalon: async () => {
          salonObserve = false;
          // Après une déconnexion, la connexion est coupée : inutile (et refusé) de la rouvrir pour ça.
          if (connexion.state !== HubConnectionState.Connected) return;
          await tenter(() => appeler("QuitterSalon"));
        },

        chercherPartieEnCours: async () => {
          const session = await tenter(() => appeler<Session | null>("PartieEnCours"));
          set({ session });
        },

        creerPartie: async () => {
          await quitterSessionActuelle();
          const session = await tenter(() => appeler<Session>("CreerPartie"));
          if (!session) return null;
          suivre(session);
          return session.partieId;
        },

        entrer: (code) => {
          const enCours = entreesEnCours.get(code);
          if (enCours) return enCours;

          const entree = (async () => {
            if (get().session?.partieId !== code) await quitterSessionActuelle();
            const session = await tenter(() => appeler<Session>("RejoindrePartie", code));
            if (session) suivre(session);
            else set({ session: null, etat: null });
            return !!session;
          })();
          entreesEnCours.set(code, entree);
          entree.finally(() => entreesEnCours.delete(code));
          return entree;
        },

        quitterPartie: async () => {
          const phase = get().etat?.phase;
          await tenter(() => appeler("QuitterPartie"));
          // Une partie lancée se poursuit sans nous : on garde la session pour pouvoir la reprendre.
          if (phase === "Lobby" || phase === "Terminee" || !phase) set({ session: null });
          set({ etat: null });
        },

        demarrer: async () => { await tenter(() => appeler("Demarrer")); },
        definirComposition: async (roles) => { await tenter(() => appeler("DefinirComposition", roles)); },
        definirAmbiance: async (ambiance) => { await tenter(() => appeler("DefinirAmbiance", ambiance)); },
        agir: async (type, cibleId = null) => { await tenter(() => appeler("Agir", type, cibleId)); },
        envoyerMessage: async (texte) => { await tenter(() => appeler("EnvoyerMessage", texte)); },
        oublier: () => set({ session: null, etat: null, parties: [] }),
        signalerErreur: (erreur) => set({ erreur }),
      };
    },
    {
      name: "lg.jeu",
      storage: createJSONStorage(() => sessionStorage),
      partialize: (etat) => ({ session: etat.session }),
    },
  ),
);

connexion.on("etat", (vue: VuePartie) => {
  derniereVue = vue;
  // Ignore une vue arrivée en retard d'une partie qu'on vient de quitter.
  if (vue.partieId === useJeu.getState().session?.partieId) useJeu.setState({ etat: vue });
});

connexion.on("parties", (parties: ResumePartie[]) => useJeu.setState({ parties }));

connexion.onreconnected(async () => {
  const { session, observerSalon } = useJeu.getState();
  if (session) await appeler("RejoindrePartie", session.partieId).catch(() => undefined);
  if (salonObserve) await observerSalon();
});
