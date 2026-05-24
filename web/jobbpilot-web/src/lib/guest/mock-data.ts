// F-Pre Punkt 5 — Gäst-mode mockdata (Klas-direktiv 2026-05-24 + CTO-dom
// `docs/reviews/2026-05-24-fpre-punkt5-cto.md` Beslut 5).
//
// Importerar och återexporterar `OVERSIKT_MOCK` från `lib/oversikt/mock-data.ts`
// (DRY — samma "applications", "cv" mockdata används av båda trees så att
// /gast/oversikt och /gast/ansokningar är synkade per Klas-direktiv §E).
// Gäst-specifika fält (ansokningar-pipeline-snippet, cv-grid-snippet) bor här
// så `(guest)/*` är SRP-isolerad från `(app)`-tree.

import { OVERSIKT_MOCK } from "@/lib/oversikt/mock-data";

// Re-export så gäst-tree:s konsumenter slipper dubbel-import.
export { OVERSIKT_MOCK };

export type GuestApplicationStatus =
  | "Draft"
  | "Submitted"
  | "Interview"
  | "Offer"
  | "Rejected";

export interface GuestMockApplication {
  readonly id: string;
  readonly company: string;
  readonly role: string;
  readonly status: GuestApplicationStatus;
  readonly statusLabel: string;
  readonly updatedAtLabel: string;
  readonly source: string;
}

export interface GuestMockResume {
  readonly id: string;
  readonly title: string;
  readonly language: "sv" | "en";
  readonly latestRole: string | null;
  readonly sectionCount: number;
  readonly topSkills: ReadonlyArray<string>;
  readonly updatedAtLabel: string;
  readonly isPrimary: boolean;
}

export interface GuestMockData {
  readonly applications: ReadonlyArray<GuestMockApplication>;
  readonly resumes: ReadonlyArray<GuestMockResume>;
  // Sammanfattnings-tal som synkas med /gast/oversikt:s "Mina ansökningar"-rad
  // och /gast/ansokningar:s pipeline-count. Härleds från `applications` ovan
  // (single source of truth — uppdatera mockdata på en plats).
  readonly summary: {
    readonly applicationsTotal: number;
    readonly applicationsByStatus: Readonly<Record<GuestApplicationStatus, number>>;
    readonly resumesTotal: number;
  };
  /** Mock-totalt aktiva annonser i korpus (design-reviewer M5). */
  readonly activeJobAdsTotal: number;
}

// Konsistent demo-pipeline: 7 ansökningar fördelade över alla 5 status så
// /gast/oversikt:s "Sammanfattning" och /gast/ansokningar:s pipeline matchar.
const APPLICATIONS: ReadonlyArray<GuestMockApplication> = [
  {
    id: "ga-1",
    company: "Klarna",
    role: "Backend-utvecklare",
    status: "Interview",
    statusLabel: "Intervju",
    updatedAtLabel: "i går",
    source: "Platsbanken",
  },
  {
    id: "ga-2",
    company: "Folksam IT",
    role: "Systemutvecklare .NET",
    status: "Submitted",
    statusLabel: "Inskickad",
    updatedAtLabel: "för 3 dagar sedan",
    source: "Platsbanken",
  },
  {
    id: "ga-3",
    company: "Skatteverket",
    role: "Lösningsarkitekt",
    status: "Offer",
    statusLabel: "Erbjudande",
    updatedAtLabel: "i dag",
    source: "Manuell",
  },
  {
    id: "ga-4",
    company: "Bonnier News",
    role: "Verksamhetsutvecklare",
    status: "Submitted",
    statusLabel: "Inskickad",
    updatedAtLabel: "för 5 dagar sedan",
    source: "Platsbanken",
  },
  {
    id: "ga-5",
    company: "ICA Gruppen",
    role: "Fullstack-utvecklare",
    status: "Draft",
    statusLabel: "Utkast",
    updatedAtLabel: "för 2 dagar sedan",
    source: "Manuell",
  },
  {
    id: "ga-6",
    company: "Region Stockholm",
    role: "Backend-utvecklare junior",
    status: "Rejected",
    statusLabel: "Avslag",
    updatedAtLabel: "för 1 vecka sedan",
    source: "Platsbanken",
  },
  {
    id: "ga-7",
    company: "Trafikverket",
    role: "Webbutvecklare",
    status: "Draft",
    statusLabel: "Utkast",
    updatedAtLabel: "för 4 dagar sedan",
    source: "Platsbanken",
  },
];

