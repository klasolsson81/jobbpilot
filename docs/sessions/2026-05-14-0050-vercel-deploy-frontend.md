---
session: Vercel-deploy frontend till www.jobbpilot.se
datum: 2026-05-14 (initierat 2026-05-13 kväll, slutförd efter midnatt)
slug: vercel-deploy-frontend
status: LIVE end-to-end
commits:
  - cbe4a10 feat(infra): Vercel DNS för jobbpilot.se + www + CAA defense-in-depth
  - 25aa476 fix(web): ta bort pnpm-workspace.yaml (försök, behållits som hygien)
  - 9d0eae4 fix(web): force Webpack-build (försök, behållits som säkerhet mot Turbopack)
  - fcfe710 fix(web): vercel.json explicit framework=nextjs (LÖSNINGEN)
tag: ingen (frontend-only batch + DNS, ingen backend-deploy)
---

# Vercel-deploy frontend — F2-P10 LIVE på www.jobbpilot.se

## Mål

Ta F2-P10-frontend (commit 70e1505) live på Vercel mot dev-backend
(`https://dev.jobbpilot.se`). Custom domain `jobbpilot.se` + `www.jobbpilot.se`
per civic-utility-konvention (Klas-beslut: apex-as-redirect, www-as-primary).

## Slutresultat

- **Frontend live:** https://www.jobbpilot.se (commit `fcfe710`)
- **Apex redirect:** `jobbpilot.se` → 301 → `www.jobbpilot.se`
- **TLS:** Let's Encrypt auto-issuera via Vercel
- **Backend-koppling:** Next.js Server Components fetch:ar `https://dev.jobbpilot.se/api/v1/*`
- **Klas verifierat live (00:50 2026-05-14):** /, /logga-in, /mig, /admin/granskning, /jobb (3391 träffar från Platsbanken)

## Commits (5 totalt — varav 1 löste, 3 var fel hypoteser, 1 DNS-grund)

