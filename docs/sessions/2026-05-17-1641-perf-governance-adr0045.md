---
session: perf-governance ADR 0045 (FAS 3-prerekvisit C)
datum: 2026-05-17
slug: perf-governance-adr0045
status: levererad & CI-grön (1 ej-blockerande PENDING)
commits:
  - faf381c feat(perf): ADR 0045 perf-budgetar + observe-only CI-governance
  - 54b91ae docs(claude): CLAUDE.md §2.5 perf-konvention + §9.2 Terraform-architect-rad
  - 7ca463f feat(agents): perf-test-writer (builder, roster=13) — ADR 0045 steg 4
  - <docs-commit denna session-end-synk>
---

# Session — Performance-governance (ADR 0045), FAS 3-prerekvisit C

Tre CC:er körde parallellt mot `main` denna dag. Denna session levererade
perf-governance-bunten (FAS 3-prerekvisit C). Session-end-docs-synk kördes
SIST av de tre CC:erna, per 3-CC-koordinationsregeln.

## Mål

Etablera performance-budgetar och fitness functions som FAS 3-prerekvisit:
ADR 0045, observe-only CI-governance utan att röra ADR 0044 coverage-gaten,
CLAUDE.md-konventionsförankring, en builder-agent för perf-test, och en
generisk release-rutin-runbook.

## Leverans per steg

### Steg 1 — ADR 0045 + observe-only CI (`faf381c`)

- ADR 0045 **Accepted** (Klas-flip 2026-05-17). Budgetar:
  - (a) read-query p95 300 ms — Klas-låst
  - (b) typeahead p95 150 ms — Klas-låst
  - (c) command p95 400 ms — CTO-satt
  - (d) ingestion ≥ 200 jobb/min — CTO-satt
  - CWV: LCP < 2.5 s / CLS < 0.1 gate-intent; INP observe-only
  - Worker 512 MiB soft cap
- Mät-metod: **NBomber valt, k6 avvisat**. **BenchmarkDotNet deferrad
  per Klas-direktiv** — micro-benchmark skjuts till Fas 7.
- CI observe-only Fas 1: 3 jobb (lighthouse / loadtest / audit) lagda
  **UTANFÖR `ci.needs`** → ADR 0044 coverage-gate **orörd**. Verifierad
  grön: CI-run `25993726144` success, alla jobb inkl. coverage gröna.
- `dependabot.yml` utökad med web/jobbpilot-web npm-entry — supply-chain-
  lucka stängd; dependabot bevisat öppnat PRs efter ändringen.

### Steg 2 — CLAUDE.md §2.5 + §9.2 (`54b91ae`)

- §2.5: performance som granskningsbar kärnprincip.
- §9.2: dotnet-architect obligatorisk vid Terraform-scope.

### Steg 3 — perf-test-writer-agent (`7ca463f`)

- `.claude/agents/perf-test-writer.md` skapad. **Builder, ej reviewer/
  gate.** Mandat: NBomber + Lighthouse, ej BenchmarkDotNet.
- **Agent-roster = 13** (verifierat: `.claude/agents/` har 13 filer).
  5 CTO-avvisade agenter (infra/SRE/dependency/release/a11y) medvetet
  EJ skapade — anti-bloat per roster-gap-CTO 2026-05-17.

### Steg 4 — release-checklist-runbook

- `docs/runbooks/release-checklist.md` skapad — generisk repeterbar
  release-rutin (steg 6). Saknades genuint; `v0.2-prod-launch-checklist.md`
  är engångs-prod-specifik, ej repeterbar.

## Beslut

- BenchmarkDotNet deferrad till Fas 7 (Klas-direktiv) — micro-benchmark ej
  motiverat i Fas 1, NBomber-makrolast räcker för budget-fitness.
- CI-perf-jobb medvetet utanför `ci.needs` (observe-only Fas 1) — undviker
  att destabilisera grön main / kollidera med ADR 0044-gaten.
- Agent-roster fryst på 13; 5 föreslagna agenter avvisade som bloat.
- code-reviewer GO 0 Block / 0 Major / 0 Minor.
- senior-cto-advisor B1–B7 levererat.

## PENDING (ej blockerande — Klas-beroende nästa session)

BUILD.md §3.1 NBomber-rader (3 rader: NBomber 6.x + NBomber.Http 6.x)
**applicerades EJ**. Orsak: auto-mode-klassificeraren hård-blockerar
spec-edit-approve-hooken trots Klas-GO (STOPP 3). Ingen funktionell lucka —
NBomber är redan dokumenterad i ADR 0045 + `Directory.Packages.props`-
kommentar. Detta är den **enda kvarvarande punkten** ur perf-bunten.

Åtgärd nästa session (Klas-beroende):
- Klas kör `bash .claude/hooks/approve-spec-edit.sh` själv, ELLER
- permission-regel läggs för `bash .claude/hooks/approve-spec-edit.sh`.

## Commits

| SHA | Innehåll |
|---|---|
| `faf381c` | ADR 0045 perf-budgetar + observe-only CI-governance + dependabot.yml |
| `54b91ae` | CLAUDE.md §2.5 perf-konvention + §9.2 Terraform-architect-rad |
| `7ca463f` | perf-test-writer-agent (builder, roster=13) |
| docs-commit | denna session-end-synk (ADR-index 0045 + current-work min rad + denna logg) |

## Docs-synk (denna session-end, docs-keeper-disciplin)

- `docs/decisions/README.md` — ADR 0045-raden tillagd i index-tabellen
  (efter 0044). ADR-filen
  `0045-performance-budget-and-fitness-functions.md` verifierad committad.
- `docs/current-work.md` — ENDAST denna CC:s status-block prependat +
  metadata-raderna (Senast uppdaterad / HEAD) uppdaterade. A:s (README-
  portfolio) och B:s (pre-FAS-3 / Resume) hunks **orörda** — A:s tidigare
  status-block bevarat verbatim, endast prefixat `**(Föregående)**`.
- Rebase mot `origin/main`: **no-op** — `origin/main` HEAD = `7ca463f`
  (= min HEAD). CC A/CC B hade ej pushat vid synk-tillfället; **inga
  konflikter, inga A/B-hunks att slå samman**.
- Worktree-not: `c:/tmp/jobbpilot-perfbunt` är en git-worktree; `main` är
  utcheckad i huvud-repot. Worktreen arbetar korrekt i detached HEAD vid
  `7ca463f` och pushar via `git push origin HEAD:main`.
- Coverage-gate (ADR 0044) **ej rörd** (förbud respekterat). Ingen ny ADR
  skapad (0045 fanns redan committad). Ingen startprompt-fil skapad
  (CLAUDE.md §1.5 — levereras som chat-block av webb-Claude).

## Nästa session

1. **Klas-beroende:** applicera BUILD.md §3.1 NBomber-rader (kör approve-
   script själv eller lägg permission-regel) — enda kvarvarande perf-punkt.
2. FAS 3 (Application Management) inväntar fortsatt explicit strategisk
   Klas-GO för sessionsbyte (§9.2). Prerekvisit C nu klar.
