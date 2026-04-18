# Bedrock inference profiles — verifierat

> **Verifierat:** 2026-04-18
> **Konto:** `710427215829`
> **Verifierad av:** Session 3 steg 2g (SESSION-2-PLAN §14)
> **Regioner testade:** `eu-central-1` (Frankfurt), `eu-west-1` (Irland)

---

## 1. Sammanfattning

Alla tre Claude-modeller som BUILD.md §8.2 refererar finns som **EU cross-region
inference profiles** i både `eu-central-1` och `eu-west-1`. Model access är
redan `AUTHORIZED` i kontot — **ingen manuell AWS Console-request krävs**.

Detta är en förändring mot hur äldre Anthropic-modeller hanterades (explicit
opt-in per region). För Claude 4.x-serien räcker det att IAM-policyn tillåter
`bedrock:Converse*` mot profil-ARN:en.

---

## 2. Verifierade modeller

| Modell | Base model ID | EU inference profile ID | Källregioner | Status |
|---|---|---|---|---|
| Claude Haiku 4.5 | `anthropic.claude-haiku-4-5-20251001-v1:0` | `eu.anthropic.claude-haiku-4-5-20251001-v1:0` | `eu-central-1`, `eu-north-1`, `eu-south-1`, `eu-south-2`, `eu-west-1`, `eu-west-3` | AUTHORIZED, AVAILABLE |
| Claude Sonnet 4.6 | `anthropic.claude-sonnet-4-6` *(INGEN datumsuffix)* | `eu.anthropic.claude-sonnet-4-6` *(INGEN datumsuffix)* | samma 6 EU-regioner | AUTHORIZED, AVAILABLE |
| Claude Opus 4.7 | `anthropic.claude-opus-4-7` *(INGEN datumsuffix)* | `eu.anthropic.claude-opus-4-7` *(INGEN datumsuffix)* | samma 6 EU-regioner | AUTHORIZED, AVAILABLE |

**Status-semantik (via `aws bedrock get-foundation-model-availability`):**

- `authorizationStatus = AUTHORIZED` → kontot är godkänt att använda modellen
- `entitlementAvailability = AVAILABLE` → användningsrätt är aktiv
- `regionAvailability = AVAILABLE` → modellen är distribuerad i regionen
- `agreementAvailability.status = NOT_AVAILABLE` → *inget EULA-agreement krävs*
  (förväntat för Anthropic-modeller i EU — inte en blockerande flagga)

---

## 3. Rekommendation för JobbPilot

### 3.1 Systemflöden (icke-BYOK) — Bedrock EU-profil

Använd EU inference profile från backend i `eu-north-1`:

| Use case | Modell | Profil ID |
|---|---|---|
| Fast tier (CV-parse, cliche, rekommendationer) | Haiku 4.5 | `eu.anthropic.claude-haiku-4-5-20251001-v1:0` |
| Deep tier (CV-tailor, cover letter, match, research) | Sonnet 4.6 | `eu.anthropic.claude-sonnet-4-6` |
| Premium tier (optional, för tunga reasoning-flöden) | Opus 4.7 | `eu.anthropic.claude-opus-4-7` |

**Källregion för anrop:** `eu-north-1`. Profilen routar automatiskt till närmaste
EU-region med kapacitet. Ingen cross-region-kostnad tack vare EU-profilens
struktur.

### 3.2 BYOK — Anthropic direkt API

Endast där användaren samtyckt. Använd motsvarande Anthropic-API-model-ID:n
(utan `eu.`-prefix):

- `claude-haiku-4-5-20251001`
- `claude-sonnet-4-6`
- `claude-opus-4-7`

### 3.3 Fallback

Ingen fallback-strategi konfigurerad i v1. Om Bedrock EU-profil misslyckas,
appen returnerar felmeddelande på svenska snarare än att tyst routa till
direkt Anthropic (skulle bryta GDPR-löftet). Framtida ADR kan ompröva om
degraderad fallback är önskvärd.

---

## 4. ARN-format (dokumenterat för IAM-policies)

Profil-ARN:er använder **hemregionen** som anropar listningen, inte `*`:

```
arn:aws:bedrock:eu-central-1:710427215829:inference-profile/eu.anthropic.claude-sonnet-4-6
arn:aws:bedrock:eu-west-1:710427215829:inference-profile/eu.anthropic.claude-sonnet-4-6
```

Men IAM-policies tolererar wildcards i region-segmentet eftersom profilerna är
callable från vilken källregion som helst:

```json
"Resource": "arn:aws:bedrock:*:710427215829:inference-profile/eu.anthropic.claude-sonnet-4-6"
```

Detta är mönstret `modules/bedrock_model_access/main.tf` använder.

---

## 5. Terraform-implikation

