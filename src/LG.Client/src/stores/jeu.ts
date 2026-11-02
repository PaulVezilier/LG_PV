import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import { appeler, connexion } from "../hub";
import type { ActionType, Ambiance, ResumePartie, RoleId, Session, VuePartie } from "../types";

// L'état de la partie vient entièrement du serveur (VuePartie) : ce store ne fait que le stocker
// et exposer les appels au hub. Seule la session (jeton de joueur) est conservée entre deux rechargements.

interface JeuState {
  session: Session | null;
  etat: VuePartie | null;
  parties: ResumePartie[];
  erreur: string | null;

  observerSalon: () => Promise<void>;
  quitterSalon: () => Promise<void>;
  /** Crée une partie et renvoie son code (null en cas d'erreur). */
  creerPartie: (pseudo: string) => Promise<string | null>;
  /** Reprend la partie si on y a déjà une session, sinon la rejoint. */
  entrer: (code: string, pseudo: string) => Promise<boolean>;
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
          await tenter(() => appeler("QuitterSalon"));
        },

        creerPartie: async (pseudo) => {
          await quitterSessionActuelle();
          const session = await tenter(() => appeler<Session>("CreerPartie", pseudo));
          if (!session) return null;
          suivre(session);
          return session.partieId;
        },

        entrer: (code, pseudo) => {
          const enCours = entreesEnCours.get(code);
          if (enCours) return enCours;

          const entree = (async () => {
            const actuelle = get().session;
            if (actuelle?.partieId === code) {
              const reprise = await tenter(() => appeler<Session | null>("Reprendre", actuelle.jeton));
              if (reprise) {
                suivre(reprise);
                return true;
              }
              set({ session: null, etat: null, erreur: "Cette partie n'existe plus." });
              return false;
            }
            await quitterSessionActuelle();
            const session = await tenter(() => appeler<Session>("RejoindrePartie", code, pseudo));
            if (session) suivre(session);
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
  if (session) await appeler("Reprendre", session.jeton).catch(() => undefined);
  if (salonObserve) await observerSalon();
});
