# Contributing

Thank you for taking the time to inspect or improve this project.

## Scope

This repository is a fictional-data reference implementation for evidence-linked intake workflow review. Contributions must preserve its explicit boundaries:

- no real patient data;
- no diagnosis or treatment recommendation;
- no prescribing;
- no autonomous triage;
- no claim of clinical validation or regulatory compliance;
- no live EHR, NHS, pharmacy or external AI integration unless separately designed, reviewed and disabled by default.

Medication outputs must remain documentation prompts or reviewer questions. They must not become drug-interaction alerts, medication reconciliation, clinical decision support or prescribing advice.

## Local Checks

Backend:

```bash
dotnet test backend/ClinicalIntake.Api.Tests --configuration Release
```

Frontend:

```bash
cd frontend
npm ci
npm test
npm run build
npm run build:demo
npm audit
```

## Pull Requests

Keep changes narrow and explain:

- the workflow problem being addressed;
- the effect on safety and human review boundaries;
- the fictional test data or scenario used;
- the tests and visual checks performed;
- any new external data flow, dependency or runtime capability.

Do not commit secrets, local databases, build output, dependency folders, local handoff notes or real clinical material. Screenshots and fixtures must use clearly fictional content.

Changes to provider, integration, governance or clinical-adjacent behaviour should update the capability contract, safety documentation and relevant tests in the same pull request.
