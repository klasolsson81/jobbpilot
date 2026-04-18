# Issue: Centraliserad hantering av Claude-modellversioner

> **Skapad:** 2026-04-18, session 3 (efter STEG 5.11)
> **Status:** Open — ska tacklas efter session 3 är klar
> **Prioritet:** Medium (inget blockerar nuvarande arbete, men bör lösas innan Fas 1)
> **Relaterar till:** ADR 0002 (ska fyllas i), framtida skills i Fas 1

---

## Problem

Claude-modellnamn är hårdkodade på många platser i JobbPilot-repot:

| Plats | Räkning | Exempel |
|-------|---------|---------|
| `.claude/agents/*.md` (frontmatter) | 11 filer | `model: claude-opus-4-7` |
| `.claude/settings.json` | 1 fil | allowlist av modell-IDs |
| `prompts/*.prompt.md` (kommer i Fas 1) | N filer | `model:` + `inference_profile:` |
| BUILD.md §7 (AI-stack) | 1 sektion | Dokumenterade modeller |
| ADR 0002 | 1 fil | Policy-dokument |
| `docs/research/bedrock-inference-profiles.md` | 1 fil | Verifierade profile-IDs |
| Backend-kod (Fas 1+) | flera filer | Bedrock-anrop |

När Anthropic släpper nya modeller (Sonnet 4.7, Opus 5, etc.) måste alla dessa platser uppdateras manuellt, vilket är felbenäget.

---

## Rejectade alternativ

### Alt A: Centraliserad modell-alias med runtime-resolution

Dvs använd `"sonnet-latest"` som alias, resolva till aktuell version vid runtime.

**Varför inte:**

- **Bryter reproducerbarhet.** Ett CV genererat 2026-04-18 kan vara Sonnet 4.6. Samma prompt körd 2026-06-01 kan vara Sonnet 4.7 — annorlunda output.
- **Bryter eval-integritet.** ai-prompt-engineers eval-rubrik mäter prompt-kvalitet mot specifik modell. Om modellen ändras tyst, blir evals meningslösa.
- **GDPR-audit-trail försämras.** "Vilken modell processade denna användares data i maj?" blir svår att svara på.
- **Regression-risk.** Ny Sonnet-version kan bete sig annorlunda på specifika JobbPilot-prompts. Utan medveten eval märker vi det inte förrän användare klagar.

---

## Accepterad lösning: explicit versioner + centraliserad uppgraderings-skill

### Princip

**Explicit modellversion i alla filer (nuvarande policy) — men centraliserad uppgraderingsprocess via skill.**

Detta behåller:
- ✓ Reproducerbarhet
- ✓ Eval-integritet
- ✓ Audit-trail för GDPR
- ✓ Kontrollerad uppgraderingsprocess

Eliminerar:
- ✗ Risken att glömma platser vid uppgradering
- ✗ Manuellt arbete vid uppgradering
- ✗ Otestade byten till nya modeller

---

## Implementationsplan (för Fas 1)

### Steg 1: Fyll i ADR 0002

ADR 0002 är currently en stub. Innehåll som ska in:

- **Kontext:** Claude Bedrock stödjer både explicit versioner och alias ("claude-3-5-sonnet"). Vi måste välja.
- **Beslut:** Explicit versioner överallt (inklusive inference profile-suffix där tillämpligt).
- **Alternativ övervägda:** Alias-baserad runtime-resolution (avvisad).
- **Motivering:** GDPR-audit, eval-integritet, regression-prevention.
- **Konsekvenser positiva:** Reproducerbarhet, kontroll.
- **Konsekvenser negativa:** Manuell uppgraderingsprocess — adresseras via skill (se steg 3).

### Steg 2: Skapa `docs/models/CURRENT-MODELS.md`

Single source of truth för *aktuella* modeller (inte hårdkodade IDs — dokumentation).

Exempel:

