# CTO-dom — F-Pre Punkt 5 "Utforska som gäst"

**Datum:** 2026-05-24
**Agent:** senior-cto-advisor
**Trigger:** Klas-prompt 2026-05-24 — multi-approach-arkitektur + BE-yta-paradox
**HEAD vid dom:** `6104b7d` (main)
**Nästa tag:** `v0.2.70-dev`
**Klas-prompt-ref:** F-Pre Punkt 5 (riktigt gäst-mode: read-only + mockdata + /jobb LIVE + DEMO-banner + första-gångs-modal)

---

## TL;DR (för Klas)

1. **Arkitektur:** Variant A — egen route-grupp `(guest)/*` med URL-prefix `/gast/*`. Variant B (cookie-flag på samma URLs) **avvisas** på säkerhetsskäl (Saltzer–Schroeder fail-safe-defaults + OWASP ASVS V3). Variant C (middleware-rewrite till internal route) avvisas på Next.js-idiomatik + komplexitets-skäl.
2. **Redan-inloggade i gäst-mode:** Gäst-CTA är **anonym-only**. Inloggad användare ser inte "Utforska som gäst"-knappen. Ingen "växla till demo"-toggle.
3. **BE-yta-paradox (/jobb LIVE):** **Klas-STOPP-fråga.** CTO rekommenderar **Väg 2** (drop `.RequireAuthorization()` på publika GET-routes + dedikerad `JobAdsPublicReadPolicy` + ADR 0005-amendment + cache-headers) — men det är ADR-amendment + säkerhetsutvidgning som **kräver explicit Klas-GO och security-auditor-rond innan kod-ändring**. Alternativet (mocka /jobb) är diskvalificerat av Klas-direktiv §D.
4. **Welcome-modal:** Cookie (`__Host-jobbpilot_guest_welcomed`), inte localStorage. SSR-kompatibilitet vinner.
5. **Mockdata:** Ny `lib/guest/mock-data.ts` som **importerar och återexporterar** delmängder från `lib/oversikt/mock-data.ts`. SoC-fördel utan duplikat.

**Vad CC kan implementera direkt utan Klas-GO efter denna dom:**
- Variant A — `(guest)/*` route-grupp, mockdata, FE-only komponenter
- Welcome-modal-cookie
- Mockdata-organisation enligt §5

**Vad som kräver Klas-GO + agent-rondar innan implementation:**
- BE-yta-paradox Väg 2 (ADR 0005-amendment + security-auditor + dotnet-architect)
- Tills GO finns: gäst-mode levereras **utan /jobb LIVE**, eller /jobb hide:as ur gäst-nav, eller deploy-pausas på Punkt 5 tills frågan avgjord

---

## Beslut 1 — Arkitektur (Variant A vs B vs C)

### Vald approach: Variant A — egen route-grupp `(guest)/*` med URL-prefix `/gast/*`

Konkret struktur:

```
web/jobbpilot-web/src/app/
  (app)/                  ← oförändrad, auth-gated
    oversikt/page.tsx
    ansokningar/page.tsx
    cv/page.tsx
    jobb/page.tsx
    layout.tsx            ← getServerSession() → redirect
  (guest)/                ← NY, ingen auth
    gast/
      oversikt/page.tsx
      ansokningar/page.tsx
      cv/page.tsx
      layout.tsx          ← INGEN getServerSession; renderar GuestShell + DemoBanner
```

`/jobb` förblir **en enda URL** (se Beslut 3 — om Väg 2 godkänns: middleware släpper igenom utan cookie för `/jobb`; om inte: `/jobb` ingår inte i gäst-nav alls).

### Motivering mot principer

