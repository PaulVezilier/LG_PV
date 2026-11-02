// Importe les SVG de la direction artistique dans public/ :
//   node outils/importer-decors.mjs <dossier loup-garou-svg> <fichier Lobby.dc.html>
//
// - Plateaux (6 ambiances jour/nuit) : on retire les 16 villageois dessinés en dur (et leurs étiquettes),
//   puisque le front place lui-même les vrais joueurs aux mêmes emplacements ; on anime les feux et la fumée.
// - Table du lobby : extraite de la maquette Lobby.dc.html, déclinée pour chaque ambiance.
// - Personnages : copiés tels quels, avec des noms de fichier sans accent.
import fs from "node:fs";
import path from "node:path";

const [dossierSvg, maquetteLobby] = process.argv.slice(2);
if (!dossierSvg || !maquetteLobby) {
  console.error("Usage : node outils/importer-decors.mjs <dossier loup-garou-svg> <Lobby.dc.html>");
  process.exit(1);
}

const publicDir = path.resolve(import.meta.dirname, "..", "public");
const decors = path.join(publicDir, "decors");
const personnages = path.join(publicDir, "personnages");
fs.mkdirSync(decors, { recursive: true });
fs.mkdirSync(personnages, { recursive: true });

// Animations des éléments marqués d'une classe (fumée, chandelles, reflets) : elles fonctionnent aussi dans un <img>.
const ANIMATIONS = `<style>
.smoke{transform-box:fill-box;transform-origin:50% 100%;animation:sway 9s ease-in-out infinite}.s2{animation-duration:7s}.s3{animation-duration:11s}
@keyframes sway{0%,100%{transform:skewX(0deg) scaleY(1)}50%{transform:skewX(-5deg) scaleY(1.03)}}
.flame{transform-box:fill-box;transform-origin:50% 100%;animation:flicker 1.6s ease-in-out infinite}
.f2{animation-duration:1.2s;animation-delay:-.4s}.f3{animation-duration:.9s;animation-delay:-.2s}
@keyframes flicker{0%,100%{transform:scale(1,1) skewX(0deg)}25%{transform:scale(.96,1.06) skewX(-2deg)}50%{transform:scale(1.03,.95) skewX(1.5deg)}75%{transform:scale(.98,1.04) skewX(-1deg)}}
.ember{animation:rise 3.5s linear infinite;opacity:0}
@keyframes rise{0%{transform:translate(0,40px);opacity:0}15%{opacity:1}100%{transform:translate(10px,-110px);opacity:0}}
.shimmer{animation:shimmer 6s ease-in-out infinite}.shimmer.s2{animation-duration:8s;animation-delay:-3s}
@keyframes shimmer{0%,100%{opacity:1}50%{opacity:.72}}
</style>`;

/** Enfants directs de l'élément qui commence à `debut` (index de son « < ») : liste de [début, fin]. */
function enfants(svg, debut) {
  const re = /<(\/?)([a-zA-Z]+)([^>]*?)(\/?)>/g;
  re.lastIndex = svg.indexOf(">", debut) + 1;
  const resultat = [];
  let profondeur = 0, ouverture = 0, m;
  while ((m = re.exec(svg))) {
    if (m[1]) {
      if (profondeur === 0) break; // fermeture de l'élément parent
      if (--profondeur === 0) resultat.push([ouverture, re.lastIndex]);
    } else if (m[4]) {
      if (profondeur === 0) resultat.push([m.index, re.lastIndex]);
    } else {
      if (profondeur === 0) ouverture = m.index;
      profondeur++;
    }
  }
  return resultat;
}

function ajouterAnimations(svg) {
  return svg.replace(/(<svg[^>]*>)/, `$1${ANIMATIONS}`);
}

// Flammes du feu de camp et de la maison en ruine, reconnues au début de leur tracé (comme dans la maquette
// « Plateau de jeu », qui les anime) ; des braises montent au-dessus de la dernière flamme de chaque feu.
const FLAMMES = [
  { debut: "M676 644 Q662 600", classe: "flame" },
  { debut: "M692 644 Q684 606", classe: "flame f2" },
  { debut: "M706 644 Q702 616", classe: "flame f3", braises: [[708, 560, 0], [730, 560, 0.7], [718, 560, 1.4], [742, 560, 2.1], [700, 560, 2.8]] },
  { debut: "M1040 470 Q1032 440", classe: "flame f2" },
  { debut: "M1052 470 Q1050 448", classe: "flame f3", braises: [[1062, 400, 0.3], [1080, 400, 1.5], [1070, 400, 2.4]] },
];

