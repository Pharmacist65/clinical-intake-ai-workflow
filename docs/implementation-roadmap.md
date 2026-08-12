# Implementation Roadmap

This roadmap keeps the project progression deliberate. The goal is to make the application more useful and credible without turning a small MVP into an overbuilt clinical system.

## Completed Polish

- GitHub-ready README, screenshots and repository metadata
- GitHub Actions CI for backend tests and frontend build
- Pharmacy Context Layer with medication entries, review signals and medication timeline
- Safety documentation for clinical AI and pharmacy-context boundaries
- Swagger/OpenAPI for local API inspection
- Fictional demo seed data for a non-empty first run
- Review status audit note support
- Backend unit tests and API integration tests
- Medication documentation quality score for captured medication-history completeness
- FHIR/HL7 integration concept document for future interoperability planning
- Local Docker Compose setup for backend, frontend and SQLite volume
- Fictional evaluation dataset for deterministic workflow routing, risk flag and medication signal checks
- Multimodal Clinical Context Layer concept document for future text-source provenance and evidence-linked review signals
- Context Event model, API endpoints and UI for fictional text-source provenance
- Evidence-linked risk flags and medication review signals with short source snippets
- Mock transcript ingestion endpoint and UI using pasted fictional transcript text
- Mock document/OCR text ingestion endpoint and UI using pasted fictional document text
- FHIR-style fictional export endpoint and UI preview for interoperability discussion
- Production deployment design document covering migrations, secrets, authentication, monitoring and safety governance
- Mock-first AI provider boundary with configuration for future provider selection
- Machine-readable system capability endpoint with integration coverage
- Browser-only fictional demo adapter and tested GitHub Pages publication workflow
- Browser-demo Vitest contract coverage for capability, queue and review lifecycle behaviour
- Evidence-spine Three.js workflow visualisation with reduced-motion support
- UK/US governance explorer backed by dated primary sources
- Deterministic workflow rehearsal for provenance, handoff and documentation controls
- Current UK/US clinical AI landscape and product-pattern review

## Next Build Sequence

1. Versioned workflow assurance fixtures
   - Move rehearsal scenarios into validated JSON fixtures.
   - Add schema versions, deterministic input hashes and expected/actual event-log checks.
   - Keep `clinicalMeaning=false` unless a separate validation programme is completed.

2. Failure-mode evaluation
   - Add fictional provider timeout, malformed output, missing provenance and stale-review scenarios.
   - Test fail-closed behaviour, operator messaging and audit evidence.

3. Human-factors review with fictional data
   - Define representative tasks and correction workflows.
   - Measure provenance retrieval, omission discovery and reviewer effort without clinical-performance claims.

4. Optional speech-to-text adapter concept
   - Keep the implemented transcript path text-only by default.
   - Document any future adapter as disabled unless explicitly configured.

5. Optional OCR/document extraction adapter concept
   - Keep the implemented document path pasted-text-only by default.
   - Document any future adapter as disabled unless explicitly configured.

6. Real interoperability adapter concept
   - Keep the implemented export fictional and local.
   - Document any future FHIR/HL7 adapter as a separate integration boundary.

7. Optional external AI adapter implementation
   - Add a concrete provider only behind `IAiSummaryService` and a versioned output contract.
   - Keep mock mode as the default and require explicit configuration.
   - Add redaction, data-flow, timeout, malformed-output and provider-observability controls first.

8. Production hardening implementation
   - Add authentication, migrations, monitoring and deployment infrastructure only after requirements are known.
