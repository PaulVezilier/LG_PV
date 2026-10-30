import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// En dev, Vite sert le front sur :5173 et relaie le hub SignalR vers le serveur .NET (:5080).
// En build, le front est généré dans le wwwroot du serveur .NET, qui le sert seul.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/hub": { target: "http://localhost:5080", ws: true },
    },
  },
  build: {
    outDir: "../LG.Web/wwwroot",
    emptyOutDir: true,
  },
});
