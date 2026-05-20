# Healthcare AI Safety

This project treats AI as a workflow assistant. It does not present AI output as clinical advice.

## Safety Position

The safest first version of this idea is not a powerful model hidden behind an API key. It is a transparent workflow that:

- Preserves the original intake text
- Preserves additional fictional text context with source provenance
- Captures pasted mock transcript text as fictional text context only
- Produces a clearly labelled support summary
- Flags simple priority terms
- Captures medication-history context as review questions
- Shows medication-history documentation gaps without clinical scoring
- Shows confidence
- Routes uncertain or high-risk cases to humans
- Records important workflow actions

## Explicit Non-Goals

The app does not:

- Diagnose
- Prescribe
- Recommend treatment
- Decide clinical urgency autonomously
- Replace a qualified clinician or care team member
- Use real patient data
- Process real audio, perform speech-to-text, or identify speakers

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
