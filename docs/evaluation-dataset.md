# Fictional Evaluation Dataset

This project includes a small fictional evaluation dataset for deterministic workflow checks.

The dataset is not clinical validation. It does not measure diagnostic accuracy, prescribing safety, medication appropriateness, triage quality, or real-world model performance.

Its purpose is narrower: confirm that the mock workflow behaves consistently for representative fictional scenarios.

## Dataset Location

The machine-readable dataset lives at:

```text
backend/ClinicalIntake.Api.Tests/TestData/evaluation-cases.json
```

It is used by `WorkflowEvaluationDatasetTests`.

## What The Dataset Checks

Each fictional case defines:

- intake text
- age
- optional medication entries
- expected review status
- expected mock confidence range
- expected risk flag labels
- expected medication signal labels
- expected medication documentation quality status

The test runs each case through the real workflow service:

1. Create intake
2. Generate deterministic mock summary
3. Add medication context if present
4. Analyse medication context
5. Compare outputs with expected workflow results

## Current Cases

| Case | Purpose | Expected outcome |
| --- | --- | --- |
| `routine-school-sleep-context` | Routine school/sleep/attention context | Stays `New`, creates low-severity functional impact flag |
| `high-risk-safeguarding-language` | Self-harm/suicidal language | Routes to `NeedsReview` |
| `low-confidence-brief-note` | Very short unclear note | Routes to `NeedsReview` because confidence is below threshold |
| `otc-nsaid-with-asthma-context` | OTC NSAID plus asthma context | Creates high-severity medication review signal and routes to `NeedsReview` |
| `incomplete-current-medication-history` | Missing dose/frequency for current medicine | Creates incomplete medication-history signal and incomplete documentation quality status |
| `possible-adverse-reaction-history` | Reaction language in medication notes | Creates high-severity adverse reaction history signal and routes to `NeedsReview` |

## Safety Boundaries

The dataset uses fictional data only.

It does not attempt to prove that the application is clinically safe. It only helps prevent accidental regressions in the deterministic workflow rules.

The expected outputs are workflow expectations, not clinical truth:

- `NeedsReview` means route to qualified human review, not autonomous triage.
- Risk flags are keyword-based prompts, not diagnoses.
- Medication signals are reviewer questions, not drug-interaction alerts.
- Documentation quality is a completeness check, not a clinical risk score.

## Why This Is Useful

Healthtech AI workflows need evaluation habits even at MVP stage. A small deterministic dataset makes the system easier to inspect because reviewers can see:

- what scenarios the workflow is expected to handle
- why a case routes to review
- which signals are produced
- which limitations are deliberately out of scope

Future versions could expand this into a larger synthetic scenario set, but the project should continue to avoid real patient data.
