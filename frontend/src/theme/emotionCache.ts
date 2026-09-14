import createCache from "@emotion/cache";
import rtlPlugin from "stylis-plugin-rtl";
import { prefixer } from "stylis";

export function createEmotionCache(direction: "ltr" | "rtl") {
  return direction === "rtl"
    ? createCache({ key: "muirtl", stylisPlugins: [prefixer, rtlPlugin] })
    : createCache({ key: "muiltr" });
}
