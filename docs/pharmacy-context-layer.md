# Pharmacy Context Layer

The Pharmacy Context Layer extends the intake workflow with medication-history capture and pharmacist-review signals.

It is designed as workflow support only. It does not diagnose, prescribe, recommend treatment, infer causality, or replace pharmacist/clinician review.

This feature does not perform medication reconciliation, drug interaction checking, clinical decision support, prescribing advice, or diagnosis. It only captures medication context and creates review prompts for qualified human review.

## Why Medication Context Matters

Medication history can be incomplete during intake. Patients and families may mention medicines casually, describe over-the-counter use without dose or duration, or refer to medicines kept in the household. These details can be important for documentation quality and follow-up questions.

This layer helps capture those details in a structured way so they are visible to a human reviewer.

## Why OTC Medication History Can Be Relevant

Over-the-counter medicines can be clinically relevant because they may be used without formal prescribing records, may be taken intermittently, and may be missed in routine documentation.

This project includes simple NSAID-related review signals because medicines such as ibuprofen, Nurofen, naproxen, and NSAIDs are common examples where dose, duration, reason for use, and surrounding history can matter.

The system does not decide whether use is appropriate or unsafe. It only creates review questions.

## What The System Does

The system allows a care team user to record medication context:

- medication name
- category: `Current`, `Recent`, `Past`, `OTC`, or `FamilyHousehold`
- dose
- route
- frequency
- start and stop dates
- reason for use
- source: `PatientReported`, `FamilyReported`, `ClinicianReported`, or `Unknown`
- prescriber
- notes

It can then generate deterministic medication review signals, such as:

- OTC NSAID context
- Medication safety review signal
- Incomplete medication history
- Polypharmacy context
- Household medication context
- Possible adverse reaction history

Every signal is phrased as a review signal or reviewer question.

## What The System Does Not Do

The system does not:

- Diagnose
- Prescribe
- Recommend treatment
- Autonomously triage
- Perform medication reconciliation
- Perform drug interaction checking
- Provide clinical decision support
- Provide prescribing advice
- Claim that a medication caused a disease or symptom
- Use real patient data
- Implement a real drug-interaction engine
- Replace pharmacist or clinician judgment

## Human-In-The-Loop Design

Medication signals are routed into the same human review model as the rest of the intake workflow.

High-severity medication signals set the intake status to `NeedsReview`. This is not autonomous clinical triage. It is a workflow routing signal that indicates a qualified clinician or pharmacist should review the context.

The UI keeps the original intake, medication timeline, medication signals, reviewer questions, and audit log visible together.

## Auditability

The audit log records:

- medication entry creation
- medication context analysis
- review status changes

This supports basic traceability for how medication context entered the workflow.

## Why This Avoids Overclaiming

The mock rules are deliberately simple and deterministic. They are not a clinical decision engine. They exist to show how medication-history context can be captured and routed safely without presenting software output as medical advice.
