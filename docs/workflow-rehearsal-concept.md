# Workflow Rehearsal Concept

## Position In The Product

Workflow Rehearsal is a secondary assurance surface for the Clinical Intake AI Workflow project. The primary product remains evidence-linked intake review. Rehearsal exists to make workflow controls inspectable before any discussion of real deployment.

It is not a clinical simulation, diagnostic trainer, patient digital twin, triage game, treatment simulator or medical education assessment.

## Evaluation Contract

Every rehearsal run should carry an explicit contract:

```json
{
  "evaluationScope": "operational controls",
  "clinicalMeaning": false,
  "clinicalValidityAssessed": false,
  "dataMode": "fictional",
  "replayMode": "deterministic"
}
```

The UI displays the same boundary in plain language. A completed run shows that a configured state transition occurred. It does not show that a clinical decision was correct.

## Current Scenarios

### Missing Provenance Gate

Checks whether a fictional output remains held when a required source label is absent, then becomes available for human review after provenance is completed.

### Low-Confidence Handoff

Checks whether deterministic output below the configured `0.75` confidence threshold moves into `NeedsReview`, keeps the original text visible and appends the reviewer action to the audit trail.

### Medication Documentation Gap

Checks whether missing medication-history fields create a clarification question without producing prescribing advice, interaction checking, medication reconciliation or a clinical risk score.

## Deterministic Event Model

A scenario is an ordered list of events. Each event contains:

- actor;
- action;
- resulting workflow state;
- inspectable evidence;
- stable scenario and step identifier.

The browser demo derives a stable replay identifier from the scenario and event index. Future backend support could store a schema version, deterministic seed, input fixture hash, event log hash and implementation version.

## Debrief Design

The debrief answers three narrow questions:

1. Did the expected operational gate or handoff occur?
2. Is the evidence for that state transition visible?
3. Was the clinical boundary preserved?

There is intentionally no clinical score, leaderboard, success probability, patient outcome estimate or claim of realism.

## Future Professionalisation

A later iteration could add:

- versioned JSON scenario fixtures;
- schema validation and replay checksums;
- expected/actual workflow-state diffing;
- accessibility and human-factors observation templates;
- exportable debrief evidence for software assurance review;
- fault-injection scenarios for provider timeout, malformed output and missing provenance;
- separate reviewer roles and release gates;
- synthetic-only scenario libraries reviewed by domain experts.

Before any scenario is described as clinically representative, it would require an explicit validation protocol, qualified reviewers, controlled source material and documented limitations. That work is outside the current repository scope.
