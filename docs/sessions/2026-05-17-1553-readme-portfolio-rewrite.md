---
session: README portfolio-omarbetning — skyltfönster för CTO/gradare + parallell-CC-bundlings-incident
datum: 2026-05-17
slug: readme-portfolio-rewrite
status: levererad-med-incident
commits:
  - 62c9dc7 docs(readme) komplett omarbetning till portfolio-skyltfönster (buntade ofrivilligt CC B:s Resume.SoftDelete-fix via delat git-index)
  - 42ee92c docs attribution-korrigering + retroaktiv security/CTO-review
  - (denna docs-commit — session-end-synk)
---

# Session 2026-05-17 — README portfolio-omarbetning

## Mål

Skriva om `README.md` från projektöversikt till **portfolio-skyltfönster**
riktat mot CTO/gradare/senior lärare (betygsatt inlämning). README ska
positionera utvecklingsmodellen (agent-orkestrering, ingenjörsprinciper i
praktiken) utan AI-klyschor och utan rötande rad-nummer-referenser, med varje
princip-påstående bundet till en verifierbar mekanism.

## Vad som levererades

### README-omarbetning (`62c9dc7`)

- **Register-undantag Klas-auktoriserat.** CLAUDE.md §1 civic-utility-ton styr
  PRODUKT-UI, inte portfolio-docs. Klas godkände explicit ett mer säljande
  register för README som skyltfönster.
- **Ny sektion "Om utvecklingsmodellen"** — LinkedIn-positionerings-framing.
- **Ny sektion "Agent-orkestrering"** — mermaid-hierarki, 12 verifierade
  agenter, six-step-modell. Ersatte den svagare "AI-driven utveckling"-texten.
- **Ny sektion "Ingenjörsprinciper i praktiken"** — Clean Arch/SOLID/DRY/SoC/
  DDD/CQRS, var och en bunden till en verifierbar mekanism (arch-test / ADR /
  namngiven kod-väg), inga rötande rad-nummer.
- **FALSE-CLAIM rättad.** Gammal felaktig 4-fas-modell → auktoritativ 8-fas-
  modell från steg-tracker (Fas 0/1/2 Klar, Fas 3 Planerad).
- Faktasektioner + Mastercard-citatet + civic-utility-produktframing behållna.

**Gate-trail:** senior-cto-advisor register/substans-GO (agentId
`aaed9537d8bb200f5`); code-reviewer GO 0 Block / 0 Major efter 2 blockers
fixade (53 arch-test-fakta / 10 filer korrigerade; `ApplicationId.cs`
verbatim-komplett) — re-review GO (agentId `a475be159946aa558`).

### Attributions-korrigering + retroaktiv review (`42ee92c`)

Forward-attribution-commit. Två review-trail-filer skapade:

- `docs/reviews/2026-05-17-resume-softdelete-retroactive-security.md` —
  security-auditor GO 0/0/0: guarden STÄNGER en latent Art.17
  erasure-delay-regression, ALIGN med JobSeeker/Application, inga
  downstream-konsumenter.
- `docs/reviews/2026-05-17-resume-softdelete-retroactive-cto.md` —
  senior-cto-advisor: BENIGN consistency-alignment, CTO-clearable, ingen TD,
  ingen domän-followup; ApplicationNote/FollowUp child-guard-asymmetri
  explicit triagead som benign non-TD.

## Incident — parallell-CC-bundling via delat git-index

`62c9dc7` (`docs(readme):`) buntade **även** en parallell CC:s (CC B)
`Resume.SoftDelete` idempotens-guard (`Resume.cs`, `ResumeVersion.cs`,
`ResumeTests.cs`). Rotorsak: `git commit` kördes utan pathspec mot ett delat
git-index → CC B:s staged domän-ändring sveptes in i README-docs-committen.

**Karaktär:** koden är korrekt, intakt och CI-grön. Defekten är
commit-hygien/attribution (CLAUDE.md §1.5-brott) + cross-CC-kontaminering —
inte funktionell.

**Klas-direktiv:** forward-recovery, INGEN history-rewrite (pushad delad main
+ aktiv parallell CC, ADR 0019). Genomfört via `42ee92c`:
forward-attribution-commit + retroaktiv security-auditor + senior-cto-advisor-
review. Bägge agenter rensade koden (0/0/0 respektive benign/no-TD). Ingen
ny TD (§9.7 — process/doc-drift, ej teknisk skuld).

**ÖPPEN — kräver Klas medveten retroaktiv kvittering:** en
domän-event-semantik-ändring på ett PII-aggregat (`Resume`) nådde `main` utan
det föreskrivna pre-commit Klas-GO:t. Koden är retroaktivt rensad av
security-auditor + CTO, men PROCESS-glidningen måste Klas medvetet kvittera —
det är en disciplin-avvikelse, inte bara en kod-fråga.

## Beslut & avstickare

- **Register-undantag är scoped till portfolio-docs.** Gäller inte produkt-UI
  eller annan användarvänd copy. Dokumenterat så framtida sessioner inte
  generaliserar undantaget.
- **Forward-recovery framför rewrite.** Mekaniskt korrekt under ADR 0019
  (pushad delad main) — history-rewrite avvisad av Klas.

## Öppen not (kräver separat Klas-beslut — EJ aktionerad denna session)

Att formalisera "uppdatera README vid fas-stängning" i CLAUDE.md §1.5 är ett
**separat Klas-beslut**. CLAUDE.md är §9.2-skyddad och får inte redigeras här.
Noteras som öppen punkt i current-work.md.

## Cross-ref-verifiering (docs-keeper)

README↔ADR-cross-refs verifierade — **ingen drift**:

- ADR-länkar 0001 / 0008 / 0010 / 0011 / 0019 / 0022 / 0024 / 0027 / 0039 /
  0043 / 0044 + 0031 — samtliga målfiler existerar.
- ADR-index (`docs/decisions/README.md`) konsistent: 0001–0044, 44 poster,
  matchar README-badgen "ADR-44 beslut".
- "12 specialiserade agenter"-påståendet matchar `.claude/agents/` exakt
  (12 .md-filer).
- `docs/steg-tracker.md` + `docs/current-work.md`-länkar resolver.

## Nästa session

FAS 3 (Application Management) inväntar explicit strategisk Klas-GO för
sessionsbyte (§9.2). Pending operativt oförändrat. **Kräver Klas-action:**
medveten retroaktiv kvittering av process-incidenten (62c9dc7-bundlingen).
