import { icone, NOMS_CAMP } from "../roles";
import { useJeu } from "../stores/jeu";
import type { RoleId, VuePartie, VueRole } from "../types";
import { Infobulle } from "./Infobulle";

/** Panneau de droite : ma carte de rôle, puis le récapitulatif des rôles de la partie. */
export function PanneauDroit({ etat }: { etat: VuePartie }) {
  const { moi } = etat;
  return (
    <aside className="panneau-droit">
      <section className={"carte carte-role" + (moi.estVivant ? "" : " morte")}>
        <div className="illustration" aria-hidden>{moi.role ? icone(moi.role) : "❔"}</div>
        <h3>{moi.role ?? "Rôle secret"}</h3>
        {moi.camp && <span className={`camp camp-${moi.camp}`}>Camp : {NOMS_CAMP[moi.camp]}</span>}
        <p>{moi.descriptionRole ?? "Votre rôle vous sera révélé au lancement de la partie."}</p>
        {!moi.estVivant && <p className="ruban-mort">Vous êtes mort</p>}
      </section>
      <RecapRoles etat={etat} />
    </aside>
  );
}

function RecapRoles({ etat }: { etat: VuePartie }) {
  const definirComposition = useJeu((s) => s.definirComposition);
  const enLobby = etat.phase === "Lobby";
  const modifiable = etat.compositionModifiable;

  /** Envoie la composition entière (un rôle par joueur) après avoir ajouté ou retiré un exemplaire. */
  function modifier(roleId: RoleId, delta: number) {
    const roles: RoleId[] = [];
    for (const role of etat.composition) {
      const nombre = Math.max(0, role.nombre + (role.id === roleId ? delta : 0));
      for (let i = 0; i < nombre; i++) roles.push(role.id);
    }
    definirComposition(roles);
  }

  const total = etat.composition.reduce((somme, r) => somme + r.nombre, 0);

  return (
    <section className="carte recap-roles">
      <h3>
        {enLobby ? "Composition" : "Rôles en jeu"}
        {enLobby && <span className="discret"> · {total} rôle{total > 1 ? "s" : ""}{etat.compositionAutomatique ? " (auto)" : ""}</span>}
        {modifiable && !etat.compositionAutomatique && (
          <button className="bouton-discret" title="Revenir à la composition automatique" onClick={() => definirComposition(null)}>
            Auto
          </button>
        )}
      </h3>
      <ul>
        {etat.composition.map((role) => (
          <LigneRole key={role.id} role={role} enLobby={enLobby} modifiable={modifiable} onModifier={modifier} />
        ))}
      </ul>
    </section>
  );
}

interface LigneRoleProps {
  role: VueRole;
  enLobby: boolean;
  modifiable: boolean;
  onModifier: (roleId: RoleId, delta: number) => void;
}

function LigneRole({ role, enLobby, modifiable, onModifier }: LigneRoleProps) {
  // En jeu : « Loup-Garou x2 », puis « x1 » quand l'un meurt, grisé quand il n'en reste plus.
  const nombre = enLobby ? role.nombre : role.vivants;
  const classes = ["ligne-role", `camp-${role.camp}`];
  if (nombre === 0) classes.push("eteint");

  return (
    <li className={classes.join(" ")}>
      <span className="ligne-role-icone">{icone(role.nom)}</span>
      <span className="ligne-role-nom">{role.nom}</span>
      <Infobulle titre={role.nom} texte={role.description} />
      {modifiable ? (
        <span className="compteur-role">
          <button className="bouton-mini" disabled={role.nombre === 0} onClick={() => onModifier(role.id, -1)}>−</button>
          <span className="nombre">{role.nombre}</span>
          <button className="bouton-mini" onClick={() => onModifier(role.id, +1)}>+</button>
        </span>
      ) : (
        <span className="nombre">x{nombre}</span>
      )}
    </li>
  );
}
