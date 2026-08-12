# FHIR/HL7 Integration Concept

This document explains how the current workflow could relate to healthcare interoperability standards in a future version.

It is primarily a concept document. The current application also includes a small FHIR-style fictional export endpoint for local demonstration, but it does not implement validated FHIR, HL7, NHS, EHR, pharmacy-system, or hospital-system integration.

## Why Interoperability Matters

Clinical intake workflows rarely exist in isolation. In a real healthtech setting, intake information may come from referral forms, electronic health records, pharmacy systems, care coordination tools, call notes, or patient-facing forms.

Interoperability matters because teams need to preserve where information came from, avoid duplicate data entry, keep medication history traceable, and make review status visible without pretending that software has made a clinical decision.

For this MVP, the goal is smaller: model the internal workflow clearly so it could later be mapped to external standards at the system boundary.

## Design Position

The application should keep its internal domain model simple and explicit:

- `Intake` represents the workflow case.
- `MedicationEntry` captures medication-history context.
- `MedicationSignal` captures pharmacist/clinician review prompts.
- `MedicationDocumentationQuality` captures completeness of medication-history documentation.
- `AiSummary` captures deterministic workflow-support output.
- `RiskFlag` captures simple keyword-based review prompts.
- `ReviewStatus` captures workflow state.
- `AuditLog` captures traceability.

A future integration layer would translate between this internal model and external formats. The core workflow should not become dependent on a specific EHR vendor, message transport, or FHIR profile too early.

## Conceptual FHIR Mapping

The table below is intentionally approximate. It shows possible conceptual relationships, not a production-ready implementation.

| Current model or field | Current purpose | Possible FHIR concept | Notes |
| --- | --- | --- | --- |
| `Intake` | Internal workflow case for a fictional intake note | `QuestionnaireResponse`, `ServiceRequest`, `Encounter`, or internal case/task model | The right mapping depends on whether the source is a form, referral, encounter note, or operational queue item. |
| `patientAlias` / `age` | Fictional patient context for demo use | `Patient` | A real integration would use proper patient identifiers and demographic fields. This demo deliberately avoids real identifiers and does not store NHS numbers. |
| `intakeText` | Original free-text intake note | `QuestionnaireResponse.item`, `DocumentReference`, or `Communication` | Original text should be preserved with provenance rather than overwritten by AI output. |
| `source` | Where the intake came from | `Provenance`, `DocumentReference.type`, or local source metadata | Source context matters for trust, auditability, and review. |
| `createdBy` / `createdAt` | Who created the workflow item and when | `Provenance` | A real system would need authenticated user identity rather than demo strings. |
| `AiSummary` | Deterministic workflow-support summary | Internal generated note, `DocumentReference`, or `Composition` | This should be clearly labelled as generated workflow support, not a clinician-authored diagnosis or treatment plan. |
| `RiskFlag` | Keyword-based review prompt | Internal flag, `Observation`, or `Task` reason | In this app, risk flags are workflow prompts only. They should not be exported as diagnoses. |
| `ReviewStatus` | `New`, `NeedsReview`, or `Reviewed` workflow state | `Task.status` or local workflow status | This represents operational state, not autonomous triage. |
| `MedicationEntry` | Captured medication-history context | `MedicationStatement` | Patient/family-reported medication context should preserve source and uncertainty. |
| `MedicationSignal` | Medication-related reviewer question | Internal flag, `Task`, or review note | These are not drug-interaction alerts or clinical decision support recommendations. |
| `MedicationDocumentationQuality` | Completeness score for medication-history fields | Internal quality indicator | This should usually remain internal because it is a documentation quality signal, not a clinical observation. |
| `AuditLog` | Workflow action trace | `AuditEvent` or `Provenance` | A real system would need richer audit metadata, authentication context, and access-event tracking. |

## HL7 v2 Concept

HL7 v2 is often used for event-based messaging between healthcare systems. This project does not implement HL7 v2 messages, but a future integration could conceptually involve:

- patient administration or encounter events feeding intake context
- document or referral messages contributing source notes
- observation-style messages contributing structured context
- outbound workflow notifications to another queue or case-management system

For this MVP, implementing HL7 v2 would add transport complexity without improving the core demonstration. A design document is enough at this stage.

## Integration Boundary

A future implementation should use an adapter layer rather than mixing interoperability code directly into the workflow service.

Possible structure:

```text
External healthcare system
        |
FHIR/HL7 adapter
        |
Validation and provenance mapping
        |
Internal workflow models
        |
Human review UI and audit log
```

This keeps the project understandable and allows the internal workflow to remain stable while external integrations evolve.

## Implemented FHIR-Style Export

The MVP includes:

- `GET /api/intakes/{id}/fhir-style-export`

This endpoint maps one local intake into a fictional JSON bundle with FHIR-like resource names:

- `Patient` for fictional alias and age
- `QuestionnaireResponse` for original intake text and source details
- `Task` for local human review status
- `MedicationStatement` for captured medication-history entries
- `Provenance` for captured context sources
- `AuditEvent` for workflow audit log entries

The export is intentionally labelled as fictional. It is not a validated FHIR bundle, does not use organisation-specific profiles, does not include real patient identifiers, and does not connect to an external system.

## Safety Boundaries

A future FHIR or HL7 adapter would not change the safety position of the app.

The system would still not:

- diagnose
- prescribe
- recommend treatment
- autonomously triage
- perform medication reconciliation
- perform drug-interaction checking
- provide clinical decision support
- infer causality between a medicine and a condition

Medication outputs would remain review prompts for qualified human review.

## Data Protection Boundaries

This public demo uses fictional data only.

A real integration would need:

- a data protection impact assessment
- role-based access control
- secure authentication and authorization
- encryption in transit and at rest
- environment-specific secrets management
- full audit event design
- retention and deletion policies
- clinical governance review
- organisation-specific FHIR profiles or interface specifications

None of those are implemented in this MVP.

## What Could Be Built Later

A sensible next implementation step would be a real integration boundary design, not a live EHR connection.

For example:

- define where a future FHIR adapter would live outside the core workflow service
- document authentication, authorization, environment configuration and audit requirements
- keep any integration examples synthetic and clearly labelled as non-production
- avoid adding live EHR connectivity until governance and data protection requirements are understood

That would demonstrate interoperability thinking while keeping the project inside documented boundaries, readable, and runnable without external systems.
