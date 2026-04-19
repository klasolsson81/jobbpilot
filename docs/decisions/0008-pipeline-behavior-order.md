# ADR 0008 — Pipeline behavior order (Mediator.SourceGenerator)

**Datum:** 2026-04-19
**Status:** Accepted
**Kontext:** Fas 0 kod-scaffolding, session 6
**Beslutsfattare:** Klas Olsson
**Relaterad:** ADR 0001, CLAUDE.md §2.3

## Kontext

JobbPilot använder Mediator.SourceGenerator för CQRS (ADR 0001). Mediator-pipeline-behaviors löper i en fast ordning runt varje handler-exekvering. Ordningen påverkar:

- Vilka behaviors som ser misslyckade requests
- Om authorization körs på ogiltig data (waste + säkerhetsrisk)
- Om databastransaktioner öppnas på unauthorized requests
- Hur komplett correlation och timing-logging blir

CLAUDE.md §2.3 specificerar ordningen men motiverar den inte. Inför Fas 1-implementation behöver ordningen vara explicit beslutsad och motiverad så att framtida behavior-tillägg görs rätt.

## Beslut

Pipeline-behaviors registreras yttre-till-inre i ordningen:

1. **Logging** (ytterst)
2. **Validation**
3. **Authorization**
4. **UnitOfWork** (innerst, närmast handler)

Varje anrop som når pipeline loggas, oavsett om det misslyckas i validation eller authorization. Logging wrappar allt.

## Konsekvenser

**Positivt:**

- Logging fångar alla requests inkl. misslyckade — correlation ID och timing syns i Seq för varje inkommande command/query, inte bara lyckade
- Validation kortsluter innan authorization-overhead — ingen pointless RBAC-utvärdering på malformad data
- Authorization kräver att requesten är valid för korrekt ResourceId-utvärdering — t.ex. "Är detta din `ApplicationId`?" fungerar bara om `ApplicationId` är parsad och giltig
- UnitOfWork (transaction-scope) öppnas bara kring faktisk handler-exekvering — ingen öppen databastransaktion på avvisade requests

**Negativt:**

- Authorization kan inte "kika" på raw ovaliderad data — om ett edge-case kräver det måste det hanteras i handler istället
- Ordningen är implicit kodad i DI-registreringen; om den bryts av misstag märks det i tester, inte compile-time

**Mitigering:**

- Architecture tests verifierar att DI-registreringsordningen i `Api/Program.cs` och `Worker/Program.cs` matchar denna spec
- Ändring av ordning är breaking change → kräver ny ADR (detta är den enda mekanismen för att formellt godkänna en orderändring)

## Alternativ övervägda

**Alt 1 — Validation → Authorization → Logging → UoW:** Förlorar loggning av misslyckade requests (de kortsluts av Validation/Auth innan Logging). Omöjliggör komplett audit trail i Seq.

**Alt 2 — Logging → Authorization → Validation → UoW:** Authorization på ovaliderad data. Om `ApplicationId` är en tom GUID kan auth-querien ge fel svar. Säkerhetsrisk.

**Alt 3 — UoW ytterst:** Öppnar databastransaktion på varje request, även de som avvisas av Validation i nästa steg. Onödigt resursspill och potentiella lock-problem.

**Alt 4 — Auth ytterst:** Kör RBAC innan validation. Onödig overhead när requesten ändå är ogiltig. Authorization för attribute-baserade regler (ResourceId) kräver att data är valid.

## Implementationsstatus

**Beslutsdatum:** 2026-04-19 (Fas 0 kod-scaffolding session 6)

**Ej implementerat än:** Pipeline-behaviors skrivs i Fas 1. Denna ADR dokumenterar beslutet innan implementation så att rätt ordning kodas från start.

**Nästa steg:** Vid Fas 1 — registrera de fyra behaviors i
`Api/Program.cs` och `Worker/Program.cs` i den ordning denna ADR
specificerar. Konkret Mediator.SourceGenerator-registreringssyntax
verifieras vid Fas 1-implementation (martinothamar/Mediator API
skiljer sig från MediatR och bekräftas mot v3.0.2 README innan kod
skrivs).
