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
      // Mobile safety net: nothing in the app should ever be wide enough to cause horizontal
      // page scroll, but if something briefly is (e.g. a layout shift while a font loads), a
      // stray horizontal scrollbar throws off every fixed-position overlay's (Dialog/Menu
      // backdrop, Popover) scroll-lock math, which is what made backdrops render oddly on phones.
      MuiCssBaseline: {
        styleOverrides: {
          "html, body": { overflowX: "hidden", maxWidth: "100%" },
          // On a phone there's no room for the 32px margin Dialog uses by default — let it
          // use nearly the full viewport width/height instead of a small centered box.
          "@media (max-width: 600px)": {
            ".MuiDialog-paper": { margin: 12, width: "calc(100% - 24px)", maxHeight: "calc(100% - 24px)" },
          },
        },
      },
    },
  });
}
