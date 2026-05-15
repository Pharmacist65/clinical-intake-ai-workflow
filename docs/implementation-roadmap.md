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

## Next Build Sequence

1. Medication documentation quality score
   - Add a non-clinical completeness score for medication-history documentation.
   - Score only documentation quality, such as missing dose, frequency, source or timing.
   - Avoid clinical risk scoring or treatment recommendations.

2. FHIR/HL7 integration concept
   - Add a design document mapping current models to FHIR concepts such as `Patient`, `QuestionnaireResponse`, `MedicationStatement` and `Observation`.
   - Keep this as architecture documentation first, not a live integration.

3. Docker setup
   - Add a simple local development Docker Compose setup.
   - Keep SQLite local and avoid deployment complexity.

4. Evaluation dataset
   - Add a small fictional dataset for deterministic summary and routing checks.
   - Include expected review statuses and expected signal labels.

5. Multimodal Clinical Context Layer concept
   - Design a safe future layer for text intake, voice transcript text and document/OCR text.
   - Do not claim zero missed risk.
   - Do not interpret clinical images or make diagnoses.
   - Route extracted context into human-review prompts with evidence snippets.

6. Context Event model
   - Add a generic event model for intake context, such as `sourceType`, `sourceLabel`, `content`, `capturedAt`, `confidence` and `createdBy`.
   - Use it to preserve where each piece of context came from.

7. Evidence-linked review signals
   - Attach review signals to short source snippets.
   - Help reviewers understand why a workflow prompt was created.

8. Mock transcript ingestion
   - Add a text transcript endpoint as a safe stand-in for voice ingestion.
   - Keep real speech-to-text as a planned adapter only.

9. Mock document/OCR ingestion
   - Add fictional document text ingestion for referral notes or medication lists.
   - Do not process real patient documents.

10. Optional AI adapters
    - Add environment-variable-based adapters later.
    - Keep mock mode as the default so the project runs without API keys.
