import { HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";

/** Connexion unique au hub SignalR du serveur, partagée par toute l'application. */
export const connexion = new HubConnectionBuilder()
  .withUrl("/hub/partie")
  .withAutomaticReconnect()
  .build();

let demarrage: Promise<void> | null = null;

function assurerConnexion(): Promise<void> {
  if (connexion.state === HubConnectionState.Connected) return Promise.resolve();
  if (!demarrage || connexion.state === HubConnectionState.Disconnected) {
    demarrage = connexion.start().catch((e) => {
      demarrage = null;
      throw e;
    });
  }
  return demarrage;
}

/** Coupe la connexion : la prochaine sera ouverte avec le cookie du compte nouvellement connecté. */
export async function redemarrerConnexion(): Promise<void> {
  demarrage = null;
  await connexion.stop();
}

/** Appelle une méthode du hub. Les erreurs de jeu (HubException) remontent avec leur message lisible. */
export async function appeler<T = void>(methode: string, ...args: unknown[]): Promise<T> {
  await assurerConnexion();
  try {
    return await connexion.invoke<T>(methode, ...args);
  } catch (e) {
    const message = e instanceof Error ? e.message : String(e);
    throw new Error(message.replace(/^.*HubException: /, ""));
  }
}