function animerFeux(svg, nom) {
  for (const { debut, classe, braises } of FLAMMES) {
    const ouverture = `<path d="${debut} `;
    const position = svg.indexOf(ouverture);
    if (position < 0) throw new Error(`${nom} : flamme « ${debut} » introuvable.`);
    svg = svg.slice(0, position) + `<path class="${classe}" d="${debut} ` + svg.slice(position + ouverture.length);
    if (!braises) continue;
    const fin = svg.indexOf("</path>", position) + "</path>".length;
    const cercles = braises.map(([cx, cy, delai]) =>
      `<circle class="ember" style="animation-delay:${delai}s" cx="${cx}" cy="${cy}" r="2.4" fill="#ffb040" stroke="none"></circle>`).join("");
    svg = svg.slice(0, fin) + cercles + svg.slice(fin);
  }
  return svg;
}

// ---------------------------------------------------------------- Plateaux

for (const ambiance of ["classique", "halloween", "hiver"]) {
  for (const moment of ["jour", "nuit"]) {
    const nom = `plateau-${ambiance}-${moment}.svg`;
    const svg = fs.readFileSync(path.join(dossierSvg, nom), "utf8");

    // Le groupe du village est l'enfant de <svg> qui contient les étiquettes des villageois.
    const racine = svg.indexOf("<svg");
    const village = enfants(svg, racine).find(([a, b]) => svg.slice(a, b).includes("<text"));
    const aRetirer = enfants(svg, village[0]).filter(([a, b]) => {
      const el = svg.slice(a, b);
      return el.startsWith("<text")
        || /^<rect [^>]*width="72" height="20" rx="10"/.test(el)
        || /^<g transform="translate\([\d.]+ [\d.]+\) scale\([\d.]+\)"><g stroke="#1a130e" stroke-width="1.4"/.test(el);
    });
    if (aRetirer.length !== 48)
      throw new Error(`${nom} : 16 villageois attendus (48 éléments), ${aRetirer.length} trouvés.`);

    let sortie = svg;
    for (const [a, b] of aRetirer.reverse()) sortie = sortie.slice(0, a) + sortie.slice(b);
    fs.writeFileSync(path.join(decors, nom), ajouterAnimations(animerFeux(sortie, nom)));
    console.log(`${nom} : ${(sortie.length / 1024).toFixed(0)} Ko`);
  }
}

// ---------------------------------------------------------------- Table du lobby

const lobby = fs.readFileSync(maquetteLobby, "utf8");
const debutTable = lobby.indexOf('<svg class="lg-layer" viewBox="0 0 1600 900"');
const finTable = lobby.indexOf("</svg>", lobby.indexOf(">", debutTable)) + "</svg>".length;
if (debutTable < 0) throw new Error("Table du lobby introuvable dans la maquette.");
const table = lobby.slice(debutTable, finTable)
  .replace(/^<svg class="lg-layer"/, '<svg xmlns="http://www.w3.org/2000/svg"')
  .replace(/ aria-hidden="true"/, "");

const LUNE = { classique: "#e8d8b0", halloween: "#c0503a", hiver: "#e8d8b0" };
for (const ambiance of ["classique", "halloween", "hiver"]) {
  const svg = table
    .replaceAll("{{moonFill}}", LUNE[ambiance])
    .replaceAll("{{winterD}}", ambiance === "hiver" ? "inline" : "none")
    .replaceAll("{{halloweenD}}", ambiance === "halloween" ? "inline" : "none");
  if (svg.includes("{{")) throw new Error("Variable de maquette non remplacée dans la table du lobby.");
  fs.writeFileSync(path.join(decors, `lobby-${ambiance}.svg`), ajouterAnimations(svg));
}
console.log(`lobby-*.svg : ${(table.length / 1024).toFixed(0)} Ko`);

// ---------------------------------------------------------------- Personnages

// Une archive décompressée sous Windows peut abîmer les accents (« léonce » devient « l├®once ») :
// on remplace alors la séquence abîmée par un « e », la seule lettre accentuée des prénoms.
const sansAccent = (s) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[^\x00-\x7f]+/g, "e");
const fichiers = fs.readdirSync(dossierSvg).filter((f) => f.startsWith("personnage-") && f !== "personnage-calques.svg");
for (const f of fichiers) fs.copyFileSync(path.join(dossierSvg, f), path.join(personnages, sansAccent(f).replace("personnage-", "")));
console.log(`${fichiers.length} personnages copiés`);