| Commit | Innehåll | Effekt |
|---|---|---|
| `cbe4a10` | Vercel DNS-records (apex A 216.198.79.1 + www CNAME 9b8a4671... + CAA Let's Encrypt) — Terraform applied i prod/baseline | DNS pekar mot Vercel ✅ |
| `25aa476` | Ta bort pnpm-workspace.yaml + flytta ignoredBuiltDependencies till package.json's pnpm-field | Hypotes (fel orsak) men hygienförbättring behållen |
| `9d0eae4` | next build/dev --webpack flag (force Webpack istället för Turbopack-default) | Hypotes (fel orsak) men säkerhetsmarginal behållen |
| `fcfe710` | **vercel.json med "framework": "nextjs"** | **LÖSNINGEN** ✅ |
| (Klas UI) | Dashboard Framework Preset = Next.js (defense-in-depth match) + radera oönskat `jobbpilot-web`-projekt | Cosmetic cleanup |

## CTO-rond — diagnos först (inte gissning)

Efter 3 misslyckade hypoteser (Vercel Authentication, pnpm-workspace, Turbopack)
invokerades senior-cto-advisor med input från externa AI:er (Gemini + ChatGPT).

CTO-beslut entydigt: **diagnos via lokal `vercel pull` + inspektera
`.vercel/project.json`** (read-only, gratis). NEJ till destructive actions
(delete-project, ta bort middleware) utan datadrivet underlag.

Motivering mot principer:
- **Saltzer/Schroeder 1975 Fail-Safe Defaults** — destructive utan rotorsak = tickande bomb
- **Beck TDD-spirit** — vi vet inte varför → gissningar är cargo-cult
- **CLAUDE.md §9.4** — Discovery är gratis, agera inte på fel antagande
- **YAGNI** — en variabel åt gången

## Root cause (avslöjad av CTO-godkänd diagnos)

`vercel pull` mot rätt projekt avslöjade:

```json
{
  "projectId": "prj_8775g9nYgQH7YhBoP4x4GFbw3lvB",
  "projectName": "jobbpilot",
  "settings": {
    "framework": null,        ← KRITISKT! Inte "nextjs"
    "rootDirectory": "web/jobbpilot-web",
    ...
  }
}
```

**När projektet skapades via "New Project"-flödet i UI:t** valdes inte
Application Preset = Next.js explicit (Klas noterade att dropdown:n
"försvann" — fältet sparades som null).

**Konsekvens:** Build körde OK (Vercel CLI auto-detect:ar Next.js från
package.json) men Vercel-platform-side hade `framework: null` →
routing-tabell, edge-runtime och function-mappning registrerades INTE som
Next.js → ALLA URLs gav 404 NOT_FOUND oavsett auth, build-bundler eller
workspace-config.

## Fix

`vercel.json` i `web/jobbpilot-web/`:

```json
{
  "$schema": "https://openapi.vercel.sh/vercel.json",
  "framework": "nextjs"
}
```

Vercel.json **överrider dashboard-settings** (Production Override > Project
Settings). Versionkontrollerat, immune mot UI-glitch. Push triggade
auto-rebuild på Vercel → 200 OK på alla routes inom 90 sek.

## Disciplinmissar + lärande

### Tre misslyckade hypoteser innan datadriven diagnos

1. **Vercel Authentication** — Klas disablerade, fortsatt 404
2. **pnpm-workspace.yaml** → Vercel monorepo-detection — tog bort, fortsatt 404
3. **Turbopack-output** → bryter Vercel routing — force Webpack, fortsatt 404

Alla tre var **gissningar utan diagnostisk data**. Klas spenderade ~2h på
detta. Disciplinmiss att inte börja med `vercel pull` direkt.

### Lärande för memory

**`vercel pull` + inspektera `.vercel/project.json` är obligatoriskt
diagnos-första-steg när Vercel-deploys bär sig konstigt.** Settings-mismatch
mellan dashboard och vad CC kan se från utsidan är **osynlig** utan det
steget. CC ska INTE gissa hypoteser baserat på community-rapporter när
faktisk projekt-state är otillgänglig.

CTO-rond + AI-konsult var rätt sätt att bryta gissnings-loopen — men
hade kunnat undvikas helt om jag börjat med `vercel pull` efter första
404-svaret.

### CTO-godkända fall-back-hypoteser (ej använda)

CTO godkände att eskalera till delete-reimport av Vercel-projekt **endast
om diagnos visat korrekt `.vercel/output/`-output**. Behövdes inte —
diagnos avslöjade root cause direkt.

## Klas-cleanup-actions (genomförda)

1. **Vercel UI Settings → Framework Preset = Next.js** (defense-in-depth match med vercel.json)
2. **Raderat oönskat `jobbpilot-web`-projekt** (skapades av min `vercel pull` innan jag länkat rätt projekt)

## Verifierat end-to-end (Klas 00:50 2026-05-14)

| URL | Status | Klas-screenshot bekräftat |
|---|---|---|
| `https://jobbpilot.se` | 301 → www | ✅ |
| `https://www.jobbpilot.se/` | 200 LandingPage | ✅ (designsystem-demo, ny TD: behöver login/register-CTA) |
| `https://www.jobbpilot.se/logga-in` | 200 + login-form | ✅ |
| `https://www.jobbpilot.se/registrera` | 200 | ✅ |
| `https://www.jobbpilot.se/vantelista` | 200 | ✅ |
| `https://www.jobbpilot.se/mig` | 200 (auth-gated) | ✅ Klas profil renderad, Admin-roll |
| `https://www.jobbpilot.se/admin/granskning` | 200 (admin-gated) | ✅ Audit-logg LIVE med System.JobAdsSynced cron-events |
| `https://www.jobbpilot.se/jobb` | 200 | ✅ **3391 jobbannonser** från Platsbanken via JobTech-integration |
| `https://www.jobbpilot.se/api/me` | 401 (utan auth) | ✅ Backend-koppling fungerar |

## Web-search-källor (CLAUDE.md §9.5)

- [Next.js 16 Turbopack docs](https://nextjs.org/docs/app/api-reference/turbopack) — bekräftade Turbopack default + `--webpack` opt-in
- [Vercel Project Configuration](https://vercel.com/docs/project-configuration) — vercel.json properties
- [Vercel Monorepos](https://vercel.com/docs/monorepos) — pnpm-workspace handling
- [Vercel Community: No Next.js version detected (pnpm monorepo)](https://community.vercel.com/t/.../18750)

## TD lyfta

- **TD-81 (Minor, Trigger):** middleware.ts → proxy.ts (Next.js 17-uppgradering eller proxy-konvention stabiliserat). Källa: Vercel-deploy-session 2026-05-13/14. Bekräftat OK i Next.js 16 (deprecation-varning, inte breaking).

## TD lyft pressade mot §9.6 (NEJ — i-block-fix istället)

- pnpm-workspace.yaml-borttagning (commit 25aa476) → behållen som hygien, inte rollback
- Webpack-force (commit 9d0eae4) → behållen som säkerhetsmarginal mot Turbopack-edge-cases på Vercel
- Vercel.json med framework=nextjs → permanent fix in-block

## Pending operativt för Klas

- **Landing-page-CTA** (din observation 00:48): `(marketing)/page.tsx` är
  design-system-demo, saknar "Logga in" + "Anmäl till väntelistan"-knappar.
  Civic-utility-MVP-krav. Kandidat: snabb-fix in-block i F2-P11 ELLER egen
  UX-batch.
- AWS SSO-token-livslängd, JobTech-API-key, BUILD.md §9.1 sync mot ADR 0032 §3 — kvarstår
- Backend prod-stack-bring-up (ADR 0036 D1) — Fas 7-prep, frontend pekar
  på dev-backend tills dess (ENV-cutover via Vercel-UI vid behov)

## Nästa session — Klas-val

1. **Landing-page-CTA-fix** (snabb, civic-utility-MVP-blocker för marknadsföring)
2. **F2-P11 / nästa Fas 2-feature** TBD per Klas roadmap
3. **v0.2-prod-tag-prep** (TD-13 PII-encryption är enda kvarstående Major Fas 2-blocker; CTO confirmed defer 2026-05-13)
4. **OIDC-drift-städning** (pre-existing 2 change-poster i prod/baseline-Terraform, lyft som TD eller fixa i opportunistisk batch)
