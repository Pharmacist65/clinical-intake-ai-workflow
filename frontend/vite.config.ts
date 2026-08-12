import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => ({
  base: mode === "demo" ? "/clinical-intake-ai-workflow/" : "/",
  plugins: [react()],
  server: {
    port: 5173
  },
  preview: {
    port: 4173
  },
  build: {
    chunkSizeWarningLimit: 550
  }
}));
