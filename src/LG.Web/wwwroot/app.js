"use strict";

// L'état complet vient du serveur (VuePartie) : le client ne fait qu'afficher et envoyer des actions.

const CLE_SESSION = "lg.session";
const LIBELLES_PHASE = { Lobby: "Salle d'attente", Nuit: "Nuit", Jour: "Jour", Terminee: "Partie terminée" };
const ICONES_ROLE = {
  "Simple Villageois": "🧑‍🌾", "Loup-Garou": "🐺", "Loup-Garou Noir": "🐺", "Sorcière": "🧪", "Voyante": "🔮", "Garde": "🛡️",
};

const $ = (id) => document.getElementById(id);
let etat = null;

const connexion = new signalR.HubConnectionBuilder()
  .withUrl("/hub/partie")
  .withAutomaticReconnect()
  .build();

connexion.on("etat", (vue) => { etat = vue; afficher(); });
connexion.onreconnected(() => reprendre());

// ------------------------------------------------------------------ Session

function lireSession() {
  try { return JSON.parse(localStorage.getItem(CLE_SESSION)); } catch { return null; }
}

function ecrireSession(session) {
  try {
    if (session) localStorage.setItem(CLE_SESSION, JSON.stringify(session));
    else localStorage.removeItem(CLE_SESSION);
  } catch { /* stockage indisponible : la partie sera perdue au rafraîchissement */ }
}

async function reprendre() {
  const session = lireSession();
  if (!session) return;
  const valide = await appeler("Reprendre", session.jeton);
  if (!valide) { ecrireSession(null); etat = null; afficher(); }
}

// ------------------------------------------------------------------ Appels serveur

async function appeler(methode, ...args) {
  try {
    return await connexion.invoke(methode, ...args);
  } catch (e) {
    afficherErreur(e.message.replace(/^.*HubException: /, ""));
    return null;
  }
}

let minuterieErreur;
function afficherErreur(message) {
  const zone = $("erreur");
  zone.textContent = message;
  zone.hidden = false;
  clearTimeout(minuterieErreur);
  minuterieErreur = setTimeout(() => { zone.hidden = true; }, 4000);
}

$("btn-creer").onclick = async () => {
  const session = await appeler("CreerPartie", $("pseudo").value);
  if (session) ecrireSession(session);
};

$("btn-rejoindre").onclick = async () => {
  const session = await appeler("RejoindrePartie", $("code").value, $("pseudo").value);
  if (session) ecrireSession(session);
};

$("btn-demarrer").onclick = () => appeler("Demarrer");

$("btn-quitter").onclick = () => { ecrireSession(null); etat = null; afficher(); };

$("form-chat").onsubmit = async (e) => {
  e.preventDefault();
  const texte = $("message").value;
  if (!texte.trim()) return;
  $("message").value = "";
  await appeler("EnvoyerMessage", texte);
};

const agir = (type, cibleId = null) => appeler("Agir", type, cibleId);

// ------------------------------------------------------------------ Affichage

function element(balise, classe, texte) {
  const el = document.createElement(balise);
  if (classe) el.className = classe;
  if (texte !== undefined) el.textContent = texte;
  return el;
}

function afficher() {
  $("ecran-accueil").hidden = !!etat;
  $("ecran-partie").hidden = !etat;
  $("btn-quitter").hidden = !etat;
  if (!etat) { $("infos-partie").textContent = ""; return; }

  document.body.dataset.phase = etat.phase;
  $("infos-partie").textContent = `Code de la partie : ${etat.partieId} — vous jouez ${etat.moi.nom}`;
  afficherPhase();
  afficherJoueurs();
  afficherRole();
  afficherActions();
  afficherJournal();
  afficherChat();
}

function afficherPhase() {
  let titre = LIBELLES_PHASE[etat.phase];
  if (etat.phase === "Nuit" || etat.phase === "Jour") titre += ` ${etat.numeroTour}`;
  if (etat.phase === "Terminee") {
    titre += etat.vainqueur === "Village" ? " — victoire du village"
      : etat.vainqueur === "LoupsGarous" ? " — victoire des loups" : " — égalité";
  }
  $("titre-phase").textContent = titre;
  $("etape").textContent = etat.etape ?? (etat.phase === "Nuit" ? "Le village dort…" : "");
  $("btn-demarrer").hidden = !etat.peutDemarrer;
}

