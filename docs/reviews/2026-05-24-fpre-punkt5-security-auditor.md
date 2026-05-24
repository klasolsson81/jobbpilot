# Security-auditor — F-Pre Punkt 5 "Utforska som gäst"

**Datum:** 2026-05-24
**Agent:** security-auditor (agentId `abeb891a0aeb5cfd3`)
**HEAD vid rond:** `6104b7d` (pending commit)
**CTO-dom-referens:** `docs/reviews/2026-05-24-fpre-punkt5-cto.md`

---

## TL;DR (för Klas)

**Status: APPROVED — inga blockers, inga critical, inga high. 1 Medium + 3 Minor + 3 Praise.**

Variant A-implementationen (egen `(guest)/gast/*`-route-grupp) levererar exakt den säkerhetsisolering som CTO-domen föreskrev. Cross-pollination-skydd är vattentätt: noll auth-API-anrop i guest-tree, ingen session-cookie sätts där, middleware exkluderar gäst-prefixet medvetet, och `(app)`-routes förblir skyddade. Cookie-mekanismen `__Host-jobbpilot_guest_welcomed` följer JobbPilots befintliga `__Host-`-mönster med rätt SameSite-val. PII-läcka från `getServerSession()` till `LandingHeroSection` är förhindrad genom strikt `isAuthenticated: boolean`-prop.

Endast en Medium (defensiv hardening — `httpOnly: false` är onödig polariseringsöppning även om klienten inte läser cookien) och tre Minor.

**Inga ADR-amendments krävs. Inga DPIA-implikationer. Inga nya sub-processors. Mockdata = noll PII.**

---

## Fynd

### Medium

**M-1: `httpOnly: false` på `__Host-jobbpilot_guest_welcomed` är onödig polariseringsöppning** — **IN-BLOCK-FIXAD** (`guest-mode-actions.ts:24` ändrad till `httpOnly: true` + paritets-kommentar).

### Minor

**m-1: SameSite=Lax vs Strict — välj medvetet och dokumentera** — **IN-BLOCK-FIXAD** (kommentar tillagd i `guest-mode-actions.ts`).

**m-2: Hardcoded `GUEST_WELCOMED_MAX_AGE` duplicerad** — **IN-BLOCK-FIXAD** (deklaration borttagen från `guest-mode.ts`, bara i `guest-mode-actions.ts`).

**m-3: `Promise.all`-timing inte en reell timing-attack-vektor** — bekräftat ofarlig; ingen åtgärd krävs.

### Praise

- **P-1:** Cross-pollination-skydd implementerat exakt enligt CTO-dom Beslut 1. Noll auth-API-anrop, noll session-cookie-set i guest-tree.
- **P-2:** PII-isolering från `getServerSession()` till `<LandingHeroSection>` är vattentät — bara boolean `isAuthenticated`.
- **P-3:** Mockdata = noll PII, single source of truth, härledda summor.

---

## GDPR-bedömning

| Krav | Status |
|---|---|
| Lawful basis för welcome-cookien | Funktional cookie (EDPB Guidelines 2/2023) — undantag från samtycke-banner |
| Data minimization | Cookie-värde = `"1"`. Ingen PII. |
| Encryption in transit | TLS via `Secure: true` |
| Retention | 365 dagar (engångs-välkomst) — proportionerligt mot syftet |
| Sub-processors | Inga nya |
| DPIA-värdighet | Nej — ingen high-risk-processing |
| Konsent-UI | Funktional cookie utan samtycke-krav |

**Verdikt:** GDPR-compliant. Inga utestående frågor.