- **SRP (Martin 2017, kap. 7):** Auth-gated tree och anonym demo-tree har **olika change-reasons**. Auth-tree ändras när session/role-modell ändras; demo-tree ändras när mockdata/onboarding-copy ändras. En modul per change-reason ⇒ två trees.
- **Least Common Mechanism (Saltzer & Schroeder 1975, "Basic Principles of Information Protection"):** Varje delad mekanism mellan privilegierad och oprivilegierad kod är en potentiell privilege-escalation-vektor. Variant B delar `(app)/layout.tsx`, `getServerSession()`-callsites, alla page-komponenter — varje "if (guest) {...} else {...}"-gren är en framtida bypass-bug. Variant A delar **noll** kod på render-vägen.
- **Fail-Safe Defaults (Saltzer & Schroeder 1975):** I Variant A är default access-decision för `(app)/*` = "neka" (oförändrad middleware + layout). I Variant B blir default "tillåt om en av två cookies finns" — vilket vänder polariteten och kräver positiv bevisning per request att gäst-grenen inte exponerar auth-yta.
- **OWASP ASVS V3 (Session Management) + V4 (Access Control):** Minimera mekanismer som korsar trust-boundaries. Variant A håller boundary skarp (URL-prefix); Variant B suddar ut den.
- **Component Cohesion — CCP/CRP (Martin 2017, kap. 13):** Saker som ändras tillsammans ligger tillsammans. Demo-mockdata + DEMO-banner + welcome-modal + disabled mutationer är **en cluster** av relaterade ändringar. De hör hemma i ett tree, inte spridda som conditionals.
- **Bounded Context (Evans 2003, ch. 14):** Gäst-mode är en separat sub-domän ("anonym utforskning") med eget vokabulär (DEMO, "Logga in för att…"). Egen route-grupp = explicit context boundary.
- **Next.js 16 idiomatik (Microsoft Learn / Next.js docs — `app`-router route groups):** Route-grupper med parens (`(guest)`) är den officiellt sanktionerade mekanismen för separata layouts per logisk grupp. Det är **mer idiomatiskt** än middleware-rewrite (Variant C).

### Avvisade alternativ

**Variant B — Cookie-flag på samma URLs:** Avvisas. Bryter Least Common Mechanism, Fail-Safe Defaults och SRP. Klas pre-meddelade en preferens för "ingen URL-duplikat" men det är inte ett design-värde i sig — det är en estetisk preferens som väger lättare än säkerhets-isolering. Varje `getServerSession()`-callsite (layout.tsx + jobb/page.tsx + minst 7 till per discovery) skulle behöva en gäst-undantags-gren — varje sådan är en framtida regression-vektor när nya endpoints/pages tillkommer i `(app)/*`. Defense-in-depth (ADR 0017) bryter samman: middleware släpper igenom på gäst-cookie, layout släpper igenom på gäst-cookie, sedan ska varje page komma ihåg att svara med mockdata istället för riktig data. Första missade page = data-leak.

**Variant C — Middleware-rewrite `/oversikt` → internal `/_guest/oversikt`:** Avvisas. (a) Next.js 16 dokumenterar inte stabil "internal route"-konvention för app-router-rewrites; risken för RSC-Suspense-buggar är reell. (b) Den "samma URL" som Klas önskade är ett UX-trade som inte uppväger debug-kostnaden ("varför renderas det här när URL säger något annat?"). (c) Magic. Granskningsbarhet < Variant A.

### Trade-offs accepterade

- **URL-duplikat:** `/oversikt` (auth) och `/gast/oversikt` (anonym) existerar parallellt. Klas pre-meddelade ovilja men detta är acceptabelt — URL-paths är inte ett knappt resurs och `/gast/`-prefixet är **läsbart och självdokumenterande** (1177/Digg-ton: "du är i demoläget").
- **Vissa komponenter kan dupliceras visuellt:** lös genom att extrahera presentational components till `components/oversikt/`, `components/ansokningar/` och konsumera från båda trees. Behavior och data-source är skilda; presentation kan delas.

### Cross-pollination-skydd

Gäst-besöket får aldrig "läcka" in i auth-tree om användare loggar in. Konkret:
- `(guest)/gast/layout.tsx` sätter inte `__Host-jobbpilot_session`-cookie.
- Login-flow på `/logga-in` rensar `__Host-jobbpilot_guest_welcomed` (eller separat short-lived guest-cookie om vi inför sådan) vid lyckad inloggning.
- Inga delade React Context providers mellan trees.

---

## Beslut 2 — Redan-inloggade användare i gäst-mode

