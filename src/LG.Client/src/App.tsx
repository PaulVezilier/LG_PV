import { useEffect } from "react";
import { BrowserRouter, Navigate, Outlet, Route, Routes, useLocation } from "react-router";
import { Erreur } from "./components/Erreur";
import { PageConnexion } from "./pages/PageConnexion";
import { PagePartie } from "./pages/PagePartie";
import { PageSalon } from "./pages/PageSalon";
import { useCompte } from "./stores/compte";

export function App() {
  const charger = useCompte((s) => s.charger);
  useEffect(() => { charger(); }, [charger]);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/connexion" element={<PageConnexion />} />
        <Route element={<RouteProtegee />}>
          <Route path="/" element={<PageSalon />} />
          <Route path="/partie/:code" element={<PagePartie />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      <Erreur />
    </BrowserRouter>
  );
}

/** Renvoie vers la connexion, en retenant la page demandée (un lien de partie partagé, par exemple). */
function RouteProtegee() {
  const { utilisateur, charge } = useCompte();
  const location = useLocation();
  if (!charge) return <div className="chargement">Ouverture des portes du village…</div>;
  if (!utilisateur) return <Navigate to="/connexion" replace state={{ depuis: location.pathname }} />;
  return <Outlet />;
}