const RESUMES: ReadonlyArray<GuestMockResume> = [
  {
    id: "gr-1",
    title: "CV — Systemutveckling",
    language: "sv",
    latestRole: "Junior systemutvecklare",
    sectionCount: 6,
    topSkills: ["C#", ".NET", "PostgreSQL", "Next.js"],
    updatedAtLabel: "i dag",
    isPrimary: true,
  },
  {
    id: "gr-2",
    title: "CV — Frontend",
    language: "sv",
    latestRole: "Webbutvecklare praktik",
    sectionCount: 5,
    topSkills: ["TypeScript", "React", "CSS", "Accessibility"],
    updatedAtLabel: "för 2 dagar sedan",
    isPrimary: false,
  },
  {
    id: "gr-3",
    title: "Resume — English version",
    language: "en",
    latestRole: "Junior Software Developer",
    sectionCount: 6,
    topSkills: ["C#", ".NET", "REST APIs", "Git"],
    updatedAtLabel: "för 5 dagar sedan",
    isPrimary: false,
  },
];

function countByStatus(
  apps: ReadonlyArray<GuestMockApplication>
): Record<GuestApplicationStatus, number> {
  const counts: Record<GuestApplicationStatus, number> = {
    Draft: 0,
    Submitted: 0,
    Interview: 0,
    Offer: 0,
    Rejected: 0,
  };
  for (const app of apps) {
    counts[app.status] += 1;
  }
  return counts;
}

// design-reviewer M5 2026-05-24: "Aktiva annonser totalt" på /gast/oversikt
// hade hårdkodat `46_000` i komponenten. Flyttat hit för single-source-of-
// truth + magic-number-avhjälpning. Mock-värde av samma storleksordning som
// dev-korpus (~46k aktiva annonser) så användaren får realistisk demo.
const GUEST_ACTIVE_JOB_ADS_TOTAL = 46_000;

export const GUEST_MOCK: GuestMockData = {
  applications: APPLICATIONS,
  resumes: RESUMES,
  summary: {
    applicationsTotal: APPLICATIONS.length,
    applicationsByStatus: countByStatus(APPLICATIONS),
    resumesTotal: RESUMES.length,
  },
  activeJobAdsTotal: GUEST_ACTIVE_JOB_ADS_TOTAL,
} as const;

// Pipeline-grupperingen för /gast/ansokningar — samma data, omstrukturerad
// efter status. Härleds (inte hårdkodad) så summan = applications.length och
// alltid synk med /gast/oversikt:s tal.
export interface GuestPipelineGroup {
  readonly status: GuestApplicationStatus;
  readonly statusLabel: string;
  readonly count: number;
  readonly applications: ReadonlyArray<GuestMockApplication>;
}

const STATUS_ORDER: ReadonlyArray<{ status: GuestApplicationStatus; label: string }> = [
  { status: "Draft", label: "Utkast" },
  { status: "Submitted", label: "Inskickad" },
  { status: "Interview", label: "Intervju" },
  { status: "Offer", label: "Erbjudande" },
  { status: "Rejected", label: "Avslag" },
];

export function buildGuestPipeline(): ReadonlyArray<GuestPipelineGroup> {
  return STATUS_ORDER.map(({ status, label }) => {
    const apps = APPLICATIONS.filter((a) => a.status === status);
    return {
      status,
      statusLabel: label,
      count: apps.length,
      applications: apps,
    };
  });
}