### Vald approach: **Anonym-only. Inloggade ser inte "Utforska som gäst"-CTA.**

- `landing-hero-section.tsx` (idag `router.push("/jobb")`): byt till **conditional rendering** baserat på `getServerSession()` i server-component-föräldern.
  - Anonym besökare → "Utforska som gäst"-CTA → `/gast/oversikt`
  - Inloggad besökare → "Till översikt"-CTA → `/oversikt`
- Ingen "växla till demoläge"-toggle för inloggade.

### Motivering mot principer

- **SRP + SoC (Martin 2017, kap. 7; Dijkstra 1974):** "Utforska som gäst" har **ett syfte:** låta en oklar besökare provspela utan att skapa konto. När personen redan har konto är det syftet uppfyllt — knappen löser ingen besökar-problem.
- **YAGNI (Beck/Jeffries via XP-litteratur; Fowler refactoring-praxis):** "Demo-läge för inloggade" är en hypotetisk feature utan validerad use case. Klas-prompt nämner det som öppen fråga, inte uttryckt behov. Bygg inte specifikt UI för spekulativ användning.
- **Civic-utility-disciplin (CLAUDE.md §1, DESIGN.md):** 1177/Digg/GOV.UK exponerar inte "demo-toggle" för inloggade. Det är en SaaS-konvention från Linear/Notion-sfären. JobbPilot ska kännas seriös och pålitlig — onödiga lägesväxlare urholkar det.
- **Mental model-isolering:** En inloggad användare som ser blandning av riktig data + mock i samma session får svår-debuggad "vilken data är vilken?"-osäkerhet. Bättre att hålla lägena strikt separerade.

### Avvisade alternativ

**"Växla till demo"-toggle för inloggade:** Avvisas. Bygger spekulativ feature. Kompliscerar mental model. Bryter civic-utility-tonen. Ingen valideringspunkt visad.

**CTA syns för båda, redirect:as om inloggad:** Avvisas. UX-fula (klick → redirect). Bryter principle of least astonishment.

---

## Beslut 3 — BE-yta-paradoxen (/jobb LIVE)

### Klas-STOPP-fråga. CTO rekommendation: Väg 2 — men kräver explicit Klas-GO + agent-rondar innan implementation.

### Rekommenderad väg (för Klas att godkänna eller avvisa)

**Väg 2:** Drop `.RequireAuthorization()` på publika GET-routes i `JobAdsEndpoints.cs`, behåll på POST. Lägg dedikerad rate-limit-policy `JobAdsPublicReadPolicy` (per IP, 60/min — paritet ADR 0064). Ändra `Cache-Control` på `/taxonomy` och `/taxonomy/labels` från `private` till `public, max-age=3600` (där tillämpligt — labels är `no-store` idag pga per-request-IDs, behåll). ADR 0005-amendment dokumenterar låsnings-upplyftning + rate-limit-disciplin + bot-trafik-mätbeslut.

### Varför Väg 2 vinner mot principer (om Klas godkänner)

- **Klas-direktiv §C ("inga nya BE-endpoints"):** Väg 2 lägger inga nya endpoints — modifierar policy på befintliga. Klas-direktiv §D ("/jobb LIVE") uppfylls. Båda direktiv harmoniseras.
- **DRY (Hunt/Thomas 1999):** Att skapa parallell `/api/v1/public/job-ads/*` (Väg 3) duplicerar query-handlers, validators, rate-limit-policies. Samma knowledge piece (publik job-ad-listning) på två ställen.
- **Open/Closed (Martin 2017, kap. 8):** Endpoint-gruppen är öppen för auth-modus-konfiguration; modulen kräver inte rewrite för att stödja publik läsning.
- **ADR 0064-paritet:** Mönstret för publik-anonym + IP-rate-limit + Cache-Control är redan accepterat. Väg 2 är konsekvent tillämpning, inte nytt mönster.
- **Saltzer–Schroeder "Economy of Mechanism":** Färre kodvägar = mindre attack-yta att underhålla.

### Varför detta är Klas-STOPP-fråga (inte CC-entydigt follow)

Detta är **inte** en in-block-fix. Tre skäl:

