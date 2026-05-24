"use client";

import { useRouter } from "next/navigation";
import { ArrowRight } from "lucide-react";
import { AuthCard } from "./auth-card";

/**
 * LandingHeroSection — navy hero med copy-kolumn vänster + AuthCard höger
 * (HANDOVER §7.1 punkt 2).
 *
 * Klient-island eftersom CTA-knapparna använder `useRouter`. Per Klas-direktiv
 * 2026-05-24 (Steg 5 closed-beta-disciplin) är "Skapa konto"-CTA borttagen.
 * Två CTA:er återstår: "Anmäl till väntelista" (→ `/vantelista`) och
 * "Utforska som gäst".
 *
 * F-Pre Punkt 5 (2026-05-24 — CTO Beslut 2): "Utforska som gäst" leder till
 * `/gast/oversikt` istället för `/jobb` så middleware inte redirectar till
 * login. För inloggade besökare ändras CTA till "Till översikt" → `/oversikt`
 * (anonym-only-disciplin — ingen "växla till demo"-toggle för inloggade,
 * CTO YAGNI + civic-utility-motivering).
 *
 * Civic-utility-disciplin: inga Sparkles-ikoner, inga gradient-bg, inga
 * trust-pills. CTA-ikon (ArrowRight) är funktionell `lucide-react` monogram.
 */
export function LandingHeroSection({
  isAuthenticated,
}: {
  isAuthenticated: boolean;
}) {
  const router = useRouter();

  return (
    <section className="jp-land-hero">
      <div className="jp-land-hero__inner">
        <div className="jp-land-hero__copy">
          <h1 className="jp-land-hero__title">
            Håll ordning i ditt jobbsökande
          </h1>
          <p className="jp-land-hero__lede">
            Sök bland aktiva annonser från Platsbanken, skapa CV och brev, och följ upp
            varje ansökan, från utkast till svar.
          </p>
          <div className="jp-land-hero__ctas">
            {/* CTA-färger via designsystem-tokens per design-reviewer M3 2026-05-24:
                hex-värden (#fff, #0A2647) ersatta med --jp-surface / --jp-navy-800 /
                --jp-ink-inverse / --jp-leaf-600. Inga nya `.jp-btn--*`-modifiers
                introduceras för att undvika scope-bloat — inline-tokens är
                acceptabelt mot CLAUDE.md §5.2 så länge värdena är tokens, inte
                hex. */}
            <button
              type="button"
              className="jp-btn jp-btn--lg"
              style={{
                background: "var(--jp-surface)",
                color: "var(--jp-navy-800)",
                borderColor: "var(--jp-surface)",
              }}
              onClick={() => router.push("/vantelista")}
            >
              Anmäl till väntelista
            </button>
            <button
              type="button"
              className="jp-btn jp-btn--lg"
              style={{
                background: "var(--jp-leaf-600)",
                color: "var(--jp-ink-inverse)",
                borderColor: "var(--jp-leaf-600)",
              }}
              onClick={() =>
                router.push(isAuthenticated ? "/oversikt" : "/gast/oversikt")
              }
            >
              {isAuthenticated ? "Till översikt" : "Utforska som gäst"}{" "}
              <ArrowRight size={16} aria-hidden="true" />
            </button>
          </div>
        </div>

        <AuthCard />
      </div>
    </section>
  );
}
