import type { Camp, Phase } from "./types";

// Icônes provisoires, en attendant de vraies illustrations. Les variantes de loups prennent l'icône du loup.
const ICONES: Record<string, string> = {
  "Simple Villageois": "🧑‍🌾",
  "Sorcière": "🧪",
  "Voyante": "🔮",
  "Garde": "🛡️",
  "Chasseur": "🏹",
  "Cupidon": "💘",
  "Dictateur": "👑",
  "Héritier": "📜",
  "Chaperon Rouge": "🧣",
  "Croque-Mort": "⚰️",
  "Vétéran": "🎖️",
};

export function icone(nomRole: string): string {
  return ICONES[nomRole.replace(/ infecté$/, "")] ?? (nomRole.includes("Loup") ? "🐺" : "❔");
}

export const NOMS_CAMP: Record<Camp, string> = {
  Village: "Village",
  LoupsGarous: "Loups-Garous",
  Amoureux: "Amoureux",
};

export function titrePhase(phase: Phase, tour: number): string {
  switch (phase) {
    case "Lobby": return "Salle d'attente";
    case "Nuit": return `Nuit ${tour}`;
    case "Jour": return `Jour ${tour}`;
    case "DernieresVolontes": return "Dernières volontés";
    case "Terminee": return "Partie terminée";
  }
}