1. **ADR-amendment krävs.** ADR 0005 (Accepted) säger explicit "Anonym publik katalog kan låsas upp senare via separat ADR efter mätning av JobTech-proxy-kostnad och bot-trafik." Klas-prompten har inte presenterat mätning. CTO kan inte unilateralt deklarera mätningen tillräcklig — det är fas-strategiskt val Klas måste fatta.
2. **Säkerhets-/kostnads-tradeoff utanför CC:s mandat.** Bot-trafik mot publik `/api/v1/job-ads`-listning kan generera JobTech-proxy-kostnad (om vi proxyar) eller DB-load. ADR 0005 lyfte detta som blocker — ingen ny mätning ändrar grunden.
3. **security-auditor-rond saknas.** Att dropa `.RequireAuthorization()` på fem GET-routes är major security-touch (CLAUDE.md §9.2). Måste invokeras innan kod-ändring.

### Vad Klas behöver besluta

**Alt 1 (rekommenderat av CTO om Klas accepterar tradeoffsen):**
- GO för Väg 2
- CC invokerar security-auditor + dotnet-architect-rondar
- Klas läser rapporterna
- ADR 0005-amendment skrivs (av webb-Claude per CLAUDE.md §9.4 verbatim-källa-regel)
- Sedan kod-ändring

**Alt 2 (mer konservativ — CTO accepterar som giltig):**
- Leverera Punkt 5 utan /jobb LIVE
- /jobb hide:as ur gäst-nav (gäst-mode = bara oversikt/ansokningar/cv)
- Eller: /jobb-länken visas men leder till "Logga in/Anmäl till väntelista för att söka jobb"-vy (CTA-yta — pedagogisk konvertering)
- Punkt 5 kan deployas snabbare; /jobb LIVE blir egen senare bit

**Alt 3 (Väg 1 — mocka /jobb):** Diskvalificerad av Klas-direktiv §D. Inte ett alternativ enligt prompt-texten. CTO respekterar Klas-direktiv.

**Väg 3 (separat publik endpoint-grupp):** Avvisad (DRY-brott + Klas-direktiv "inga nya endpoints").

### CTO:s preferensordning

1. **Alt 2 först** — släpp Punkt 5 nu utan /jobb LIVE. Få DEMO-mode levererad, mät om gäster ens når /jobb-CTA, sedan separat session: Alt 1 om signal finns.
2. **Alt 1** — om Klas redan vill ha /jobb LIVE i samma punkt. Då pausas Punkt 5 till ADR-amendment + agent-rondar klara.

Skälet är **Beck/Fowler small-batches**: leverera en cohesive ändring (demo-mode) snabbt, mät, sedan nästa cohesive ändring (publik /jobb). Att bunta dem ökar scope och risk.

---

## Beslut 4 — Welcome-modal-trigger (cookie vs localStorage)

### Vald approach: Cookie `__Host-jobbpilot_guest_welcomed` (HttpOnly: false; SameSite=Lax; Path=/)

### Motivering mot principer

- **SSR-kompatibilitet (Microsoft Learn / Next.js RSC docs):** Server Component i `(guest)/gast/oversikt/page.tsx` behöver läsa "har modal visats?"-flag **innan render** för att inte skapa hydration flash. localStorage är inte tillgänglig server-side. Cookie är.
- **Defense-in-depth (ADR 0017):** Cookie följer existerande mönster (`__Host-jobbpilot_session`, `__Host-jobbpilot_guest_welcomed`). Konsekvent infrastruktur.
- **GDPR funktional-cookie (EDPB Guidelines 2/2023):** En cookie som husar UX-state ("har sett välkomst") är funktional och kräver inte samtycke-banner (krono-kakor undantagna). localStorage med samma syfte har samma juridiska status men teknisk-skuld-värre integration.

### Implementation

- `__Host-`-prefix kräver `Secure` + `Path=/` + inget `Domain` — paritet med session-cookie.
- HttpOnly inte nödvändigt (klient behöver inte läsa; men server behöver — så server sätter, server läser).
- Modal-state hanteras client-side med shadcn `Dialog`; första visning trigger:as av server-prop `showWelcome={true}` när cookie saknas; on-close anropar Server Action som sätter cookien.
- Cookie giltighet: 365 dagar (engångs-välkomst).

