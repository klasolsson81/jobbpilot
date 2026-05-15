# Runbook — Frontend visual verification (screenshot-loop)

> **Status:** Obligatorisk rutin. Beslut: senior-cto-advisor 2026-05-16
> (efter att v2-refaktorn godkändes på diff-nivå men underkändes visuellt av
> Klas i bred viewport). Rotorsak: agent-review av kod ≠ verifiering av
> renderad UI.

## Varför

design-reviewer granskar kod och diff. Den ser `border-b` och godkänner
mönstret — men ser inte att rader smälter ihop på en 3440px-skärm eller att
innehåll klistras mot vänsterkanten. Visuell verifiering i **verkliga
viewports** är den enda spärr som fångar detta.

## När (trigger — obligatorisk)

Visuell verifiering krävs när en frontend-batch:

- skapar en **ny route/sida**, eller
- gör en **markant ändring** av en renderad yta. *Markant* = något av:
  - ändrad sidlayout, grid eller shell
  - ny eller ombyggd komponent som renderas på en sida
  - ändring i `globals.css` som rör `.jp-page` / `.jp-app` / `.jp-main` /
    layout-tokens / `.jp-*`-komponentprimitiv
  - ändrad responsiv struktur

Ren copy- eller token-färgändring utan strukturell påverkan triggar **inte**.
Vid tvekan: kör loopen — den är billig.

## Hur

1. Starta dev-servern i en separat terminal:
   `cd web/jobbpilot-web && pnpm dev`
2. Kör loopen: `cd web/jobbpilot-web && pnpm visual-verify`
3. Scriptet (`scripts/visual-verify.ts`) tar screenshots i **tre viewports
   (1280 / 1920 / 3440)** × **light + dark** av alla publika sidor.

### Viewports

| Bredd | Varför |
|-------|--------|
| 1280  | Vanlig laptop — baslinje |
| 1920  | Vanlig desktop |
| 3440  | Bred/ultrawide — **obligatorisk**; broad-screen-buggen var osynlig under denna bredd |

### Lagring och cleanup (self-cleaning by construction)

- Bilder sparas i `C:/tmp/jobbpilot-visual/<tidsstämpel>/` — **utanför repot**
  (aldrig under `web/` eller `docs/`; repo-renhet per CLAUDE.md §1.5).
- Cleanup är **inte** ett kom-ihåg-steg. `visual-verify.ts` raderar **alla**
  tidigare körningars mappar vid start av varje ny körning. Inget att städa
  manuellt, inget att glömma.

### Auth-gated sidor (tre-nivå-policy)

| Nivå | Sidor | Verifiering |
|------|-------|-------------|
| Publika | `/`, `/logga-in`, `/registrera`, `/vantelista` | Alltid i batchen (ingen backend krävs) |
| Auth-gated | `/jobb`, `/ansokningar`, `/cv`, `/mig`, `/admin/granskning` | Deferras till **live-deploy mot dev-backend** efter Klas tag-push. Noteras i STOPP-rapporten som "visuell verifiering pending live-deploy" om batchen rör en auth-gated yta. |

Mock-session används **inte** — det verifierar inte sann render (tomma
data-states, layout-skew från riktig data missas). Ingen Docker-up tvingas i
ren frontend-batch (YAGNI).

## Vem gör vad

| Roll | Ansvar |
|------|--------|
| CC | Kör loopen efter implementation, innan STOPP-rapport. Det är ett verifieringssteg, inte ett granskningsbeslut. |
| design-reviewer | Invokeras **mot bilderna** (inte mot diff). Detta är spärren som rotfelet kräver. |
| Klas | Slutgodkänner bilderna i STOPP-rapporten. |

## STOPP-rapport — obligatorisk rad

Varje STOPP-rapport för en triggande batch innehåller:

```
Visuell verifiering: C:/tmp/jobbpilot-visual/<tidsstämpel>/
  — N screenshots (publika sidor × 1280/1920/3440 × light/dark)
  — design-reviewer-verdikt mot bilderna: <kort>
  — auth-gated: <pending live-deploy | ej berört i denna batch>
  — raderas automatiskt vid nästa körning
```

## Referens

- senior-cto-advisor-beslut 2026-05-16 (denna runbook + `scripts/visual-verify.ts`)
- CLAUDE.md §1.5 (repo-renhet), §1.6 (runbooks), §9.4 (strukturella spärrar)
- Kent Beck, *XP* — "make the right thing the easy thing" (self-cleaning cleanup)
