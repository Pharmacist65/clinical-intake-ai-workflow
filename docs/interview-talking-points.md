# Interview Talking Points

## How I Would Explain This In An Interview

I would describe this as a small human-in-the-loop clinical workflow demo. The goal is not to make AI clinical decisions, but to show how unstructured intake notes can be captured, summarised, risk-flagged, audited and routed to a qualified human reviewer. I kept the AI deterministic in the MVP so the behaviour is testable, inspectable and safe to run without patient data or API keys.

The main engineering point is that the workflow is explicit: validation happens before persistence, summary generation is behind an interface, review routing is based on confidence and high-risk flags, and audit logs record important state changes. That lets me discuss both product judgment and implementation trade-offs.

## Clinical Problem

Clinical teams often receive unstructured intake information through calls, messages or referral notes. Important details can be buried in free text, and teams need a consistent way to record the note, identify possible priority language and route the case for human review.

I built this project as a workflow support demo, not as a diagnostic tool.

## Product Workflow

The user creates a fictional intake note with patient alias, age, source and intake text. The system stores the note, generates a structured AI-style summary, surfaces risk flags and records an audit trail. Cases with low confidence or high-risk terms move into a review queue.

The core workflow is:

1. Create intake
2. Generate mock AI summary
3. Review risk flags and confidence
4. Route to review queue when needed
5. Mark reviewed after human inspection
6. Preserve audit history

## Technical Architecture

The backend is an ASP.NET Core Web API using EF Core and SQLite. The frontend is React with TypeScript and Vite.

The backend separates concerns:

- API endpoints handle HTTP requests and responses.
- `IntakeWorkflowService` owns workflow transitions, audit logs and status changes.
- `MockAiSummaryService` owns deterministic summary and risk flag generation.
- EF Core models represent intakes, summaries, risk flags and audit logs.

This keeps the project readable and testable.

I also added a FHIR/HL7 concept document to show how the internal models could later map to healthcare interoperability concepts such as `QuestionnaireResponse`, `MedicationStatement`, `Task`, `Provenance` and `AuditEvent`. It is intentionally documentation-only: the app does not connect to a real EHR, pharmacy system, FHIR server or HL7 message feed.

## AI Safety

The AI component is deliberately constrained. It does not make clinical decisions. It produces a structured summary, flags configured terms and adds a safety disclaimer.

I used deterministic rules first because it makes the project runnable without API keys, testable in CI and easier for interviewers to inspect.

## Human Review

Human review is central to the design:

- High-risk terms route the case to `NeedsReview`.
- Low confidence routes the case to `NeedsReview`.
- The original intake remains visible beside the summary.
- Review status updates are audited.
- The UI makes the review queue explicit.

## Trade-Offs

I chose a small architecture instead of a complex one. The first version has no authentication, no production deployment, no real LLM integration and no live FHIR/HL7 integration. That is intentional for a small MVP: the priority is to show clean workflow modelling, safe AI framing and understandable code.

SQLite and `EnsureCreated` keep setup simple. I added Docker Compose as a local development convenience, but not as production infrastructure. In production, I would use migrations, environment-specific configuration and managed database infrastructure.

## What I Would Improve Next

The next improvements I would discuss are:

- Add an optional OpenAI adapter behind an interface while keeping mock mode as the default.
- Add RAG over local clinical policy documents so generated suggestions are grounded in approved guidance.
- Add role-based access control for reviewers and administrators.
- Add production deployment documentation beyond the local Docker Compose setup.
- Add structured observability for summary generation, review queue volume and failure modes.
- Build a small evaluation dataset to test keyword rules and model outputs against expected workflow routing.
- Add fictional FHIR-style export examples for intake, medication context and audit events, while keeping identifiable patient data out of the demo.