### Avvisade alternativ

**localStorage:** Avvisas. SSR-flash-risk + duplicerat state-pattern (vi använder cookies för auth, varför inte också för UX-state?).

**Per-session-state utan persistens:** Avvisas. Användare som loggar in och ut igen får modal upprepat — irritation.

---

## Beslut 5 — Mockdata-organisation

### Vald approach: Ny `web/jobbpilot-web/src/lib/guest/mock-data.ts` som **importerar och återexporterar** delmängder från `lib/oversikt/mock-data.ts`. Egna gäst-specifika fält (väntelista-applications, demo-CV) deklareras i `guest/mock-data.ts`.

```
lib/oversikt/mock-data.ts      ← single source för oversikt-mock (oförändrad)
lib/guest/
  mock-data.ts                 ← NY: importerar oversikt-mock + lägger till
                                  guest-specifika (ansokningar-lista, cv-snippet)
  guest-mode.ts                ← server-action helpers (cookie set/read)
```

### Motivering mot principer

- **DRY (Hunt/Thomas 1999):** Klas-direktiv §E säger "mockdata synkad mellan /oversikt och /ansokningar (samma applications-objekt)". Detta är samma knowledge piece. EN deklaration, två konsumenter.
- **SRP (Martin 2017, kap. 7):** `oversikt/mock-data.ts` ägs av oversikt-domänen. `guest/mock-data.ts` ägs av guest-mode-domänen. När oversikt-domänen ändras (t.ex. ny widget) ändras `oversikt/mock-data.ts` ensam; gäst-tree konsumerar oförändrat. Guest-specifika fält (väntelista-mock-applications-data) ändras i `guest/mock-data.ts`.
- **CCP/CRP (Martin 2017, kap. 13):** Gäst-tree och guest-mock ändras tillsammans → ligger tillsammans i `lib/guest/`.
- **Klas-direktiv §C ("inga nya BE-endpoints"):** Mockdata är ren FE-konstant. Konsistent.

### Avvisade alternativ

**Utvidga `lib/oversikt/mock-data.ts` med guest-specifika fält:** Avvisas. Förorenar oversikt-modulen med guest-koncept. Bryter SRP — oversikt-modulen ska inte veta att guest-mode existerar.

**Helt separat `lib/guest/mock-data.ts` utan import-återexport:** Avvisas. Duplicerar oversikt-mockdata-shapes. Bryter DRY när Klas vill ha "synkad" data.

---

## Sammanfattning — vad CC ska göra

### CC implementerar direkt (efter denna dom, utan extra Klas-GO):

1. Skapa `(guest)/gast/`-route-grupp med `layout.tsx` (GuestShell + DemoBanner, ingen auth).
2. Bygg `(guest)/gast/oversikt/page.tsx`, `/gast/ansokningar/page.tsx`, `/gast/cv/page.tsx` — alla server components, konsumerar `lib/guest/mock-data.ts`.
3. Skapa `lib/guest/mock-data.ts` enligt Beslut 5.
4. Skapa `lib/guest/guest-mode.ts` med Server Actions för welcome-cookie set/read.
5. Bygg `<GuestDemoBanner />` (overhead-banner ovanför page-content i `(guest)/gast/layout.tsx`) — civic-utility-ton, ingen emoji/utropstecken.
6. Bygg `<GuestWelcomeModal />` (shadcn Dialog) — trigger:as när welcome-cookie saknas; on-close → cookie set.
7. Uppdatera `landing-hero-section.tsx` enligt Beslut 2 (anonym-only CTA → `/gast/oversikt`; inloggade ser "Till översikt").
8. Lägg gäst-nav i `GuestShell` (samma look som AppShell-nav men /gast/-prefix; INGEN `/gast/jobb`-länk tills Beslut 3 avgjort).
9. Dölj/disable muterande knappar i guest-tree (Klas-direktiv §F): Spara, Markera ansökt, Ladda upp CV, Radera notis → ersätts med inline-CTA "Logga in eller anmäl till väntelista".
10. Visual-verify-loop per `web/jobbpilot-web/AGENTS.md`.
11. `pnpm build` pre-push-gate (RSC-boundary-ändringar).

