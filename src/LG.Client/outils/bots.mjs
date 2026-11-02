// Fait rejoindre une partie par des joueurs automatiques, pour tester seul.
// Usage, depuis src/LG.Client, le serveur .NET étant lancé : node outils/bots.mjs <CODE> [nombre de bots]
// Les comptes Bot1, Bot2... sont créés au premier lancement (base de développement uniquement).
// Chaque bot agit au hasard dès qu'une action lui est proposée.
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const SERVEUR = process.env.LG_SERVEUR ?? "http://localhost:5080";
const MOT_DE_PASSE = process.env.LG_BOTS_MOT_DE_PASSE ?? "bot-de-test-2026";

const [code, nombre = "4"] = process.argv.slice(2);
if (!code) {
  console.error("Usage : node outils/bots.mjs <CODE> [nombre de bots]");
  process.exit(1);
}

async function poster(url, corps) {
  return fetch(SERVEUR + url, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(corps) });
}

/** Connecte le compte (en le créant au besoin) et renvoie son cookie de session. */
async function ouvrirSession(pseudo) {
  // Le serveur limite les connexions à 10 par minute et par adresse IP : on attend que la fenêtre se libère.
  while (true) {
    let reponse = await poster("/api/compte/connexion", { identifiant: pseudo, motDePasse: MOT_DE_PASSE, seSouvenir: false });
    if (reponse.status === 401)
      reponse = await poster("/api/compte/inscription", { pseudo, email: `${pseudo.toLowerCase()}@bots.test`, motDePasse: MOT_DE_PASSE });
    if (reponse.ok) return reponse.headers.getSetCookie().map((c) => c.split(";")[0]).join("; ");
    if (reponse.status !== 429) throw new Error(`${pseudo} : ${reponse.status} ${await reponse.text()}`);
    console.log(`${pseudo} : trop de connexions, nouvel essai dans une minute…`);
    await new Promise((fin) => setTimeout(fin, 61_000));
  }
}

const auHasard = (liste) => liste[Math.floor(Math.random() * liste.length)];

async function lancerBot(pseudo) {
  const cookie = await ouvrirSession(pseudo);
  const connexion = new HubConnectionBuilder()
    .withUrl(`${SERVEUR}/hub/partie`, { headers: { Cookie: cookie } })
    .configureLogging(LogLevel.Warning)
    .build();

  // Une seule tentative par action et par moment de la partie.
  const dejaFait = new Set();
  connexion.on("etat", (vue) => {
    if (vue.phase === "Terminee") {
      console.log(`${pseudo} : partie terminée`);
      connexion.stop();
      return;
    }
    for (const action of vue.actions) {
      const cle = `${vue.phase}-${vue.numeroTour}-${action.type}`;
      if (action.type === "Passer" || dejaFait.has(cle)) continue;
      const cibles = action.cibles.filter((id) => id !== vue.moi.id);
      if (action.cibleRequise && cibles.length === 0) continue;
      dejaFait.add(cle);
      const cible = action.cibleRequise ? auHasard(cibles) : null;
      // Petit délai, pour que la partie reste lisible dans le navigateur.
      setTimeout(() => connexion.invoke("Agir", action.type, cible).catch(() => undefined), 500 + Math.random() * 1500);
      break;
    }
  });

  await connexion.start();
  await connexion.invoke("RejoindrePartie", code.toUpperCase());
  console.log(`${pseudo} a rejoint ${code.toUpperCase()}`);
}

for (let i = 1; i <= Number(nombre); i++) await lancerBot(`Bot${i}`);
