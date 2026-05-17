---
session: Coverage-finalisering — ADR 0044 Accepted + regressions-gate aktiverad
datum: 2026-05-17
slug: coverage-finalisering-adr0044-accepted
status: levererad
commits:
  - ee4709a ci(coverage) aktivera ADR 0044 per-lager regressions-gate + flip Accepted
  - d67d340 docs(readme) kvalitet/coverage-skryt-sektion (ADR 0044)
  - (denna docs-commit — session-end-synk)
---

# Session 2026-05-17 — Coverage-finalisering (ADR 0044 Accepted + gate-aktivering)

## Mål

Slutföra test-coverage-sidospåret: flippa ADR 0044 Proposed→Accepted, aktivera
den per-lager icke-regression-ratchet-gaten blockerande i CI och skriva
README:s kvalitet/coverage-skryt-sektion. Strategisk transition (§9.2) som
föregående session flaggade som kvarstående Klas-STOPP.

## Vad som gjordes

1. **Baseline-körning + schema-verifiering.** First-party-baseline (post-`98b6f17`,
   1156/1156 grön, 0 failed) bekräftad: Line 92.1 / Branch 84.5 / Method 90.2.
   Per lager: Domain 95.3/93.3/91.9, Application 97.7/91.1/98.1, Infrastructure
   84.0/71.1/80.3, Api 93.7/82.9/92.3, Worker 30.7/observe-only/36.8. Migrate
   exkluderad.
2. **CTO-pin (senior-cto-advisor `a7fc36da3d8b1a8dc`).** Golv = `floor(baseline−2.0pp)`:
   Domain line 93 / branch 91, Application line 95 / branch 89, Infrastructure
   line 82, Api line 91, Worker observe-only Fas 1, ingen global/method-gate.
3. **Gate-implementation (`ee4709a`).** `coverage`-jobbet tog bort
   continue-on-error + `exit 0`-stub, blockerar nu via
   `ci.needs: [backend, frontend, coverage]`. Gate-steget jämför uppmätt
   per-lager-coverage mot pinnade golv, fail-closed.
4. **ADR 0044 Proposed→Accepted (`ee4709a`).** adr-keeper: status-flip,
   §58-prosa pinnad, Mekanism-mening omformulerad till enforce:ad/historik
   (past tense), ADR-index rad 59 → Accepted.
5. **README-skryt (`d67d340`).** Sektionen "Kvalitet, test och coverage" med
   test-disciplin, reproducerbar coverage-mekanism, first-party-tabell och
   regressions-gate-beskrivning. ADR 0044-länkar pekar på rätt fil.
6. **code-reviewer GO 0 Block / 0 Maj / 0 Minor.** Edge-case-dry-runs
   verifierade fail-closed: regression under golv, saknad assembly, korrupt
   coverage-JSON.
7. **CI-grön-verifiering.** main-CI run `25989344497` = success
   (backend/frontend/coverage/ci). Gate-stegets ubuntu-output: alla 6
   per-lager-golv PASS, Worker observe-only loggad, "Coverage-gate PASSED".

## Beslut & avstickare

- **Mekanism-mening-rättning utöver adr-keeper-scope.** Utöver adr-keepers
  Accepted-flip korrigerades Mekanism-meningen till enforce:ad/historik past
  tense så ADR-prosan inte längre beskriver gaten som framtida planerad — den
  är nu aktiv. Noterat så Klas är medveten om touch-omfånget.
- **Application branch pinnad 89, inte ~75.** CTO-motiverat: golvet följer
  `floor(baseline−2.0pp)` mot uppmätt 91.1, inte en lägre defensiv siffra.
  Noterat för Klas-medvetenhet — ej blockerande, CTO-entydigt.
- **Inga TD lyfta** (§9.6 — alla fynd i-fas, in-block). **Inga §9.2-fil-ändringar**
  utöver auktoriserad README + ADR 0044-flip. Inga prod-deploys.

## Nästa session

FAS 3 (Application Management) inväntar explicit strategisk Klas-GO för
sessionsbyte (§9.2). Pending operativt oförändrat: Resume.SoftDelete-CTO-triage
vid lämplig domän-touch + F2 ingestion-cron-async-followup.