function afficherJoueurs() {
  const liste = $("liste-joueurs");
  liste.replaceChildren();
  const actionsCiblees = etat.actions.filter((a) => a.cibleRequise);

  for (const joueur of etat.joueurs) {
    const li = element("li", joueur.estVivant ? "" : "mort");
    li.append(element("span", "nom", joueur.nom + (joueur.id === etat.moi.id ? " (vous)" : "")));
    if (joueur.estHote) li.append(element("span", "badge", "hôte"));
    if (joueur.estLoupAllie) li.append(element("span", "badge loup", "loup"));
    if (joueur.role) li.append(element("span", "badge", joueur.role));
    if (joueur.voix > 0) li.append(element("span", "badge voix", `${joueur.voix} voix`));
    if (joueur.aVote) li.append(element("span", "badge", "a voté"));

    // Les actions ciblées s'affichent directement sur la ligne du joueur visé.
    for (const action of actionsCiblees.filter((a) => a.cibles.includes(joueur.id))) {
      const bouton = element("button", "", action.libelle);
      bouton.onclick = () => agir(action.type, joueur.id);
      li.append(bouton);
    }
    liste.append(li);
  }
}

function afficherRole() {
  const moi = etat.moi;
  $("illustration-role").textContent = moi.role ? (ICONES_ROLE[moi.role.replace(/ infecté$/, "")] ?? "?") : "?";
  $("nom-role").textContent = moi.role ? moi.role + (moi.estVivant ? "" : " (mort)") : "Rôle attribué au lancement";
  $("description-role").textContent = moi.descriptionRole ?? "";
}

function afficherActions() {
  const zone = $("actions");
  zone.replaceChildren();
  for (const action of etat.actions) {
    if (action.cibleRequise) continue;
    const bouton = element("button", "", action.libelle);
    bouton.onclick = () => agir(action.type);
    zone.append(bouton);
  }
  if (etat.actions.some((a) => a.cibleRequise)) {
    zone.append(element("p", "discret", "Choisissez votre cible dans la liste des joueurs."));
  }
  if (etat.actions.length === 0) {
    zone.append(element("p", "discret", "Aucune action pour le moment."));
  }
}

function afficherJournal() {
  const journal = $("journal");
  const enBas = journal.scrollHeight - journal.scrollTop - journal.clientHeight < 30;
  journal.replaceChildren();
  for (const evenement of etat.journal) {
    const li = element("li", evenement.canal);
    if (evenement.auteur) li.append(element("span", "auteur", evenement.auteur + " :"));
    li.append(document.createTextNode(evenement.texte));
    journal.append(li);
  }
  if (enBas) journal.scrollTop = journal.scrollHeight;
}

function afficherChat() {
  const moi = etat.moi;
  const champ = $("message");
  let canal = "Message au village";
  let autorise = true;
  if (etat.phase === "Nuit" || etat.phase === "Jour") {
    if (!moi.estVivant) canal = "Message aux morts";
    else if (etat.phase === "Nuit" && moi.camp === "LoupsGarous") canal = "Message à la meute";
    else if (etat.phase === "Nuit") { canal = "Le village dort…"; autorise = false; }
  }
  champ.placeholder = canal;
  champ.disabled = !autorise;
}

setInterval(() => {
  if (!etat?.finPhase) { $("minuteur").textContent = ""; return; }
  const reste = Math.max(0, Math.ceil((new Date(etat.finPhase) - Date.now()) / 1000));
  $("minuteur").textContent = `${Math.floor(reste / 60)}:${String(reste % 60).padStart(2, "0")}`;
}, 250);

// ------------------------------------------------------------------ Démarrage

connexion.start()
  .then(reprendre)
  .catch(() => afficherErreur("Connexion au serveur impossible."));
