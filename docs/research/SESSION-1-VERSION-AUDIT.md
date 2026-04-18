# Session 1 — Version Audit av BUILD.md tech-stack

> **Status:** Research only. Ingen BUILD.md-ändring gjord.
> **Datum:** 2026-04-18
> **Scope:** BUILD.md §3.1 (Exakta versioner) och §3.2 (Infrastruktur) + AI-lagret (§8.2).
> **Systerdokument:** [`SESSION-1-FINDINGS.md`](./SESSION-1-FINDINGS.md)

---

## 0. Sammanfattning (executive)

| Kategori | Antal rader verifierade | Antal `OK` | Antal `UPDATE` | Antal `NEW MAJOR` | Antal `LICENSE CHANGE` / `FORK RECOMMENDED` |
|----------|------------------------:|------:|----------:|-----------:|-----------:|
| Backend  | 20 | 7 | 6 | 5 | 2 |
| Frontend | 12 | 7 | 3 | 2 | 0 |
| AI & cloud | 6 | 2 | 3 | 1 | 0 |
| Infra | 2 | 0 | 2 | 0 | 0 |

**Kritiska fel i BUILD.md:**

1. ❌ **".NET 10 GA ~nov 2026"** — fel. .NET 10 är GA **sedan 2025-11-11** och är LTS till 2028-11. Hela stacken ska byggas på .NET 10 / C# 14 / ASP.NET Core 10 / EF Core 10 från dag ett.
2. ❌ **MediatR 12.x utan licens-notis** — MediatR bytte till commercial license (Community + Enterprise) 2025-07-02. Kommersiell över USD 5M revenue. Vi bör byta till **Mediator.SourceGenerator (martinothamar/Mediator)**.
3. ❌ **QuestPDF "Community license free för JobbPilot"** — sant idag (under USD 1M revenue) men licensen är runtime-enforced via `QuestPDF.Settings.License = LicenseType.Community;`. Måste sättas i kod.
4. ❌ **"Next.js 15 (App Router)"** — Next.js 16 är GA sedan Q1 2026 och rekommenderad för nya projekt. Bump till 16.2.x.
5. ❌ **"Anthropic.SDK 5.x+ (community)"** — Anthropic släppte **officiell första-parts C# SDK** (package id `Anthropic`) i april 2026. Använd den istället för `tghamm/Anthropic.SDK`.
6. ❌ **`eu.anthropic.claude-*-<date>` som placeholder** — behöver konkreta model-ID:n. Se §3 nedan.
7. ⚠️ **FluentAssertions** — används inte i BUILD.md direkt, men §17.1 refererar "FluentAssertions". Den blev Xceed Community License (USD 130/dev/yr från v8) 2025. CLAUDE.md §17 behöver uppdateras. **Rekommenderar Shouldly**.

---

## 1. Backend (§3.1 första del)