```markdown
# JobbPilot — aktuella Claude-modeller

> **Senast uppdaterad:** 2026-04-18
> **Uppdateringsprocess:** kör /audit-models för att hitta nya versioner,
> sedan /evaluate-new-model för eval, sedan /update-model-version för
> konsistent uppgradering.

## Aktiva modeller (produktion)

| Roll | Modell-ID | Bedrock EU profile | Använd i |
|------|-----------|---------------------|----------|
| Premium reasoning | claude-opus-4-7 | eu.anthropic.claude-opus-4-7 | 7 agents, 2 prompts |
| Balanced | claude-sonnet-4-6 | eu.anthropic.claude-sonnet-4-6 | 4 agents, 1 prompt |
| Fast/cheap | claude-haiku-4-5-20251001-v1:0 | eu.anthropic.claude-haiku-4-5-20251001-v1:0 | 0 agents, 2 prompts |

## Kandidater för nästa uppgradering

| Roll | Föreslagen | Eval-status | Blockers |
|------|-----------|-------------|----------|
| (tom) | | | |
```

### Steg 3: Skapa tre skills i Fas 1

**Skill A: `/audit-models`** (read-only, hittar nya modeller)

- Läser `docs/models/CURRENT-MODELS.md`
- Kollar Anthropic + Bedrock-docs efter nya versioner
- Rapporterar: "Sonnet 4.7 finns nu tillgänglig. Föreslår eval innan uppgradering."
- Kör **INTE** uppgraderingen automatiskt

**Skill B: `/evaluate-new-model <model-id>`** (eval-baserad validering)

- Kör alla prompts i `prompts/*.prompt.md` mot ny modell med existerande fixtures i `prompts/evals/`
- Jämför scores mot nuvarande produktionsmodell
- Rapporterar: "Sonnet 4.7 scorar 0.87 vs 4.6:s 0.85 på CV-generering. Promote rekommenderad."
- Kör **INTE** uppgraderingen — endast rapport

**Skill C: `/update-model-version <old> <new>`** (write, konsistent uppgradering)

- Listar alla filer som refererar `<old>`:
  - `.claude/agents/*.md` (frontmatter `model:`)
  - `.claude/settings.json` (allowlists)
  - `prompts/*.prompt.md` (model + inference_profile)
  - `docs/models/CURRENT-MODELS.md`
  - BUILD.md §7 (om listat där)
- Visar diff för varje ändring
- Väntar på explicit Klas-godkännande per fil eller total
- Applicerar
- Föreslår att adr-keeper skapar ny ADR: "ADR XXXX — Uppgradering till <new>"
- Bumpar prompt-versioner där relevant (cv-generation-v3 → v4 med ny modell; v3 arkiveras)

### Steg 4: Integrera med CI (framtid, inte Fas 1)

På längre sikt: pre-push hook som verifierar att alla modell-referenser matchar `CURRENT-MODELS.md` (drift-prevention).

---

## Scope & tidsplan

**Ska göras:** Fas 1 (efter att session 3 är klar)

**Inte nu:** Session 3 är fokuserad på agent-setup. Att försöka lösa modell-versionshantering nu är scope-creep som försenar Fas 0-start.

**Förstahands-implementation:**
1. Fyll i ADR 0002 (snabb, kan göras när adr-keeper körs första gången)
2. Skapa `docs/models/CURRENT-MODELS.md` (snabb, ren dokumentation)
3. Skill A (`/audit-models`) — första iterationen, read-only
4. Senare: Skill B och C när first modell-uppgradering faktiskt närmar sig

---

## Referenser

- ADR 0002 stub: `docs/decisions/0002-explicit-model-versions.md`
- Inference profiles: `docs/research/bedrock-inference-profiles.md`
- Session 3 diskussion (Klas fråga efter STEG 5.10 om modell-centralisering)
- ai-prompt-engineers eval-system: `prompts/evals/*.eval.md` (kommer i Fas 1)
