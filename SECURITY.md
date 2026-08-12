# Security Policy

## Project Status

This repository is a local reference implementation and browser-only fictional showcase. It is not a production clinical service and has no authentication, production hardening or approval for real patient data.

Only the current `main` branch is maintained. There are no supported production releases.

## Reporting A Vulnerability

Use the repository's private **Report a vulnerability** flow under the GitHub Security tab when it is available. Include:

- affected commit and component;
- reproduction steps using fictional data;
- expected and observed behaviour;
- potential confidentiality, integrity or availability impact;
- a suggested mitigation, if known.

Do not include patient information, credentials, access tokens, private keys, proprietary clinical records or other sensitive material. Do not test against real healthcare systems or data.

If private vulnerability reporting is unavailable, open a minimal public issue requesting a private maintainer contact channel. Do not publish exploit details or sensitive evidence in that issue.

## Security Boundaries

The current project:

- uses fictional local data only;
- registers only the deterministic mock summary provider;
- performs no external AI call in the default build;
- provides no live EHR, FHIR, HL7, NHS or pharmacy-system integration;
- is not HIPAA compliant, NHS deployment ready, clinically validated or regulatory cleared.

See [docs/healthcare-ai-safety.md](docs/healthcare-ai-safety.md) and [docs/production-deployment-design.md](docs/production-deployment-design.md) for the wider safety and production-readiness boundaries.
