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

## Next Build Sequence

1. Multimodal Clinical Context Layer concept
   - Design a safe future layer for text intake, voice transcript text and document/OCR text.
   - Do not claim zero missed risk.
   - Do not interpret clinical images or make diagnoses.
   - Route extracted context into human-review prompts with evidence snippets.

2. Context Event model
   - Add a generic event model for intake context, such as `sourceType`, `sourceLabel`, `content`, `capturedAt`, `confidence` and `createdBy`.
   - Use it to preserve where each piece of context came from.

3. Evidence-linked review signals
   - Attach review signals to short source snippets.
   - Help reviewers understand why a workflow prompt was created.

4. Mock transcript ingestion
   - Add a text transcript endpoint as a safe stand-in for voice ingestion.
   - Keep real speech-to-text as a planned adapter only.

5. Mock document/OCR ingestion
   - Add fictional document text ingestion for referral notes or medication lists.
   - Do not process real patient documents.

6. Optional FHIR-style export examples
   - Add fictional JSON export examples for intake, medication context and audit events.
   - Do not connect to real healthcare systems.

7. Production deployment design
   - Document production concerns separately from local Docker Compose.
   - Include migrations, secrets, authentication, monitoring and environment-specific configuration.

8. Optional AI adapters
   - Add environment-variable-based adapters later.
   - Keep mock mode as the default so the project runs without API keys.