Modulens `var.eu_inference_profile_ids`-default täcker Haiku + Sonnet idag.
Opus 4.7 saknas i default-listan — inte ett akut problem (v1 startar på
Haiku + Sonnet per BUILD.md §8.2). När Premium-tier faktiskt används läggs
`eu.anthropic.claude-opus-4-7` till i `terraform.tfvars` och `terraform apply`
körs om (policy-only change).

---

## 6. Commands som kördes

```bash
# Session
aws sts get-caller-identity --profile jobbpilot
# → AROA2K2GDA7KRAWJV2UYA:Klas @ 710427215829

# Profil-listning per region (filtrerad till Claude 4.x)
aws bedrock list-inference-profiles --region eu-central-1 --profile jobbpilot \
    --query "inferenceProfileSummaries[?contains(inferenceProfileId, 'sonnet-4') || contains(inferenceProfileId, 'haiku-4') || contains(inferenceProfileId, 'opus-4')]"

aws bedrock list-inference-profiles --region eu-west-1 --profile jobbpilot \
    --query "inferenceProfileSummaries[?contains(inferenceProfileId, 'sonnet-4') || contains(inferenceProfileId, 'haiku-4') || contains(inferenceProfileId, 'opus-4')]"

# Foundation models per region
aws bedrock list-foundation-models --region eu-central-1 --profile jobbpilot \
    --query "modelSummaries[?providerName=='Anthropic']"

# Access-status per modell (verifierade AUTHORIZED + AVAILABLE båda regionerna)
aws bedrock get-foundation-model-availability --region eu-central-1 \
    --model-id anthropic.claude-haiku-4-5-20251001-v1:0 --profile jobbpilot
aws bedrock get-foundation-model-availability --region eu-central-1 \
    --model-id anthropic.claude-sonnet-4-6 --profile jobbpilot
aws bedrock get-foundation-model-availability --region eu-central-1 \
    --model-id anthropic.claude-opus-4-7 --profile jobbpilot
# (samma upprepat i --region eu-west-1)
```

---

## 7. Full rådata

### 7.1 EU-profiler i `eu-central-1` (Claude 4.x+)

Följande profiler returnerades av `aws bedrock list-inference-profiles --region eu-central-1`.
Fält utanför verifieringens omfång (`createdAt`, `updatedAt`, övriga Claude-3-profiler) är
utelämnade — fullständig output finns i shell-historiken om återbesök behövs.

| `inferenceProfileId` | `inferenceProfileName` | `status` | Antal `models` |
|---|---|---|---|
| `eu.anthropic.claude-haiku-4-5-20251001-v1:0` | EU Anthropic Claude Haiku 4.5 | ACTIVE | 6 |
| `eu.anthropic.claude-sonnet-4-6` | EU Anthropic Claude Sonnet 4.6 | ACTIVE | 6 |
| `eu.anthropic.claude-opus-4-7` | EU Anthropic Claude Opus 4.7 | ACTIVE | 6 |
| `global.anthropic.claude-haiku-4-5-20251001-v1:0` | Global Anthropic Claude Haiku 4.5 | ACTIVE | 2 |
| `global.anthropic.claude-sonnet-4-6` | Global Anthropic Claude Sonnet 4.6 | ACTIVE | 2 |
| `global.anthropic.claude-opus-4-7` | Global Anthropic Claude Opus 4.7 | ACTIVE | 2 |
| `eu.anthropic.claude-opus-4-5-20251101-v1:0` | EU Anthropic Claude Opus 4.5 | ACTIVE | 6 |
| `eu.anthropic.claude-opus-4-6-v1` | EU Anthropic Claude Opus 4.6 | ACTIVE | 6 |
| `eu.anthropic.claude-sonnet-4-20250514-v1:0` | EU Claude Sonnet 4 | ACTIVE | 6 |
| `eu.anthropic.claude-sonnet-4-5-20250929-v1:0` | EU Anthropic Claude Sonnet 4.5 | ACTIVE | 6 |

### 7.2 EU-profiler i `eu-west-1`

Identiska profil-IDs som eu-central-1 (samma `SYSTEM_DEFINED` profiler — enda
skillnaden är ARN-homeregion). Confirms att vi kan anropa samma profiler från
vilken EU-region som helst.

### 7.3 Access-status per modell (båda regionerna)

Alla tre verifierade modeller:

```json
{
  "authorizationStatus": "AUTHORIZED",
  "entitlementAvailability": "AVAILABLE",
  "regionAvailability": "AVAILABLE",
  "agreementAvailability": { "status": "NOT_AVAILABLE" }
}
```

`agreementAvailability: NOT_AVAILABLE` är **inte** en blockerande flagga för
Anthropic — den indikerar att inget separat avtals-opt-in krävs. Andra providers
(t.ex. Meta Llama) kräver agreement, vilket syns som `PENDING` eller `AVAILABLE`.
