import { icone } from "../roles";
import { useJeu } from "../stores/jeu";
import type { RoleId, VuePartie, VueRole } from "../types";
import { Infobulle } from "./Infobulle";

/**
 * Rôles de la partie. Dans le lobby, l'hôte règle la composition (+/−) ;
 * en jeu, « Loup-Garou x2 » devient « x1 » quand l'un meurt, et s'éteint à zéro.
 */
export function RecapRoles({ etat }: { etat: VuePartie }) {
  const definirComposition = useJeu((s) => s.definirComposition);
  const enLobby = etat.phase === "Lobby" || etat.phase === "Lancement";
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
    <div className="recap-roles">
      {enLobby && (
        <p className="recap-entete">
          <span>{total} rôle{total > 1 ? "s" : ""} pour {etat.joueurs.length} joueur{etat.joueurs.length > 1 ? "s" : ""}{etat.compositionAutomatique ? " · automatique" : ""}</span>
          {modifiable && !etat.compositionAutomatique && (
            <button className="bouton-discret" title="Revenir à la composition automatique" onClick={() => definirComposition(null)}>
              Auto
            </button>
          )}
        </p>
      )}
      <ul>
        {etat.composition.map((role) => (
          <LigneRole key={role.id} role={role} enLobby={enLobby} modifiable={modifiable} onModifier={modifier} />
        ))}
      </ul>
    </div>
  );
}

interface LigneRoleProps {
  role: VueRole;
  enLobby: boolean;
  modifiable: boolean;
  onModifier: (roleId: RoleId, delta: number) => void;
}

function LigneRole({ role, enLobby, modifiable, onModifier }: LigneRoleProps) {
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
          <button className="bouton-mini" disabled={role.nombre === 0} onClick={() => onModifier(role.id, -1)} aria-label={`Retirer un ${role.nom}`}>−</button>
          <span className="nombre">{role.nombre}</span>
          <button className="bouton-mini" onClick={() => onModifier(role.id, +1)} aria-label={`Ajouter un ${role.nom}`}>+</button>
        </span>
      ) : (
        <span className="nombre">x{nombre}</span>
      )}
    </li>
  );
}
