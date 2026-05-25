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

## Next Build Sequence

1. Optional external AI adapter implementation
   - Add concrete provider adapters later, behind `IAiSummaryService`.
   - Keep mock mode as the default so the project runs without API keys.
   - Require explicit configuration before any external provider is used.

2. Optional speech-to-text adapter concept
   - Keep the implemented transcript path text-only by default.
   - Document any future adapter as disabled unless explicitly configured.

3. Optional OCR/document extraction adapter concept
   - Keep the implemented document path pasted-text-only by default.
   - Document any future adapter as disabled unless explicitly configured.

4. Real interoperability adapter concept
   - Keep the implemented export fictional and local.
   - Document any future FHIR/HL7 adapter as a separate integration boundary.

5. Production hardening implementation
   - Add authentication, migrations, monitoring and deployment infrastructure only after requirements are known.
