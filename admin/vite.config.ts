import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  // Pinned (not Vite's default 5173, which frontend/ already uses) so this origin can be
  // reliably allow-listed in the backend's CORS config for local dev — see
  // appsettings.Development.json.
  server: { port: 5174 },
})
