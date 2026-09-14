import { createTheme } from "@mui/material/styles";
import type { Direction } from "@mui/material/styles";

/**
 * Calm, WhatsApp-simple visual language: a single accent color, generous
 * whitespace, no dense banking-style tables (BRD "not a complicated
 * banking-style dashboard").
 */
export function createDourakTheme(direction: Direction) {
  return createTheme({
    direction,
    palette: {
      mode: "light",
      primary: { main: "#1F8A70" }, // calm green — money/trust without looking like a bank
      secondary: { main: "#F2A541" },
      background: { default: "#FAF9F6" },
    },
    shape: { borderRadius: 12 },
    typography: {
      fontFamily: direction === "rtl"
        ? '"Tajawal", "Segoe UI", Tahoma, sans-serif'
        : '"Inter", "Segoe UI", Roboto, sans-serif',
    },
    components: {
      MuiButton: { defaultProps: { disableElevation: true } },
      MuiCard: { styleOverrides: { root: { boxShadow: "0 1px 4px rgba(0,0,0,0.08)" } } },
    },
  });
}
