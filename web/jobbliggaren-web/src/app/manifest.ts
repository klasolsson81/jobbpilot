import type { MetadataRoute } from "next";

// Next.js 16 file convention: Web App Manifest. Background_color vit (matchar
// landing-light). theme_color = granskogsgrön #15603F (matchar grön-accent-identiteten
// ADR 0068 + sigill-logon, logo-översyn 2026-06-13) — ersätter tidigare navy.
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "Jobbliggaren",
    short_name: "Jobbliggaren",
    description: "Den svenska jobbansökningshanteraren",
    start_url: "/",
    display: "standalone",
    background_color: "#FFFFFF",
    theme_color: "#15603F",
    lang: "sv",
    icons: [
      {
        src: "/icon.svg",
        type: "image/svg+xml",
        sizes: "any",
      },
      {
        src: "/apple-icon",
        type: "image/png",
        sizes: "180x180",
      },
    ],
  };
}
