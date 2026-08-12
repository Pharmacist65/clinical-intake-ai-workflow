# Healthcare AI Safety

This project treats AI as a workflow assistant. It does not present AI output as clinical advice.

## Safety Position

The safest first version of this idea is not a powerful model hidden behind an API key. It is a transparent workflow that:

- Preserves the original intake text
- Preserves additional fictional text context with source provenance
- Captures pasted mock transcript text as fictional text context only
- Captures pasted mock document/OCR text as fictional text context only
- Provides a FHIR-style fictional export for interoperability discussion only
- Produces a clearly labelled support summary
- Flags simple priority terms
- Captures medication-history context as review questions
- Shows medication-history documentation gaps without clinical scoring
- Shows confidence
- Routes uncertain or high-risk cases to humans
- Records important workflow actions
- Exposes disabled capabilities through a machine-readable runtime manifest
- Separates operational workflow rehearsal from any claim of clinical simulation or validation

## Explicit Non-Goals

The app does not:

- Diagnose
- Prescribe
- Recommend treatment
- Decide clinical urgency autonomously
- Replace a qualified clinician or care team member
- Use real patient data
- Process real audio, perform speech-to-text, or identify speakers
- Process real images, perform OCR, or parse real clinical documents
- Implement live FHIR, HL7, NHS, EHR, or pharmacy-system integration
- Claim UK or US regulatory compliance, certification or clinical validation
- Use workflow rehearsal as evidence of diagnosis, triage, prescribing or patient-outcome performance

## Human-In-The-Loop Controls

Cases are routed to `NeedsReview` when:

- A configured high-risk keyword is detected
- The mock confidence score is below `0.75`
- A high-severity medication review signal is generated

The reviewer can inspect:

- Original intake text
- AI-style structured summary
- Risk flags and reasons
- Evidence snippets for deterministic review signals
- Confidence score
- Captured context sources and source labels
- Medication timeline and medication review questions
- Medication documentation quality issues
- Audit log

Only a human reviewer can mark a case `Reviewed`.

## Deterministic Mock Mode

The first version avoids real LLM calls deliberately.

Benefits:

- The project can be run and reviewed without API keys.
- Tests are deterministic.
- Safety behaviour is readable in source code.
- The demo focuses on workflow design and governance boundaries.

This also avoids treating AI output as if it were inherently clinically safe. Here, the model-like component is constrained and inspectable.

## AI Provider Boundary

Summary generation is wired through a mock-first provider boundary. The default configuration is:

- `AiSummary:Provider=Mock`
- `AiSummary:ExternalProvidersEnabled=false`

Only the deterministic mock provider is registered. Configuring another provider name fails unless a future adapter is explicitly implemented and registered. This keeps the default demo free of API keys, hidden model behaviour, real patient-data transmission, and unreviewed external AI dependencies.

## Capability Manifest

`GET /api/system/capabilities` exposes the current build mode, provider name, external-provider state and explicit booleans for real patient data, diagnosis, prescribing, autonomous triage and live integrations. The frontend reads this contract rather than relying only on marketing copy.

The manifest describes repository behaviour. It is not a regulatory determination, safety certification, compliance assessment or substitute for deployment controls.

## Static Showcase Safety

The GitHub Pages-compatible build uses only in-memory fictional records. It does not call the backend or an external model, and changes reset on reload. It is useful for public inspection but has no authentication, durable storage or production security controls. It must never be used as a real clinical service.

## Workflow Rehearsal Boundary

Workflow Rehearsal tests deterministic operational state transitions such as provenance gates and human-review handoffs. Every scenario is scoped with `clinicalMeaning=false` and `clinicalValidityAssessed=false`. A successful run demonstrates software behaviour only.

## Risk Flag Rules

Configured high-risk terms include:

- `self-harm`
- `self harm`
- `suicidal`
- `harm`
- `abuse`
- `safeguarding`

Urgency terms include:

- `urgent`
- `crisis`
- `severe`

These rules are intentionally simple. They are not a clinical risk model, and absence of a flag does not mean absence of risk.

Evidence snippets show the local source text that matched a deterministic rule. They are included to support human review and auditability. They are not clinical proof, diagnostic reasoning, or a complete safety assessment.

## Disclaimer

Every generated summary includes:

> AI output is for workflow support only and must be reviewed by a qualified clinician.

## Auditability

The audit log records:

- Intake creation
- Context event creation
- Mock transcript context creation
- Mock document/OCR context creation
- Summary generation
- Medication entry creation
- Medication context analysis
- Review status updates

This supports a basic accountability trail and makes workflow state changes visible during review.

## Validation And Error Handling

The API validates required fields, age range, known review statuses and text lengths before writing workflow data. Invalid requests return structured validation errors rather than relying on unhandled exceptions. Unexpected failures return a consistent server error shape.

This matters in healthtech because unclear errors can create unsafe operator assumptions, hidden data quality problems, or inconsistent audit trails.

## Production Safety Improvements

A real system would need:

- Clinical governance and safety case documentation
- Data protection review and DPIA
- Threat modelling
- Strong authentication and role-based access control
- Full audit event design
- Human factors testing
- Evaluation against representative scenarios
- Monitoring for model, data and workflow drift
- Incident reporting and rollback processes
- Integration review for FHIR, HL7 or local EHR interfaces

See [production-deployment-design.md](production-deployment-design.md) for the production deployment readiness checklist and operational boundaries.
