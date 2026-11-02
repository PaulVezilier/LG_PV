// Miroir des objets envoyés par le serveur (LG.Domain/Vues/VuePartie.cs, LG.Web/GestionnaireParties.cs).

export type Phase = "Lobby" | "Lancement" | "Nuit" | "Jour" | "DernieresVolontes" | "Terminee";
export type Ambiance = "Classique" | "Halloween" | "Hiver";
export type Camp = "Village" | "LoupsGarous" | "Amoureux";
export type Canal = "Public" | "Loups" | "Morts" | "Prive";
export type ActionType =
  | "Voter" | "Proteger" | "Sonder" | "Infecter" | "Soigner" | "Empoisonner"
  | "Unir" | "Heriter" | "CoupDEtat" | "Tirer" | "Designer" | "Passer";
export type RoleId = string;

export interface Session {
  jeton: string;
  partieId: string;
  joueurId: string;
}

export interface ResumePartie {
  id: string;
  hote: string;
  nombreJoueurs: number;
  joueursMaximum: number;
  phase: Phase;
  numeroTour: number;
}

export interface ActionDisponible {
  type: ActionType;
  libelle: string;
  cibleRequise: boolean;
  cibles: string[];
}

export interface VueJoueur {
  id: string;
  nom: string;
  estVivant: boolean;
  estHote: boolean;
  role: string | null;
  estLoupAllie: boolean;
  estAmoureux: boolean;
  voix: number;
  aVote: boolean;
}

export interface VueMoi {
  id: string;
  nom: string;
  estVivant: boolean;
  estHote: boolean;
  role: string | null;
  descriptionRole: string | null;
  camp: Camp | null;
}

export interface VueEvenement {
  numero: number;
  date: string;
  canal: Canal;
  texte: string;
  auteur: string | null;
}

export interface VueRole {
  id: RoleId;
  nom: string;
  camp: Camp;
  description: string;
  nombre: number;
  vivants: number;
}

export interface VuePartie {
  partieId: string;
  phase: Phase;
  numeroTour: number;
  ambiance: Ambiance;
  narration: string;
  consigne: string | null;
  monVote: string | null;
  finPhase: string | null;
  vainqueur: Camp | null;
  peutDemarrer: boolean;
  obstacleAuDemarrage: string | null;
  compositionModifiable: boolean;
  compositionAutomatique: boolean;
  composition: VueRole[];
  canalChat: Canal | null;
  moi: VueMoi;
  joueurs: VueJoueur[];
  actions: ActionDisponible[];
  journal: VueEvenement[];
}