**Agent-invocations innan STOPP-rapport till Klas:**
- code-reviewer (FE-only — Klas-direktiv §C utesluter BE-ändring i denna batch)
- design-reviewer (rendered-veto + DemoBanner + WelcomeModal + civic-ton)
- security-auditor (cookie-mekanism + cross-pollination-skydd + verifiera att gäst-tree inte läcker auth-yta)

### Klas måste explicit godkänna (innan kod-ändring):

1. **/jobb LIVE-frågan (Beslut 3).** Välj Alt 1 (Väg 2 — ADR 0005-amendment + agent-rondar + sen kod) eller Alt 2 (släpp utan /jobb LIVE, hide:a länken eller visa konverterings-CTA). Tills GO: leverera Punkt 5 enligt Alt 2 (`/gast/jobb` finns inte).

### Klas-override-tolerans

Klas kan välja Variant B (cookie-flag, samma URLs). CTO:s bedömning är att det är säkerhets-svagare och bryter mot etablerade principer (Least Common Mechanism, Fail-Safe Defaults). Om Klas väljer B: dokumenteras som medvetet val, accepteras, ingen ytterligare CTO-pushback. Men då måste varje `getServerSession()`-callsite (10+) granskas av security-auditor per ändring under hela feature-livstid — det är permanent disciplin-cost.

---

## Trade-offs accepterade samlat

- **URL-duplikat (`/oversikt` + `/gast/oversikt`):** acceptabelt mot säkerhets-isolering.
- **Komponent-presentational extraction-arbete:** sker när duplikat upptäcks, inte upfront (YAGNI).
- **Punkt 5 levereras potentiellt utan /jobb LIVE:** acceptabelt mot ADR-disciplin (Beslut 3 Alt 2). Beck/Fowler small-batches — bättre än bunta scope.
- **Cookie över localStorage:** acceptabelt mot SSR-flash-risk.

---

## Referenser

- Robert C. Martin, *Clean Architecture* (Prentice Hall, 2017) — kap. 7 (SRP), kap. 8 (OCP), kap. 13 (Component Cohesion: REP, CCP, CRP)
- Eric Evans, *Domain-Driven Design* (Addison-Wesley, 2003) — ch. 14 (Bounded Contexts)
- Jerome H. Saltzer & Michael D. Schroeder, "The Protection of Information in Computer Systems" (Proceedings of the IEEE, 1975) — Principle of Least Common Mechanism, Fail-Safe Defaults, Economy of Mechanism
- Andrew Hunt & David Thomas, *The Pragmatic Programmer* (Addison-Wesley, 1999) — DRY (kap. 7)
- Kent Beck (XP-litteratur) / Martin Fowler, *Refactoring* 2nd ed. (Addison-Wesley, 2018) — YAGNI + small-batches
- OWASP ASVS v5 (2024) — V3 Session Management, V4 Access Control
- EDPB Guidelines 2/2023 on Technical Scope of Art. 5(3) ePrivacy Directive — funktional-cookie-undantag
- Microsoft Learn / Next.js docs (v16) — App Router Route Groups, Server Components, Server Actions, cookie API
- JobbPilot ADR 0005 — Go-to-market-strategi (auth-gating av job-ads + bot-trafik-mätning som blocker)
- JobbPilot ADR 0017 — Defense-in-depth middleware + layout-verifiering
- JobbPilot ADR 0064 — Public-anonym aggregate read via Worker-precomputed Redis-cache (rate-limit-mönster för publika endpoints)
- JobbPilot CLAUDE.md §1 (civic-utility), §2.1 (Clean Arch), §5 (anti-patterns), §9.2 (agent-invocations), §9.4 (verbatim-källa), §9.6 (in-block-fix vs TD)

---

**Slut på CTO-dom.** CC: börja med Beslut 1, 4, 5 implementation. Eskalera Beslut 3 till Klas innan /jobb-relaterad kod skrivs.
