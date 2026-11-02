import type { Ambiance } from "./types";

// Décors et emplacements tirés des maquettes (« Lobby → lancement de la partie », « Plateau de jeu »).
// Les SVG sont dans public/, importés par outils/importer-decors.mjs.

/** Les 16 villageois dessinés : le n-ième joueur de la partie (ordre d'arrivée) prend le n-ième personnage. */
const PERSONNAGES = [
  "aldric", "bertille", "come", "douce", "elouan", "faustine", "gaspard", "hermine",
  "isaure", "jehan", "leonce", "mahaut", "norbert", "oriane", "perceval", "quitterie",
];

export const personnage = (index: number) => `/personnages/${PERSONNAGES[index % PERSONNAGES.length]}.svg`;

export const decorLobby = (ambiance: Ambiance) => `/decors/lobby-${ambiance.toLowerCase()}.svg`;

export const decorVillage = (ambiance: Ambiance, nuit: boolean) =>
  `/decors/plateau-${ambiance.toLowerCase()}-${nuit ? "nuit" : "jour"}.svg`;

export const AMBIANCES: Ambiance[] = ["Classique", "Halloween", "Hiver"];

// ---------------------------------------------------------------- Lobby

export interface SiegeLobby {
  /** Centre du médaillon et sa largeur, en % de la scène 16:9 (et en mode portrait sur mobile : px, py, pw). */
  lx: number; ly: number; lw: number;
  px: number; py: number; pw: number;
  /** Plaque du pseudo : position et ancrage (CSS transform). */
  plx: number; ply: number; plt: string;
}

const CENTRE = "translate(-50%, -50%)";
const DROITE = "translate(0, -50%)";
const GAUCHE = "translate(-100%, -50%)";

/** 16 places autour de la table, dans le sens des aiguilles d'une montre depuis le haut. */
export const SIEGES_LOBBY: SiegeLobby[] = [
  { lx: 44.15, ly: 27.26, lw: 7, px: 39.97, py: 26.11, pw: 11.67, plx: 44.15, ply: 33.49, plt: CENTRE },
  { lx: 55.85, ly: 27.26, lw: 7, px: 60.03, py: 26.11, pw: 11.67, plx: 55.85, ply: 33.49, plt: CENTRE },
  { lx: 66.67, ly: 31.91, lw: 7, px: 80.08, py: 26.11, pw: 11.67, plx: 66.67, ply: 38.13, plt: CENTRE },
  { lx: 74.94, ly: 40.49, lw: 7, px: 90.97, py: 33.44, pw: 11.67, plx: 78.94, ply: 40.49, plt: DROITE },
  { lx: 79.42, ly: 51.71, lw: 7, px: 90.97, py: 49.48, pw: 11.67, plx: 83.42, ply: 51.71, plt: DROITE },
  { lx: 79.42, ly: 63.85, lw: 7, px: 90.97, py: 65.52, pw: 11.67, plx: 83.42, ply: 63.85, plt: DROITE },
  { lx: 74.94, ly: 75.06, lw: 7, px: 90.97, py: 81.56, pw: 11.67, plx: 78.94, ply: 75.06, plt: DROITE },
  { lx: 66.67, ly: 83.65, lw: 7, px: 80.08, py: 88.89, pw: 11.67, plx: 66.67, ply: 89.87, plt: CENTRE },
  { lx: 55.85, ly: 88.29, lw: 7, px: 60.03, py: 88.89, pw: 11.67, plx: 55.85, ply: 94.51, plt: CENTRE },
  { lx: 44.15, ly: 88.29, lw: 7, px: 39.97, py: 88.89, pw: 11.67, plx: 44.15, ply: 94.51, plt: CENTRE },
  { lx: 33.33, ly: 83.65, lw: 7, px: 19.92, py: 88.89, pw: 11.67, plx: 33.33, ply: 89.87, plt: CENTRE },
  { lx: 25.06, ly: 75.06, lw: 7, px: 9.03, py: 81.56, pw: 11.67, plx: 21.06, ply: 75.06, plt: GAUCHE },
  { lx: 20.58, ly: 63.85, lw: 7, px: 9.03, py: 65.52, pw: 11.67, plx: 16.58, ply: 63.85, plt: GAUCHE },
  { lx: 20.58, ly: 51.71, lw: 7, px: 9.03, py: 49.48, pw: 11.67, plx: 16.58, ply: 51.71, plt: GAUCHE },
  { lx: 25.06, ly: 40.49, lw: 7, px: 9.03, py: 33.44, pw: 11.67, plx: 21.06, ply: 40.49, plt: GAUCHE },
  { lx: 33.33, ly: 31.91, lw: 7, px: 19.92, py: 26.11, pw: 11.67, plx: 33.33, ply: 38.13, plt: CENTRE },
];

// ---------------------------------------------------------------- Village

/** Emplacement d'un villageois sur le décor 1440 × 900 : centre, pieds, et échelle du personnage (100 × 240). */
type Emplacement = [x: number, pieds: number, echelle: number];

/** Jusqu'à 10 joueurs : villageois plus grands, répartis sur toute la place. */
const VILLAGE_10: Emplacement[] = [
  [543.1, 559.3, 0.697], [1354.6, 648.5, 0.795], [527.5, 856.3, 1.025], [110.5, 647.6, 0.794], [916.1, 862.3, 1.031],
  [918.9, 576.2, 0.715], [389.3, 720.4, 0.875], [1066.7, 743.8, 0.901], [1213.1, 555.5, 0.692], [249.5, 556.9, 0.694],
];

/** De 11 à 16 joueurs : trois rangées, celles du décor d'origine. */
const VILLAGE_16: Emplacement[] = [
  [545, 500, 0.416], [1330, 886, 0.698], [110, 886, 0.698], [1320, 503, 0.419], [1000, 680, 0.548], [205, 680, 0.548],
  [540, 886, 0.698], [120, 503, 0.419], [900, 886, 0.698], [895, 500, 0.416], [440, 680, 0.548], [1235, 680, 0.548],
  [325, 886, 0.698], [1115, 886, 0.698], [330, 506, 0.421], [1110, 506, 0.421],
];

export interface PlaceVillage {
  /** Coin haut-gauche et largeur du personnage, en % de la scène. */
  gauche: number; haut: number; largeur: number;
  /** Plus un villageois est bas sur l'image, plus il est devant. */
  profondeur: number;
  /** Ancre de l'étiquette du pseudo (au-dessus de la tête). */
  etiquetteX: number; etiquetteY: number;
}

export function placesVillage(nombre: number): PlaceVillage[] {
  return (nombre > VILLAGE_10.length ? VILLAGE_16 : VILLAGE_10).slice(0, nombre).map(([x, pieds, echelle]) => {
    const largeur = 100 * echelle, hauteur = 240 * echelle;
    return {
      gauche: ((x - largeur / 2) / 1440) * 100,
      haut: ((pieds - hauteur) / 900) * 100,
      largeur: (largeur / 1440) * 100,
      profondeur: Math.round(pieds),
      etiquetteX: (x / 1440) * 100,
      etiquetteY: ((pieds - hauteur) / 900) * 100,
    };
  });
}
