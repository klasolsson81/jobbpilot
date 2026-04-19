# ADR 0007 — Branch protection för `main` i Fas 0

**Status:** Accepted
**Datum:** 2026-04-18
**Kontext:** STEG 8.3 (GitHub-integration)
**Beslutsfattare:** Klas Olsson
**Relaterad:** SESSION-2-PLAN §11.1, ADR 0004 (GitHub Flow)

## Kontext

Branch protection för `main` ska täcka två distinkta faser:

- **Fas 0 (nu):** solo-dev, ingen CI-infrastruktur, Claude Code-hooks + Husky
  är lokala kvalitetsgaten. GitHub-sidan har inga workflows att gate:a på.
- **Fas 0/1 med CI:** CI-workflows introduceras (planerat i senare STEG).
  Då finns `ci-build`, `ci-test`, `code-reviewer`, `security-auditor` som
  riktiga status checks. Required review blir meningsfullt när fler dev:s
  tillkommer eller när Klas använder self-review via PR-flow.

Att aktivera full §11.1 nu skulle **blockera alla merges** — required status
checks som inte existerar failar alltid. Samtidigt vill vi ha skydd mot de
mest destruktiva operationerna (force push, branch-radering) direkt.

## Repo-visibility-beslut

Branch protection på GitHub Free-plan kräver **publikt repo**. Initialt (session 2)
beslutades "privat tills klass-launch" men när STEG 8.3 kördes visade sig
classic branch protection returnera HTTP 403 på privat free-tier. Samma gäller
för rulesets.

Alternativ utvärderade:
- **A — Gör repot publikt:** Gratis protection, fler GitHub Actions-minuter
  (2000/mån vs 500/mån privat), Dependabot security alerts. Kod synlig för
  världen.
- **B — GitHub Pro ($4/mån eller gratis via Student Developer Pack):** Privat
  + full protection. Kräver Student Pack-ansökan eller betalning.
- **C — Acceptera oskyddat main:** Lokala hooks + self-discipline som enda skydd.

**Valt: A (publikt repo).** Motivering: gitleaks-hooken i pre-push är redan
aktiv mot secrets. Kod är inte affärskritisk att dölja i Fas 0 — värdet
ligger i upplevelse och data, inte algoritmer. Publikt ger även bättre
GitHub-Actions-quota för senare CI-arbete och fungerar som transparens i ett
akademiskt sammanhang (NBI/Handelsakademin).

Beslutet kan revideras vid class-launch om kommersiella överväganden ändras.

## Beslut

Implementera **B-nivå branch protection** för `main` i Fas 0:

**Aktivt:**
- `allow_force_pushes: false` — skyddar main-historiken från oavsiktlig rewrite
- `allow_deletions: false` — förhindrar radering av default-branch
- `required_linear_history: false` — squash-merge är konvention (ADR 0004) men
  inte enforced på protection-nivå

**Ej aktivt (Fas 0):**
- `required_status_checks: null` — CI existerar inte än
- `required_pull_request_reviews: null` — solo-dev, ingen att review:a från
- `required_conversation_resolution: false` — inga PR-konversationer i solo-flöde
- `enforce_admins: false` — Klas är solo-admin och ska kunna pusha direkt vid
  behov. `allow_force_pushes` och `allow_deletions` är ändå hårda gates även
  utan admin-enforcement.

## Discussions

Aktiverat i samband med STEG 8.3. Länkas från `.github/ISSUE_TEMPLATE/config.yml`
som fallback för frågor som inte är bug/feature.

## Växtväg till C-nivå

**Trigger:** När CI-workflows (`ci-build`, `ci-test`, `code-reviewer`,
`security-auditor`) är gröna i minst 3 PRs under en vecka.

**Uppgradering:**

```bash
MSYS_NO_PATHCONV=1 gh api --method PUT \
  -H "Accept: application/vnd.github+json" \
  --input - \
  /repos/klasolsson81/jobbpilot/branches/main/protection <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["ci-build", "ci-test", "code-reviewer", "security-auditor"]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1
  },
  "restrictions": null,
  "required_linear_history": false,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "required_conversation_resolution": true
}
EOF
```

`gh api PUT` ersätter hela protection-objektet — inkludera alla nuvarande
fält plus de nya, annars tappas skydd.

## Konsekvenser

**Positivt:**
- Force-push och branch-deletion är stoppade omedelbart
- Inga falska gates som blockerar legitima commits i Fas 0
- Publikt repo ger bättre GitHub Actions-quota (2000 min/mån gratis)
- Växtvägen är explicit och dokumenterad

**Negativt:**
- Ingen skydd mot att pusha icke-testade commits (Husky pre-push är lokal gate,
  fungerar bara om Klas använder Git Bash eller Claude Code-integration som
  triggar Husky)
- Ingen PR-obligation — Klas kan pusha direkt till main. Konvention är att
  följa GitHub Flow (feature-branches + PR) men det är inte enforced.
- Repot är publikt. Alla commits och filer kan ses av vem som helst.

**Mitigering:**
- Husky pre-push scannar secrets via gitleaks — scaffold-agnostisk, aktiv
  sedan STEG 7.5
- Claude Code-hooks (ADR 0006): guard-bash.sh + guard-spec-files.sh
- Self-discipline för GitHub Flow tills PR-flödet är etablerat
- Gitleaks i pre-push fångar secrets innan de når publikt repo

## Windows-teknisk notering — `MSYS_NO_PATHCONV=1`

Git Bash på Windows (MSYS2) konverterar `/repos/...` i kommandoargument till
Windows-sökvägar, vilket bryter `gh api`-anrop. Prefixa `MSYS_NO_PATHCONV=1`
på alla `gh api`-kommandon med URL-path-argument:

```bash
MSYS_NO_PATHCONV=1 gh api /repos/klasolsson81/jobbpilot/branches/main/protection
```

Dessutom: `-f "field=null"` skickar strängen `"null"`, inte JSON `null`.
Använd `--input -` med en JSON-body för fält som ska vara `null`:

```bash
echo '{"required_status_checks":null,...}' | MSYS_NO_PATHCONV=1 gh api --method PUT --input - /repos/...
```

## Alternativ övervägda

**Alt 1 — Full §11.1 nu (C-nivå):** Avvisat. Required status checks som inte
existerar blockerar alla merges.

**Alt 2 — Ingen branch protection alls:** Avvisat. Force push + deletion är
för destruktiva för att lämna oskyddade även i solo-dev-fas.

**Alt 3 — Rulesets istället för classic branch protection:** Övervägt men
uppskjutet. Rulesets är modernare men classic är well-documented och räcker
för B-nivå. Migration kan ske vid C-uppgradering om det då ger värde.

**Alt 4 — GitHub Pro för privat + protection:** Övervägt. Student Developer
Pack ger Pro gratis för studenter, men valet blev A (publikt) för enklare
setup och bättre CI-quota. Kan omvärderas vid class-launch.

## Validering

Branch protection-settings verifieras via:

```bash
MSYS_NO_PATHCONV=1 gh api /repos/klasolsson81/jobbpilot/branches/main/protection \
  --jq '{force_pushes: .allow_force_pushes.enabled, deletions: .allow_deletions.enabled}'
```

Eller i GitHub UI: Settings → Branches → Branch protection rules.

**Testvalidering (efter commit):**

Försök `git push --force origin main`. Ska returnera:

```
remote: error: GH006: Protected branch update failed for refs/heads/main.
remote: error: Cannot force-push to this branch.
```

Om push:en lyckas ändå → protection är inte aktiv. Kör om PUT-kommandot ovan.