| # | Komponent | BUILD.md säger | Senaste stable (2026-04-18) | Status | Källa |
|---|-----------|----------------|------------------------------|--------|-------|
| 1 | **.NET runtime** | 9.0 (migrera till 10.0 vid GA ~nov 2026) | **10.0.6 (LTS till 2028-11-14)** | ❌ **UPDATE — BUILD.md claim är FEL** | [devblogs.microsoft.com](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/), [dotnet.microsoft.com policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| 2 | **C# språk** | 13 (12 fallback) | **14** | UPDATE | [learn.microsoft.com C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) |
| 3 | **ASP.NET Core** | 9 | **10.0.6** | UPDATE | [nuget.org Microsoft.AspNetCore.App.Ref](https://www.nuget.org/packages/Microsoft.AspNetCore.App.Ref) |
| 4 | **EF Core** | 9 | **10.0.6** | UPDATE | [nuget.org Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) |
| 5 | **MediatR** | 12.x | **14.1.0** | ⚠️ **LICENSE CHANGE / FORK RECOMMENDED** | [jimmybogard.com](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/), [nuget MediatR](https://www.nuget.org/packages/MediatR) |
| 6 | **FluentValidation** | 11.x | **12.1.1** | OK (Apache-2.0) | [nuget](https://www.nuget.org/packages/FluentValidation), [github](https://github.com/FluentValidation/FluentValidation) |
| 7 | **Mapster** | 7.x | **10.0.7** | NEW MAJOR | [nuget](https://www.nuget.org/packages/Mapster) |
| 8 | **Hangfire** | 1.8.x | **1.8.23 (OSS)** | OK | [nuget](https://www.nuget.org/packages/Hangfire) |
| 9 | **Ardalis.SmartEnum** | 8.x | **8.2.0** | OK (low activity, feature-complete) | [nuget](https://www.nuget.org/packages/Ardalis.SmartEnum) |
| 10 | **Serilog** | 4.x | **4.3.1** | OK | [nuget](https://www.nuget.org/packages/Serilog) |
| 11 | **OpenTelemetry .NET** | 1.10+ | **1.15.2** | UPDATE | [nuget](https://www.nuget.org/packages/OpenTelemetry) |
| 12 | **PdfPig** | 0.1.10+ | **0.1.14** | OK | [nuget](https://www.nuget.org/packages/PdfPig) |
| 13 | **DocumentFormat.OpenXml** | 3.x | **3.5.1** | OK | [nuget](https://www.nuget.org/packages/DocumentFormat.OpenXml) |
| 14 | **QuestPDF** | 2024.x | **2026.2.4** | UPDATE (licens-setup krävs) | [questpdf.com/license](https://www.questpdf.com/license/), [nuget](https://www.nuget.org/packages/QuestPDF) |
| 15 | **AWSSDK.BedrockRuntime** | 3.7.x+ | **4.0.17.3** | NEW MAJOR | [nuget](https://www.nuget.org/packages/AWSSDK.BedrockRuntime) |
| 16 | **Anthropic SDK (.NET)** | `Anthropic.SDK 5.x+` (community) | **`Anthropic` 12.16.0 (officiell)** | ⚠️ UPDATE — byt till officiell | [github anthropic-sdk-csharp](https://github.com/anthropics/anthropic-sdk-csharp), [nuget Anthropic](https://www.nuget.org/packages/Anthropic) |
| 17 | **Refit** | 7.x | **10.1.6** | NEW MAJOR | [nuget](https://www.nuget.org/packages/Refit) |
| 18 | **PostgreSQL** | 17.x | **18.3** | UPDATE | [aws.amazon.com PG18 GA](https://aws.amazon.com/about-aws/whats-new/2025/11/amazon-rds-postgresql-major-version-18/), [aws PG 18.3 minor](https://aws.amazon.com/about-aws/whats-new/2026/02/rds-minor-version-18-3-17-9-16-13-15-17-14-22/) |
| 19 | **Redis** | 7.4 | **8.6.2** | UPDATE (license caveat) | [github releases](https://github.com/redis/redis/releases), [redis.io licenses](https://redis.io/legal/licenses/) |
| 20 | **Npgsql.EntityFrameworkCore.PostgreSQL** | (implicit) | **10.0.1** | OK (måste följa EF Core 10) | [nuget](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) |

### 1.1 Kommentarer på backend-stacken

**Rad 1–4 (.NET 10-sviten):** BUILD.md antar att .NET 10 kommer senare. Den är redan här sedan november 2025 och är den stabila LTS-linjen till nov 2028. .NET 9 (STS) går out of support i maj 2026 — vi har bara veckor kvar på 9. Vi SKA starta JobbPilot på .NET 10 / C# 14 / ASP.NET Core 10 / EF Core 10.

**Rad 5 (MediatR):** Jimmy Bogard (skapare) flyttade MediatR + AutoMapper under Lucky Penny Software 2025-07-02. Nytt namn är "Community edition vs Enterprise". Community är gratis för organisationer under **USD 5M gross annual revenue**. JobbPilot kvalificerar idag men är inte framtidssäker. **Rekommendation: byt till [martinothamar/Mediator](https://github.com/martinothamar/Mediator)** — MIT-licens, source-generated dispatch (noll runtime-reflection, snabbare än MediatR), samma pipeline behavior-API. Byte är smärtfritt på greenfield.

**Rad 6 (FluentValidation):** Apache-2.0 fortfarande. **OBS — förväxla inte med FluentAssertions** som blev commercial 2025 (Xceed Community License). CLAUDE.md §17 nämner "FluentAssertions" implicit i testing-kontext; bör uppdateras. **Rekommendation: Shouldly (gratis, LMS använder det, bra svensk utvecklar-DX)**.

**Rad 14 (QuestPDF):** Licensen är runtime-enforced. Vid startup måste man sätta:
```csharp
QuestPDF.Settings.License = LicenseType.Community;
```
Annars får man en exception. Community MIT gäller organisationer under **USD 1M annual gross revenue**. JobbPilot är solo/pre-revenue → OK. Anteckna i ADR.

**Rad 15 (AWS SDK):** v4 är nu aktuell major. Viktig för Bedrock **`Converse`-API** som är GA och normaliserar tool-use + streaming över Claude Haiku/Sonnet. Inget behov att ligga kvar på v3.7.

**Rad 16 (Anthropic SDK):** Anthropic släppte officiell första-parts C# SDK under package id `Anthropic` i april 2026 (hittar `Anthropic 12.16.0` publicerad 2026-04-16, samma dag som Opus 4.7 GA). Detta är en strong signal — de investerar i .NET-klienten nu. Använd officiell. Wrappern `tghamm/Anthropic.SDK` fungerar fortfarande (MIT) men redundant.

**Rad 18 (PostgreSQL):** PG 18 är GA på RDS sedan nov 2025, minor 18.3 (som fixar en community-regression) är default sedan feb 2026. EF Core 10 + Npgsql 10 stödjer PG 18. Starta på 18.3.

**Rad 19 (Redis):** Redis 8 är tri-licensed (RSALv2 / SSPLv1 / AGPLv3). Eftersom JobbPilot använder Redis som extern cache (ElastiCache, inte embedding) gäller inga restriktioner. Valkey är BSD-licensierad fork om licensparanoia blir en issue — ej rekommenderat nu, Redis 8 är snabbare.

**Rad 20 (Npgsql):** Npgsql.EntityFrameworkCore.PostgreSQL har peer-dependency `>= 10.0.4 && < 11.0.0` på EF Core → bumpar .NET 10 tvingar denna till 10.x. Fine.

---

## 2. Frontend (§3.1 andra del)

| # | Komponent | BUILD.md säger | Senaste stable (2026-04-18) | Status | Källa |
|---|-----------|----------------|------------------------------|--------|-------|
| 1 | **Next.js** | 15 (App Router) | **16.2.3** | ⚠️ NEW MAJOR | [nextjs.org/blog/next-16](https://nextjs.org/blog/next-16) |
| 2 | **React** | (implicit via Next 15) | **19.2.5** | OK | [react.dev blog](https://react.dev/blog/2025/10/01/react-19-2) |
| 3 | **TypeScript** | 5.6+ | **6.0.3** | NEW MAJOR | [npmjs.com typescript](https://www.npmjs.com/package/typescript) |
| 4 | **shadcn/ui** | "senaste" | **CLI v4 (mars 2026)** | UPDATE (still CLI-copy, new registry:base) | [ui.shadcn.com changelog](https://ui.shadcn.com/docs/changelog/2026-03-cli-v4) |
| 5 | **Tailwind CSS** | 4 | **4.2.0 (2026-02-18)** | OK | [github releases](https://github.com/tailwindlabs/tailwindcss/releases) |
| 6 | **TanStack Query** | 5.x | **5.99.0** | OK | [npmjs @tanstack/react-query](https://www.npmjs.com/package/@tanstack/react-query) |
| 7 | **TanStack Table** | 8.x | **8.21.3** (v9 alpha exists) | OK — stanna på 8 | [npmjs @tanstack/react-table](https://www.npmjs.com/package/@tanstack/react-table) |
| 8 | **React Hook Form** | latest | **7.72.1** (v8 beta) | OK — pin ^7.72 | [npmjs react-hook-form](https://www.npmjs.com/package/react-hook-form) |
| 9 | **Zod** | latest | **4.3.6** | UPDATE (Zod 4 är GA) | [npmjs zod](https://www.npmjs.com/package/zod) |
| 10 | **NextAuth.js / Auth.js** | 5 beta | **v5 (fortfarande beta-tag, prod-used)** | OK — treat as production | [github discussion 13382](https://github.com/nextauthjs/next-auth/discussions/13382) |
| 11 | **date-fns** | 4.x | **4.1.0** (ingen ny release ~2 år) | OK | [npmjs date-fns](https://www.npmjs.com/package/date-fns) |
| 12 | **Lucide React** | latest | **1.8.0 (2026-04-11)** | UPDATE | [npmjs lucide-react](https://www.npmjs.com/package/lucide-react) |

### 2.1 Kommentarer på frontend-stacken

**Rad 1 (Next.js 16):** Release Q1 2026. Stabilt Turbopack, stable Adapter API, AI-optimerad `create-next-app`. Inget drama för oss — App Router-mönstret är detsamma. Rekommendation: `"next": "^16.2.0"`.

**Rad 3 (TypeScript 6):** Släpptes 2026-04-16 — supernytt. TS 6 är **sista JS-codebase-releasen** före TS 7 (Go-rewrite). Uppgradering från 5.x har minimal breakage. Kan vänta 1–2 veckor om man vill låta ekosystemet settla, men annars OK att starta på 6.0.x.

**Rad 4 (shadcn/ui):** Fortsatt code-distribution (copy-in-repo). Nytt CLI v4 har `registry:base` som kan dra ett helt design-system (components + deps + tokens + fonts) i en payload. Relevant för oss när vi setuppar vårt civic-design-system.

**Rad 9 (Zod):** Zod 4 är nu huvudlinjen. Root-package exportar v4. `zod/v3` finns som subpath om något legacy-bibliotek kräver v3.

**Rad 10 (Auth.js v5):** Trots "beta"-taggen är det den underhållna linjen och krävs för Next.js 16. Treat som production. OBS — CLAUDE.md §13.5 säger "JWT i localStorage förbjudet"; Auth.js v5 stöder cookie-based sessions by default, perfekt.

**Rad 12 (Lucide React):** Crossade 1.0 nu. Pin till `^1.8`.

---

## 3. AI & cloud (§3.2 + §8.2)

| # | Komponent | BUILD.md säger | Senaste / verifierat | Status | Källa |
|---|-----------|----------------|------------------------|--------|-------|
| 1 | **Claude Haiku 4.5 Bedrock model ID (EU profile)** | `eu.anthropic.claude-haiku-4-5-<date>` | **`eu.anthropic.claude-haiku-4-5-20251001-v1:0`** | UPDATE (fyll i konkret) | [AWS Bedrock inference profiles](https://docs.aws.amazon.com/bedrock/latest/userguide/inference-profiles-support.html) |
| 2 | **Claude Haiku 4.5 Anthropic direct model ID** | `claude-haiku-4-5-20251001` | **`claude-haiku-4-5-20251001`** | OK | [platform.claude.com models](https://platform.claude.com/docs/en/about-claude/models/) |
| 3 | **Claude Sonnet 4.6 Bedrock model ID (EU profile)** | `eu.anthropic.claude-sonnet-4-6-<date>` | **`eu.anthropic.claude-sonnet-4-6`** (INGEN date-suffix) | ⚠️ UPDATE — viktigt | [AWS Bedrock model card sonnet 4.6](https://docs.aws.amazon.com/bedrock/latest/userguide/model-card-anthropic-claude-sonnet-4-6.html) |
| 4 | **Claude Sonnet 4.6 Anthropic direct model ID** | `claude-sonnet-4-6` | `claude-sonnet-4-6` (bekräfta dated snapshot på [platform.claude.com](https://platform.claude.com/docs/en/about-claude/models/)) | ⚠️ verifiera | [platform.claude.com](https://platform.claude.com/docs/en/about-claude/models/) |
| 5 | **AWS Bedrock EU-regioner för Claude** | eu-central-1 eller eu-west-1 | **EU cross-region profile**: eu-central-1, eu-north-1, eu-south-1, eu-south-2, eu-west-1, eu-west-3 | UPDATE — eu-north-1 (Stockholm) är nu ett giltigt source-region | [AWS Bedrock inference profiles](https://docs.aws.amazon.com/bedrock/latest/userguide/inference-profiles-support.html) |
| 6 | **Anthropic API version header** | `2023-06-01` | **`2023-06-01`** | OK | [platform.claude.com versioning](https://platform.claude.com/docs/en/api/versioning) |
| 7 | **AWS RDS PostgreSQL senaste supported** | 17.x | **18.3** | UPDATE | [aws.amazon.com PG 18.3 minor](https://aws.amazon.com/about-aws/whats-new/2026/02/rds-minor-version-18-3-17-9-16-13-15-17-14-22/) |
| 8 | **AWS Bedrock SDK API** | (InvokeModel implicit) | **Converse / ConverseStreamAsync GA i SDK v4** | UPDATE — byt från InvokeModel till Converse | [AWS SDK for .NET v4 Bedrock examples](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/csharp_bedrock-runtime_code_examples.html) |

### 3.1 Opus 4.7 — ny modell att räkna med

BUILD.md §8.2-tabellen använder Sonnet 4.6 för Deep tier. Opus 4.7 släpptes **2026-04-16 (två dagar sedan)**. Frågan är om vi vill routea vissa Deep-operationer till Opus istället för Sonnet.

| Use case | BUILD.md säger | Fråga för Klas |
|----------|----------------|----------------|
| CV-tailoring (Deep) | Sonnet 4.6 | Troligen räcker Sonnet. Opus ~5x dyrare. |
| Cover letter-generering | Sonnet 4.6 | Troligen Sonnet. |
| Research-brief med web_search | Sonnet 4.6 | Kanske Opus här? — reasoning-tung. |
| Deep match-score | Sonnet 4.6 | Sonnet räcker. |

**Rekommendation:** Starta allt på Sonnet 4.6. Om kvaliteten är otillräcklig på research-brief (ägnar mest reasoning), lägg in Opus 4.7 som optional high-tier. Lägg `Premium: opus-4-7` som config-key i `Ai.Bedrock.ModelIds` så vi kan routea utan kodändring.

### 3.2 Viktig detalj om Sonnet 4.6 Bedrock-profil

Subagenten flaggade att `eu.anthropic.claude-sonnet-4-6` använder **alias-form utan date-suffix** — om vi lägger på fake `-YYYYMMDD-v1:0` får vi 400. Detta är en konvention-skillnad mellan olika model IDs. **Dubbelverifiera innan vi skriver config i fas 0.**

### 3.3 EU-regioner — bra nyhet

BUILD.md §3.2 antar att Bedrock måste köras från eu-central-1 eller eu-west-1 och att backend i eu-north-1 (Stockholm) → cross-region. Med EU cross-region inference profile har vi nu **eu-north-1 som giltigt source region**. Det innebär:
- Backend i eu-north-1 (Stockholm, svensk data-suveränitet).
- Bedrock-anrop går genom EU-profilen.
- Inget cross-region-problem.
- Fortfarande inom EU-data-lokaliseringsgränsen.

Detta stärker vårt GDPR-narrativ ytterligare.

### 3.4 Verifierade Bedrock modell-ID (Klas review, 2026-04-18)

Klas dubbel-verifierade base model IDs mot AWS docs 2026-04-18. Dessa ersätter de placeholder-värden som stod i BUILD.md §8.2 och överskrider det min subagent hittade (bekräftar Sonnet 4.6-utan-datumsuffix-mönstret).

**Verifierade base model IDs:**
- `anthropic.claude-sonnet-4-6` — **INGEN datumsuffix**
- `anthropic.claude-haiku-4-5-20251001-v1:0`
- `anthropic.claude-sonnet-4-5-20250929-v1:0` (tidigare generation, referens)

**EU inference profile-prefix-mönster:** `eu.anthropic.<base-model-id>` (t.ex. `eu.anthropic.claude-sonnet-4-6`).

⚠️ **STOPPUNKT — verifiering i session 3 steg 2:**

Exakta inference profile-ARNs **varierar per källregion** och får INTE cachas från docs eller blogg. När AWS-kontot finns (session 3, efter Klas har kört §6.9-setupen i `SESSION-1-FINDINGS.md`) ska följande köras:

```
aws bedrock list-inference-profiles --region eu-north-1
```

Output dokumenteras i en ny fil `docs/research/bedrock-inference-profiles.md` — detta blir sanningen som BUILD.md §8.2 citerar. Innan dess ska inga exakta ARNs skrivas in i `appsettings.json`-exemplen i BUILD.md.

**Översikt:** session 3 verifierar både att (a) Sonnet 4.6 EU-profile verkligen saknar datumsuffix i den regionens listning, (b) Haiku 4.5-datumsuffix matchar `20251001-v1:0` i EU-profilen, och (c) vilken exakt profile-ARN som får lägsta latens från eu-north-1.

### 3.5 Andra version-deltan från Klas review

- **Anthropic C# SDK senaste version:** Klas verifierade **12.11.0** (rad 16 i tabellen listade 12.16.0 från subagent-data). Mindre delta, troligen en minor-release mellan sökningarna. Session 3 bekräftar aktuell senaste när `dotnet add package Anthropic` körs och pinnar det värdet i `.csproj`.
- **MediatR v13+ licens-detalj:** commercial under **RPL-1.5** (Reciprocal Public License 1.5), inte oklart "commercial license" som ursprungsraden i tabellen antydde. Community-edition under USD 5M revenue. Beslut oförändrat: byt ändå till Mediator.SourceGenerator (se `SESSION-1-FINDINGS.md` §6.4).
- **FluentAssertions-situation:** bekräftad (Xceed Community License, USD 130/dev/år från v8). `SESSION-1-FINDINGS.md` §7.4 beslutar **Shouldly** som ersättning.

---

## 4. Infrastruktur (§3.2)

| # | Komponent | BUILD.md säger | Senaste / verifierat | Status | Källa |
|---|-----------|----------------|------------------------|--------|-------|
| 1 | **Terraform** | 1.9+ | **1.14.8** (1.15-rc2 i RC) | UPDATE | [github hashicorp/terraform releases](https://github.com/hashicorp/terraform/releases) |
| 2 | **Docker Compose** | Compose v2 | **Compose Specification v5.0.0 (dec 2025)** | UPDATE | [docs.docker.com compose releases](https://docs.docker.com/compose/releases/release-notes/) |

### 4.1 Kommentarer

**Terraform 1.14:** IBM-förvärv av HashiCorp (stängt 2025) har **inte** ändrat BSL-licens-villkoren för Terraform CLI än. Vi kan fortsätta använda Terraform utan oro. OpenTofu (OSS fork) är ett alternativ om licensparanoia blir en issue, men Terraform 1.14 är fine.

**Docker Compose:** Compose v2 är integrerat i Docker Desktop/Engine sedan länge. I april 2026: använd Compose Specification v5. Ta bort `version:` top-level-keyen (obsolete, varnar). Använd `depends_on` med `condition: service_healthy` för postgres/redis/seq startup-ordning. Builds delegar till Docker Bake.

---

## 5. Rekommenderad patch-plan för BUILD.md

**Första-order (kritiska, gör i session 2):**

1. Skriv om §3.1-tabellen med .NET 10 / C# 14 / ASP.NET Core 10 / EF Core 10 / Next.js 16.2.x / TypeScript 6.0.x.
2. Byt MediatR → Mediator.SourceGenerator (martinothamar). Uppdatera §4.4 och §17 där MediatR-pipeline nämns.
3. Byt Anthropic.SDK → officiell `Anthropic`-package.
4. Uppdatera AWS SDK Bedrock → v4 + Converse-API. Uppdatera §9.6.
5. Fyll i konkreta model IDs i §8.2 med verifierade Bedrock-profiler.
6. Lägg till PostgreSQL 18.3 i §3.1 och §3.2.
7. Bump Redis 7.4 → 8.6.x.
8. Bump OpenTelemetry → 1.15+.
9. Bump Mapster, Refit, Lucide React.
10. Klargör QuestPDF licens-setup kod (`QuestPDF.Settings.License = LicenseType.Community;`).

**Andra-order (bör göras i session 2 men mindre kritiskt):**

11. Lägg till en licens-notis-kolumn i §3.1 (MediatR, FluentAssertions-ersättare, QuestPDF, Redis).
12. Uppdatera CLAUDE.md §17.1: byt "FluentAssertions" mot Shouldly (eller pinna FluentAssertions 7 om Klas föredrar).
13. Uppdatera Bedrock EU-regioner-texten i §3.2 och §9.6 — eu-north-1 nu giltigt source.
14. Lägg till Opus 4.7 som optional "Premium" tier i §8.2.
15. BUILD.md §18 (fas-roadmap) refererar ".NET 10 vid ~nov 2026" — ta bort helt.

**Tredje-order (kan vänta till senare session):**

16. Uppdatera Dockerfile-mall i infra-docs med Compose Specification v5 + Docker Bake.
17. Uppdatera CI/CD-templates i §15.3 med `actions/setup-dotnet@v4` + `dotnet-version: "10.0.x"`.
18. Ta ställning till Opus 4.7 för enskilda use-cases i §8.2.

---

## 6. Konkreta kodsnuttar att uppdatera

### 6.1 BUILD.md §3.1 tabell — föreslagen diff (högnivå)

```diff
- | Backend runtime | .NET | 9.0 (migrera till 10.0 vid GA ~nov 2026) | LTS, stabilt |
- | Språk (backend) | C# | 13 (12 fallback) | Primära funktioner, records, pattern matching |
- | Backend framework | ASP.NET Core | 9 | Minimal API |
- | ORM | EF Core | 9 | Npgsql-provider |
+ | Backend runtime | .NET | 10 (LTS till 2028-11) | GA sedan 2025-11-11 |
+ | Språk (backend) | C# | 14 | Extension members, field keyword GA, null-conditional assignment |
+ | Backend framework | ASP.NET Core | 10 | Minimal API |
+ | ORM | EF Core | 10 | Npgsql-provider 10.x |

- | Mediator | MediatR | 12.x | CQRS pipeline |
+ | Mediator | Mediator (martinothamar) | 3.x | Source-generated CQRS, MIT |

- | AI SDK (direkt) | Anthropic.SDK | 5.x+ (community) eller egen HTTP-klient | BYOK-flöde |
+ | AI SDK (direkt) | Anthropic (officiell) | 12.x | Första-parts, MIT |

- | AI SDK (Bedrock) | AWSSDK.BedrockRuntime | 3.7.x+ | Primary för systemnyckel |
+ | AI SDK (Bedrock) | AWSSDK.BedrockRuntime | 4.x (Converse API) | Primary för systemnyckel |

- | Mapping | Mapster | 7.x | Snabbare än AutoMapper, kodgenerering |
+ | Mapping | Mapster | 10.x | Snabbare än AutoMapper, kodgenerering |

- | Observability | OpenTelemetry | 1.10+ | Traces + metrics |
+ | Observability | OpenTelemetry | 1.15+ | Traces + metrics |

- | HTTP | HttpClientFactory + Refit | 7.x | JobTech-klient |
+ | HTTP | HttpClientFactory + Refit | 10.x | JobTech-klient |

- | Database | PostgreSQL | 17.x | RDS, Sweden region |
+ | Database | PostgreSQL | 18.3 | RDS eu-north-1, Sweden region |

- | Cache | Redis | 7.4 | ElastiCache |
+ | Cache | Redis | 8.6 | ElastiCache |

- | Frontend framework | Next.js | 15 (App Router) | SSR + ISR |
+ | Frontend framework | Next.js | 16.2 (App Router) | SSR + ISR |

- | Språk (frontend) | TypeScript | 5.6+ | Strict mode |
+ | Språk (frontend) | TypeScript | 6.0 | Strict mode |

- | Ikoner | Lucide React | latest | Minimalistiskt, civic-vänligt |
+ | Ikoner | Lucide React | ^1.8 | Minimalistiskt, civic-vänligt |
```

### 6.2 BUILD.md §8.2 — fyll i model IDs

```diff
  "Bedrock": {
-   "Region": "eu-central-1",
+   "Region": "eu-north-1",
    "ModelIds": {
-     "Fast": "eu.anthropic.claude-haiku-4-5-<date>",
-     "Deep": "eu.anthropic.claude-sonnet-4-6-<date>"
+     "Fast": "eu.anthropic.claude-haiku-4-5-20251001-v1:0",
+     "Deep": "eu.anthropic.claude-sonnet-4-6"
    }
  },
  "AnthropicDirect": {
    "ApiVersion": "2023-06-01",
    "InferenceGeo": "global",
    "Models": {
      "Fast": "claude-haiku-4-5-20251001",
      "Deep": "claude-sonnet-4-6"
    }
  }
```

> **NB**: Sonnet 4.6 Bedrock-profilen använder **alias utan datum-suffix**. Dubbelverifiera mot AWS Bedrock-model-cards-sidan innan vi commitar kod.

### 6.3 QuestPDF license-setup (för dokumentation)

```csharp
// Någonstans i Program.cs eller bootstrap
QuestPDF.Settings.License = LicenseType.Community;
// ADR-referens: docs/ADR/0011-questpdf-community-license.md
```

---

## 7. Provenance & osäkerheter

**Källor använda:**
- NuGet.org för .NET-paketversioner (verifierat per paket).
- nextjs.org, react.dev, tailwindcss.com för frontend-majors.
- npmjs.com för TS/React-ekosystem.
- docs.aws.amazon.com för Bedrock + RDS.
- platform.claude.com + anthropic.com för Opus 4.7 + model IDs.
- github.com/hashicorp/terraform, github.com/redis/redis för infrastructure.

**Osäkerhetsflaggor (verifiera innan kod commits):**
- Sonnet 4.6 Bedrock alias exakt form — hämta via `aws bedrock list-inference-profiles --region eu-north-1` innan vi pinnar i config.
- Anthropic officiella C# SDK `Anthropic 12.16.0` — vi antog major-hoppet reflekterar en ny semver-linje; kolla `CHANGELOG.md` i repot för att förstå vilken API-yta 12.x introducerar.
- TypeScript 6.0.3 ("släppt 2026-04-16") — supernytt. Vänta 7–14 dagar och pinna 6.0.x.patch innan deploy.
- Next.js 16.2.3 exakt version — kör `npx create-next-app@latest` i fas 0 och ta vad det ger som sanning.
- Lucide React 1.8 — största jump i hela auditen (0.562 → 1.8). Verifiera att det verkligen är `lucide-react`-paketet och inte ny `lucide`-distributionsform.

---

**Slut på SESSION-1-VERSION-AUDIT.md.** Återgå till [`SESSION-1-FINDINGS.md`](./SESSION-1-FINDINGS.md) för bredare kontext.
