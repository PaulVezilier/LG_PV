// Pictogrammes des maquettes (trait 24 × 24, couleur héritée du texte).

const trait = { fill: "none", stroke: "currentColor", strokeLinecap: "round", strokeLinejoin: "round" } as const;

export function IconeLune({ taille = 20 }: { taille?: number }) {
  return (
    <svg width={taille} height={taille} viewBox="0 0 24 24" strokeWidth="1.8" {...trait} aria-hidden>
      <path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5Z" />
    </svg>
  );
}

export function IconeCartes() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" strokeWidth="1.7" {...trait} aria-hidden>
      <rect x="4" y="5" width="11" height="15" rx="1.5" />
      <path d="M9 4.5 L18 3 L20.5 17 L16 18" />
    </svg>
  );
}

export function IconeInviter() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" strokeWidth="1.7" {...trait} aria-hidden>
      <circle cx="9" cy="8" r="3.5" />
      <path d="M2.5 20c1-3.6 3.4-5.5 6.5-5.5s5.5 1.9 6.5 5.5M18 8v6M15 11h6" />
    </svg>
  );
}

export function IconeLancer() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
      <path d="M7 4.5v15l12-7.5z" />
    </svg>
  );
}

export function IconeEnvoyer() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" strokeWidth="2.2" {...trait} aria-hidden>
      <path d="M5 12h13M13 6l6 6-6 6" />
    </svg>
  );
}

export function IconeCouronne() {
  return (
    <svg viewBox="0 0 24 24" width="66%" height="66%" fill="currentColor" aria-hidden>
      <path d="M3 8l4.5 4L12 5l4.5 7L21 8l-2 10H5z" />
    </svg>
  );
}

/** Silhouette « place libre » des sièges vides du lobby. */
export function IconePlaceLibre() {
  return (
    <svg viewBox="0 0 100 100" width="62%" height="62%" aria-hidden>
      <circle cx="50" cy="34" r="17" fill="#3a3f52" />
      <path d="M16 96 Q18 60 50 58 Q82 60 84 96 Z" fill="#3a3f52" />
      <circle cx="50" cy="78" r="13" fill="#1a120b" stroke="#b8954a" strokeWidth="2.5" />
      <path d="M50 71 L50 85 M43 78 L57 78" stroke="#efe2c4" strokeWidth="3" strokeLinecap="round" />
    </svg>
  );
}

/** Les quatre ornements dorés aux coins des scènes. */
export function CoinsDores() {
  return (
    <>
      {["tl", "tr", "bl", "br"].map((coin) => (
        <svg key={coin} className={`lg-corner ${coin}`} viewBox="0 0 40 40" aria-hidden>
          <path d="M4 4 L36 4 M4 4 L4 36 M10 10 L24 10 M10 10 L10 24 M10 10 L16 16" stroke="#c8a24a" strokeWidth="2" fill="none" />
          <circle cx="18" cy="18" r="3.5" fill="#c8a24a" />
        </svg>
      ))}
    </>
  );
}

export function Marque() {
  return (
    <div className="lg-brand">
      <span className="lg-brand-lune"><IconeLune taille={26} /></span>
      <span className="lg-word">Loup-Garou</span>
    </div>
  );
}
