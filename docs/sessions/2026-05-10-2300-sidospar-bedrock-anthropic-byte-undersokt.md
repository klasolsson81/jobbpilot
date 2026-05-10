---
session: Sidospår — Bedrock → Anthropic provider-byte undersökt och AVFärdat
datum: 2026-05-10
slug: sidospar-bedrock-anthropic-byte-undersokt
status: AVFärdat (web-search omkullkastade premissen — Anthropic API direct har ingen GA EU-residency-endpoint per 2026-05)
commits:
  - (denna session-logg, separat sidospårs-commit)
---

# Sidospår — Bedrock → Anthropic provider-byte undersökt

## Bakgrund

Klas hade en gammal prompt från webb-Claude (2026-05-09-original) som föreslog
provider-byte från AWS Bedrock till Anthropic API direct. Premissen:

> *"Anthropic API direct via api.eu.anthropic.com med inference_geo='eu' ger
> samma GDPR-compliance som Bedrock cross-region inference profiles."*

Klas valde att köra discovery + analys utan webb-Claude (vi klarade oss på
CC + agent-reviews + min web-search för verifiering).

## Discovery (kort)

44 filer berörda, **0 i `src/**`** (IAiProvider-impl planerad för Fas 4
men ej skriven än). Spec/IaC/docs primärt. Provider-byte hade varit
low-friction från ren refactoring-synvinkel.

Fil-distribution:
- TERRAFORM: 11 (modules/bedrock_model_access/ + prod-konsumenter + iam_ecs-policy-attach)
- SPEC: 3 (BUILD.md, README.md, CLAUDE.md)
- ADR: 6 (0001, 0002, 0005, 0025, 0026, decisions/README)
- RUNBOOK: 1 (aws-setup.md §3.1)
- RESEARCH: 5 (bedrock-inference-profiles + SESSION-1/2 historik)
- .claude: 4 (ai-prompt-engineer + README + settings + security-auditor)
- HISTORY (KEEP-AS-IS): 11 (sessions + reviews + tech-debt + steg-tracker)

## Web-search-fynd (2026-05-10) — SHOWSTOPPER

Sökningar mot Anthropic-docs, GitHub-issues, AWS-blogs, och Anthropic Privacy
Center gav följande nyckelfakta:

### 1. `api.eu.anthropic.com` är **inte GA**

Endpoint är "proposed as configurable" i Cowork/Claude.ai Enterprise per
GitHub-issues #40526 + #40530. **"Requested feature rather than currently
available functionality."**

### 2. Anthropic API direct har bara `us` + `global` inference geos

> *"Currently, only 'us' and 'global' inference geos are available at launch,
> with additional regions to be added over time. The direct Anthropic API
> offers only 'us' and 'global' inference geographies — there is no dedicated
> EU-only option yet."*

### 3. EU-Data-Boundary-status

> *"Data is stored in the US. Microsoft has confirmed that Anthropic-processed
> requests are excluded from the EU Data Boundary and from in-country
> processing guarantees. Organizations handling personal data under GDPR must
> either leave Anthropic disabled or implement a lawful transfer mechanism."*

### 4. EU-data-residency-vägar som **fortfarande är giltiga**

- ✅ **AWS Bedrock + EU inference profiles** — vad vi har redan
- ✅ Google Cloud Vertex AI EU
- ⏳ Microsoft Foundry EU — "Coming 2026" (inte GA än)

### 5. Anthropic .NET SDK

Officiell NuGet-paket `Anthropic` v12.20.0. Konfigureras via
`ANTHROPIC_BASE_URL` env-var. **Ingen dokumenterad EU-region-konfig** för
API direct.

## Beslut (Klas, 2026-05-10 ~23:00)

**Behåll Bedrock för system-key. BYOK = Anthropic API direct.**

Detta är **status quo** i praktiken — exakt vad BUILD.md §8 + README.md
§Säkerhet redan beskriver:

- System-key-flöden (CV-parsing, tailoring, cover-letter, etc.) går via
  AWS Bedrock med EU cross-region inference profile (eu-central-1/eu-west-1
  callable från eu-north-1)
- BYOK-flöden där användaren samtyckt går via Anthropic API direct (global
  routing accepteras explicit av användaren)
- Subprocessor-kedjan: AWS (eu-north-1) som primär, Anthropic (Bedrock EU)
  via cross-region — inga US-baserade processors för system-key-PII

**Ingen IaC-ändring. Ingen spec-ändring. Inga ADR-edits.**

## Varför undersökningen var värd tiden

Discovery + web-search **förhindrade en GDPR-degradering** som skulle ha
brutit BUILD.md §13 + README.md §Säkerhet + ADR 0024 (audit-retention +
Art. 17 cascade). Web-search-disciplinen i CLAUDE.md §9.5 gjorde sitt
jobb — premiss-verifying mot 2026-05-data fångade en outdated webb-Claude-
ADR-prompt från 2026-05-09 (samma dag, men marknaden rörs snabbt).

## Pekare för framtida revisit

Om/när någon av dessa triggers inträffar — revisita provider-byte:

1. **Anthropic announcer `api.eu.anthropic.com` som GA** med dokumenterad
   EU-data-residency-garanti
2. **`inference_geo="eu"` blir GA** för Anthropic API direct (idag: bara
   "us" + "global")
3. **Microsoft Foundry EU GA** (Anthropic-listade som "Coming 2026" —
   alternativ EU-vag)
4. **Klas-strategi-skifte**: om Klas:s GDPR-position skiftar (t.ex. om
   produkten flyttar bort från svensk arbetsmarknad), kan US-data-residency
   accepteras med SCC + lawful transfer mechanism per Art. 44-46

Inga TDs lyfta — status quo är bevarad och ingen drift-skuld.

## Källor (web-search 2026-05-10)

- Anthropic Privacy Center: "Where are your servers located?" — privacy.claude.com
- GitHub anthropics/claude-code issues #40526 + #40530 (Cowork EU residency)
- AWS Blogs Switzerland/Austria: cross-region inference for EU data processing
- Anthropic supported countries page
- Microsoft Q&A: Foundry Anthropic Azure EU timeline
- NuGet: Anthropic 12.20.0 (officiell .NET SDK)

## Tidsåtgång

~30 min (discovery 15 min + web-search 5 min + beslut 5 min + denna logg 5 min).

## Nästa session

Tillbaka till **Fas 1** (Core Domain) per STEG 14c-stäng-rapport. Ingen
påverkan på Fas 1-scope.
