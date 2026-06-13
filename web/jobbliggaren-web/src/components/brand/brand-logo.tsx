// Jobbliggaren brand-mark — "Sigillet" (logo-översyn 2026-06-13, ersätter kompassen).
// Ren RSC + inline SVG via BrandMarkSvg. Marken är tre-färgad (grön skiva + guld + papper)
// och kan därför inte ärva en enda currentColor som den gamla kompassen — fyllen sätts
// explicit via mark-tokens (--jp-mark-*). Wordmarken ärver .jp-brand color (ink).

import { BrandMarkSvg } from "./brand-mark-svg";

type BrandLogoVariant = "full" | "mark";

export interface BrandLogoProps {
  /**
   * `full` (default) renderar mark + wordmark "Jobbliggaren".
   * `mark` renderar bara sigillet (för minimala kontexter).
   */
  variant?: BrandLogoVariant;
  /**
   * Mark-storlek i px. Wordmark skalas via .jp-brand__word-CSS i full-varianten.
   */
  markSize?: number;
}

export function BrandLogo({ variant = "full", markSize = 32 }: BrandLogoProps) {
  return (
    <>
      <BrandMarkSvg
        className="jp-brand__mark"
        width={markSize}
        height={markSize}
        primaryFill="var(--jp-mark-primary)"
        accentFill="var(--jp-mark-accent)"
        paperFill="var(--jp-mark-paper)"
        ariaHidden={variant === "full" ? true : undefined}
        ariaLabel={variant === "mark" ? "Jobbliggaren" : undefined}
      />
      {variant === "full" ? (
        <span className="jp-brand__word" aria-hidden={true}>
          Jobbliggaren
        </span>
      ) : null}
    </>
  );
}
