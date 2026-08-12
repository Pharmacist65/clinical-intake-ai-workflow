import type { ClinicalIntakeApi } from "./apiContract";
import type {
  AiSummary,
  AuditLog,
  ContextEvent,
  ContextSourceType,
  CreateContextEventPayload,
  CreateDocumentContextPayload,
  CreateIntakePayload,
  CreateMedicationPayload,
  CreateTranscriptContextPayload,
  FhirStyleExport,
  IntakeDetail,
  IntakeListItem,
  MedicationDocumentationIssue,
  MedicationDocumentationQuality,
  MedicationEntry,
  MedicationSignal,
  ReviewQueueItem,
  ReviewStatus,
  RiskFlag,
  RiskSeverity,
  SystemCapabilities
} from "./types";

const demoCapabilities: SystemCapabilities = {
  applicationMode: "FictionalWorkflowDemo",
  aiProvider: "Mock",
  externalProvidersEnabled: false,
  realPatientDataPermitted: false,
  diagnosisEnabled: false,
  prescribingEnabled: false,
  autonomousTriageEnabled: false,
  liveIntegrationsEnabled: false,
  clinicalValidationCompleted: false,
  workflowRehearsalClinicalMeaning: false,
  jurisdictionLenses: ["UK governance review prompts", "US governance review prompts"],
  disclaimer:
    "Capability metadata describes this repository build. It is not regulatory clearance, clinical validation, or a compliance assessment."
};

const documentationDisclaimer =
  "Medication documentation quality reflects completeness of captured medication-history fields only. It is not a clinical risk score, diagnosis, prescribing recommendation, medication reconciliation, drug-interaction check, or clinical decision support.";

const seedIntakes: IntakeDetail[] = [
  {
    id: 101,
    patientAlias: "Fictional Patient A",
    age: 12,
    intakeText:
      "Family reports disrupted sleep, school support needs and reduced attention. A recent family call included language about feeling unsafe that requires qualified human review. Intermittent Nurofen use was reported, but dose and duration are unclear.",
    source: "family phone note",
    reviewStatus: "NeedsReview",
    createdAt: "2026-08-11T08:42:00Z",
    createdBy: "demo-coordinator",
    aiSummary: {
      id: 201,
      intakeId: 101,
      presentingConcerns: "Disrupted sleep, school support needs and reduced attention were documented.",
      relevantHistory: "A family call and intermittent OTC medication use were recorded as separate context sources.",
      possibleRisks: "Configured safety language was detected and must be interpreted by a qualified human reviewer.",
      recommendedNextStep: "Review the original source excerpts and complete the local human review workflow.",
      confidenceScore: 0.69,
      generatedAt: "2026-08-11T08:45:00Z",
      disclaimer: "AI output is for workflow support only and must be reviewed by a qualified clinician."
    },
    riskFlags: [
      {
        id: 301,
        intakeId: 101,
        label: "Safety language detected",
        severity: "High",
        reason: "A configured term was found in a fictional source and routed for human review.",
        evidenceSourceType: "TranscriptText",
        evidenceSourceLabel: "Mock family call transcript",
        evidenceSnippet: "...included language about feeling unsafe that requires qualified human review."
      },
      {
        id: 302,
        intakeId: 101,
        label: "Sleep concern documented",
        severity: "Low",
        reason: "A workflow keyword was surfaced for reviewer context.",
        evidenceSourceType: "IntakeText",
        evidenceSourceLabel: "Original intake note",
        evidenceSnippet: "Family reports disrupted sleep, school support needs and reduced attention."
      }
    ],
    contextEvents: [
      {
        id: 401,
        intakeId: 101,
        sourceType: "IntakeText",
        sourceLabel: "Original intake note",
        content:
          "Family reports disrupted sleep, school support needs and reduced attention. Intermittent Nurofen use was reported, but dose and duration are unclear.",
        capturedAt: "2026-08-11T08:42:00Z",
        createdBy: "demo-coordinator",
        confidenceScore: null,
        metadataJson: "{\"mode\":\"fictional-demo\"}",
        createdAt: "2026-08-11T08:42:00Z"
      },
      {
        id: 402,
        intakeId: 101,
        sourceType: "TranscriptText",
        sourceLabel: "Mock family call transcript",
        content:
          "This fictional transcript included language about feeling unsafe that requires qualified human review.",
        capturedAt: "2026-08-11T08:30:00Z",
        createdBy: "demo-coordinator",
        confidenceScore: 0.88,
        metadataJson: "{\"inputMode\":\"pasted-fictional-transcript\"}",
        createdAt: "2026-08-11T08:43:00Z"
      }
    ],
    medicationEntries: [
      {
        id: 501,
        intakeId: 101,
        medicationName: "Nurofen",
        normalizedName: "nurofen",
        category: "OTC",
        dose: null,
        route: "oral",
        frequency: null,
        startedAt: null,
        stoppedAt: null,
        reasonForUse: "Pain relief reported by family",
        source: "FamilyReported",
        prescribedBy: null,
        notes: "Dose and duration unclear in this fictional example.",
        createdAt: "2026-08-11T08:47:00Z"
      }
    ],
    medicationSignals: [
      {
        id: 601,
        intakeId: 101,
        medicationEntryId: 501,
        label: "Medication documentation gap",
        severity: "Medium",
        rationale: "Dose and frequency were not captured for the reported OTC medicine.",
        reviewerQuestion: "Can the recorded dose, frequency and timing be clarified by an authorised reviewer?",
        evidenceSourceType: "MedicationHistory",
        evidenceSourceLabel: "Nurofen medication entry",
        evidenceSnippet: "Dose and duration unclear in this fictional example.",
        createdAt: "2026-08-11T08:48:00Z"
      }
    ],
    medicationDocumentationQuality: {
      score: 43,
      status: "Incomplete",
      summary: "The fictional medication history has documentation gaps that should be clarified by a human reviewer.",
      issues: [
        { medicationEntryId: 501, medicationName: "Nurofen", field: "dose", reason: "Dose is missing." },
        { medicationEntryId: 501, medicationName: "Nurofen", field: "frequency", reason: "Frequency is missing." },
        { medicationEntryId: 501, medicationName: "Nurofen", field: "timing", reason: "Start timing is missing." }
      ],
      disclaimer: documentationDisclaimer
    },
    auditLogs: [
      audit(701, 101, "IntakeCreated", "demo-coordinator", "2026-08-11T08:42:00Z", "Fictional intake created."),
      audit(702, 101, "ContextEventAdded", "demo-coordinator", "2026-08-11T08:43:00Z", "Mock transcript text added with provenance."),
      audit(703, 101, "SummaryGenerated", "Mock", "2026-08-11T08:45:00Z", "Deterministic summary generated; human review retained."),
      audit(704, 101, "MedicationContextAnalysed", "demo-coordinator", "2026-08-11T08:48:00Z", "Documentation prompts generated; no clinical decision was made.")
    ]
  },
  {
    id: 102,
    patientAlias: "Fictional Patient B",
    age: 72,
    intakeText:
      "Patient reports sleep disruption, a recent falls concern and several current medicines. The note is intended to rehearse medication-history completeness before clinician review.",
    source: "clinic intake form",
    reviewStatus: "NeedsReview",
    createdAt: "2026-08-10T13:15:00Z",
    createdBy: "demo-coordinator",
    aiSummary: {
      id: 202,
      intakeId: 102,
      presentingConcerns: "Sleep disruption, a recent falls concern and multiple current medicines were documented.",
      relevantHistory: "The medication list contains five fictional entries with one incomplete record.",
      possibleRisks: "A configured falls term and medication documentation gaps require human review.",
      recommendedNextStep: "Inspect source evidence and clarify the incomplete medication fields in the review workflow.",
      confidenceScore: 0.78,
      generatedAt: "2026-08-10T13:18:00Z",
      disclaimer: "AI output is for workflow support only and must be reviewed by a qualified clinician."
    },
    riskFlags: [
      {
        id: 303,
        intakeId: 102,
        label: "Falls language detected",
        severity: "Medium",
        reason: "A configured workflow term was surfaced for qualified human review.",
        evidenceSourceType: "IntakeText",
        evidenceSourceLabel: "Original intake note",
        evidenceSnippet: "Patient reports sleep disruption, a recent falls concern and several current medicines."
      }
    ],
    contextEvents: [
      {
        id: 403,
        intakeId: 102,
        sourceType: "IntakeText",
        sourceLabel: "Original intake note",
        content:
          "Patient reports sleep disruption, a recent falls concern and several current medicines. The note is intended to rehearse documentation completeness.",
        capturedAt: "2026-08-10T13:15:00Z",
        createdBy: "demo-coordinator",
        confidenceScore: null,
        metadataJson: "{\"mode\":\"fictional-demo\"}",
        createdAt: "2026-08-10T13:15:00Z"
      }
    ],
    medicationEntries: [
      medication(502, 102, "Amlodipine", "5 mg", "once daily", "PatientReported", "2026-08-10T13:20:00Z"),
      medication(503, 102, "Atorvastatin", "20 mg", "once daily", "PatientReported", "2026-08-10T13:21:00Z"),
      medication(504, 102, "Lansoprazole", "15 mg", "once daily", "PatientReported", "2026-08-10T13:22:00Z"),
      medication(505, 102, "Metformin", "500 mg", "twice daily", "PatientReported", "2026-08-10T13:23:00Z"),
      medication(506, 102, "Vitamin D", null, null, "Unknown", "2026-08-10T13:24:00Z")
    ],
    medicationSignals: [
      {
        id: 602,
        intakeId: 102,
        medicationEntryId: 506,
        label: "Incomplete medication history",
        severity: "Medium",
        rationale: "One medication entry is missing dose, frequency and a confirmed source.",
        reviewerQuestion: "Can the missing medication-history fields be verified from an approved source?",
        evidenceSourceType: "MedicationHistory",
        evidenceSourceLabel: "Vitamin D medication entry",
        evidenceSnippet: "Dose, frequency and information source are not documented.",
        createdAt: "2026-08-10T13:25:00Z"
      }
    ],
    medicationDocumentationQuality: {
      score: 86,
      status: "NeedsClarification",
      summary: "Most fictional medication fields are present; one entry needs clarification.",
      issues: [
        { medicationEntryId: 506, medicationName: "Vitamin D", field: "dose", reason: "Dose is missing." },
        { medicationEntryId: 506, medicationName: "Vitamin D", field: "frequency", reason: "Frequency is missing." },
        { medicationEntryId: 506, medicationName: "Vitamin D", field: "source", reason: "Source is unknown." }
      ],
      disclaimer: documentationDisclaimer
    },
    auditLogs: [
      audit(705, 102, "IntakeCreated", "demo-coordinator", "2026-08-10T13:15:00Z", "Fictional intake created."),
      audit(706, 102, "SummaryGenerated", "Mock", "2026-08-10T13:18:00Z", "Deterministic summary generated."),
      audit(707, 102, "MedicationContextAnalysed", "demo-coordinator", "2026-08-10T13:25:00Z", "Documentation prompts generated for human review.")
    ]
  },
  {
    id: 103,
    patientAlias: "Fictional Patient C",
    age: 9,
    intakeText:
      "Parent reports communication concerns and school support questions. No urgent language is documented in this fictional note.",
    source: "school referral note",
    reviewStatus: "Reviewed",
    createdAt: "2026-08-09T09:05:00Z",
    createdBy: "demo-coordinator",
    aiSummary: {
      id: 203,
      intakeId: 103,
      presentingConcerns: "Communication concerns and school support questions were documented.",
      relevantHistory: "Only the original fictional referral note is present.",
      possibleRisks: "No configured priority terms were detected; absence of a flag does not mean absence of clinical risk.",
      recommendedNextStep: "Continue the local human review workflow using the original note.",
      confidenceScore: 0.84,
      generatedAt: "2026-08-09T09:08:00Z",
      disclaimer: "AI output is for workflow support only and must be reviewed by a qualified clinician."
    },
    riskFlags: [],
    contextEvents: [
      {
        id: 404,
        intakeId: 103,
        sourceType: "IntakeText",
        sourceLabel: "Original intake note",
        content: "Parent reports communication concerns and school support questions.",
        capturedAt: "2026-08-09T09:05:00Z",
        createdBy: "demo-coordinator",
        confidenceScore: null,
        metadataJson: "{\"mode\":\"fictional-demo\"}",
        createdAt: "2026-08-09T09:05:00Z"
      }
    ],
    medicationEntries: [],
    medicationSignals: [],
    medicationDocumentationQuality: {
      score: null,
      status: "NotAssessed",
      summary: "No medication context has been recorded for this fictional intake.",
      issues: [],
      disclaimer: documentationDisclaimer
    },
    auditLogs: [
      audit(708, 103, "IntakeCreated", "demo-coordinator", "2026-08-09T09:05:00Z", "Fictional intake created."),
      audit(709, 103, "SummaryGenerated", "Mock", "2026-08-09T09:08:00Z", "Deterministic summary generated."),
      audit(710, 103, "ReviewStatusChanged", "demo-clinician", "2026-08-09T09:20:00Z", "Human review marked complete in the fictional workflow.")
    ]
  }
];

let intakes = clone(seedIntakes);
let nextIntakeId = 104;
let nextSummaryId = 204;
let nextRiskFlagId = 304;
let nextContextEventId = 405;
let nextMedicationId = 507;
let nextMedicationSignalId = 603;
let nextAuditId = 711;

export function createDemoApi(): ClinicalIntakeApi {
  return {
    getSystemCapabilities: async () => clone(demoCapabilities),
    listIntakes: async () => clone(toListItems(intakes)),
    getIntake: async (id) => clone(findIntake(id)),
    createIntake: async (payload) => clone(createIntake(payload)),
    generateSummary: async (id) => clone(generateSummary(findIntake(id))),
    addContextEvent: async (id, payload) => clone(addContextEvent(findIntake(id), payload)),
    addTranscriptContext: async (id, payload) => clone(addTranscriptContext(findIntake(id), payload)),
    addDocumentContext: async (id, payload) => clone(addDocumentContext(findIntake(id), payload)),
    listContextEvents: async (id) => clone(findIntake(id).contextEvents),
    addMedication: async (id, payload) => clone(addMedication(findIntake(id), payload)),
    listMedications: async (id) => clone(findIntake(id).medicationEntries),
    analyseMedicationContext: async (id) => clone(analyseMedicationContext(findIntake(id))),
    listMedicationSignals: async (id) => clone(findIntake(id).medicationSignals),
    getMedicationDocumentationQuality: async (id) => clone(findIntake(id).medicationDocumentationQuality),
    getFhirStyleExport: async (id) => clone(toFhirStyleExport(findIntake(id))),
    listReviewQueue: async () => clone(toReviewQueue(intakes)),
    updateReviewStatus: async (id, reviewStatus, reviewNote, actor = "demo-reviewer") =>
      clone(updateReviewStatus(findIntake(id), reviewStatus, reviewNote, actor))
  };
}

export function resetDemoApiState() {
  intakes = clone(seedIntakes);
  nextIntakeId = 104;
  nextSummaryId = 204;
  nextRiskFlagId = 304;
  nextContextEventId = 405;
  nextMedicationId = 507;
  nextMedicationSignalId = 603;
  nextAuditId = 711;
}

function createIntake(payload: CreateIntakePayload): IntakeDetail {
  const timestamp = new Date().toISOString();
  const intake: IntakeDetail = {
    id: nextIntakeId++,
    patientAlias: payload.patientAlias.trim(),
    age: payload.age,
    intakeText: payload.intakeText.trim(),
    source: payload.source.trim(),
    reviewStatus: "New",
    createdAt: timestamp,
    createdBy: payload.createdBy.trim(),
    aiSummary: null,
    riskFlags: [],
    contextEvents: [],
    medicationEntries: [],
    medicationSignals: [],
    medicationDocumentationQuality: emptyMedicationQuality(),
    auditLogs: [audit(nextAuditId++, nextIntakeId - 1, "IntakeCreated", payload.createdBy.trim(), timestamp, "Fictional intake created in the browser-only demo.")]
  };

  intakes = [intake, ...intakes];
  return intake;
}

function generateSummary(intake: IntakeDetail): IntakeDetail {
  const allSources = [intake.intakeText, ...intake.contextEvents.map((event) => event.content)].join(" ");
  const lower = allSources.toLowerCase();
  const timestamp = new Date().toISOString();
  const flags: RiskFlag[] = [];

  if (lower.includes("unsafe") || lower.includes("safeguarding") || lower.includes("suicid")) {
    flags.push(makeRiskFlag(intake, "Safety language detected", "High", "A configured safety term was found and routed for qualified human review.", "unsafe"));
  }

  if (lower.includes("fall")) {
    flags.push(makeRiskFlag(intake, "Falls language detected", "Medium", "A configured falls term was surfaced for qualified human review.", "fall"));
  }

  if (lower.includes("sleep")) {
    flags.push(makeRiskFlag(intake, "Sleep concern documented", "Low", "A workflow keyword was surfaced for reviewer context.", "sleep"));
  }

  const confidence = lower.includes("unclear") || lower.length < 120 ? 0.68 : 0.81;
  const summary: AiSummary = {
    id: intake.aiSummary?.id ?? nextSummaryId++,
    intakeId: intake.id,
    presentingConcerns: truncate(intake.intakeText, 180),
    relevantHistory: `${intake.contextEvents.length} additional provenance-tracked context source(s) are available for review.`,
    possibleRisks:
      flags.length === 0
        ? "No configured terms were detected; absence of a flag does not mean absence of clinical risk."
        : `${flags.length} deterministic workflow flag(s) require source inspection by a qualified reviewer.`,
    recommendedNextStep: "Review the original source material and complete the local human review workflow.",
    confidenceScore: confidence,
    generatedAt: timestamp,
    disclaimer: "AI output is for workflow support only and must be reviewed by a qualified clinician."
  };

  intake.aiSummary = summary;
  intake.riskFlags = flags;
  if (confidence < 0.75 || flags.some((flag) => flag.severity === "High")) {
    intake.reviewStatus = "NeedsReview";
  }
  intake.auditLogs.push(audit(nextAuditId++, intake.id, "SummaryGenerated", "Mock", timestamp, "Deterministic browser-demo summary generated; no external provider was called."));
  return intake;
}

function addContextEvent(intake: IntakeDetail, payload: CreateContextEventPayload): ContextEvent {
  const timestamp = new Date().toISOString();
  const contextEvent: ContextEvent = {
    id: nextContextEventId++,
    intakeId: intake.id,
    sourceType: payload.sourceType,
    sourceLabel: payload.sourceLabel.trim(),
    content: payload.content.trim(),
    capturedAt: payload.capturedAt ?? timestamp,
    createdBy: payload.createdBy.trim(),
    confidenceScore: payload.confidenceScore,
    metadataJson: payload.metadataJson,
    createdAt: timestamp
  };
  intake.contextEvents.push(contextEvent);
  intake.auditLogs.push(audit(nextAuditId++, intake.id, "ContextEventAdded", contextEvent.createdBy, timestamp, `${contextEvent.sourceType} source added with provenance.`));
  return contextEvent;
}

function addTranscriptContext(intake: IntakeDetail, payload: CreateTranscriptContextPayload): ContextEvent {
  return addContextEvent(intake, {
    sourceType: "TranscriptText",
    sourceLabel: payload.transcriptLabel,
    content: payload.transcriptText,
    capturedAt: payload.capturedAt,
    createdBy: payload.createdBy,
    confidenceScore: payload.confidenceScore,
    metadataJson: JSON.stringify({ inputMode: "pasted-fictional-transcript", speakerContext: payload.speakerContext })
  });
}

function addDocumentContext(intake: IntakeDetail, payload: CreateDocumentContextPayload): ContextEvent {
  return addContextEvent(intake, {
    sourceType: "DocumentText",
    sourceLabel: payload.documentLabel,
    content: payload.documentText,
    capturedAt: payload.capturedAt,
    createdBy: payload.createdBy,
    confidenceScore: payload.confidenceScore,
    metadataJson: JSON.stringify({ inputMode: "pasted-fictional-document-text", documentType: payload.documentType, pageReference: payload.pageReference })
  });
}

function addMedication(intake: IntakeDetail, payload: CreateMedicationPayload): MedicationEntry {
  const timestamp = new Date().toISOString();
  const entry: MedicationEntry = {
    id: nextMedicationId++,
    intakeId: intake.id,
    medicationName: payload.medicationName.trim(),
    normalizedName: payload.medicationName.trim().toLowerCase(),
    category: payload.category,
    dose: payload.dose,
    route: payload.route,
    frequency: payload.frequency,
    startedAt: payload.startedAt,
    stoppedAt: payload.stoppedAt,
    reasonForUse: payload.reasonForUse,
    source: payload.source,
    prescribedBy: payload.prescribedBy,
    notes: payload.notes,
    createdAt: timestamp
  };
  intake.medicationEntries.push(entry);
  intake.medicationDocumentationQuality = assessMedicationQuality(intake.medicationEntries);
  intake.auditLogs.push(audit(nextAuditId++, intake.id, "MedicationContextAdded", "demo-user", timestamp, "Fictional medication-history context added; no prescribing or reconciliation performed."));
  return entry;
}

function analyseMedicationContext(intake: IntakeDetail): IntakeDetail {
  const timestamp = new Date().toISOString();
  intake.medicationDocumentationQuality = assessMedicationQuality(intake.medicationEntries);
  intake.medicationSignals = intake.medicationDocumentationQuality.issues.slice(0, 4).map((issue) => ({
    id: nextMedicationSignalId++,
    intakeId: intake.id,
    medicationEntryId: issue.medicationEntryId,
    label: "Medication documentation gap",
    severity: "Medium",
    rationale: issue.reason,
    reviewerQuestion: `Can the ${issue.field} field for ${issue.medicationName} be clarified from an approved source?`,
    evidenceSourceType: "MedicationHistory",
    evidenceSourceLabel: `${issue.medicationName} medication entry`,
    evidenceSnippet: `${issue.field} is not documented in this fictional entry.`,
    createdAt: timestamp
  }));
  intake.auditLogs.push(audit(nextAuditId++, intake.id, "MedicationContextAnalysed", "demo-user", timestamp, "Deterministic documentation prompts generated; no clinical decision was made."));
  return intake;
}

function updateReviewStatus(intake: IntakeDetail, reviewStatus: ReviewStatus, reviewNote: string | null | undefined, actor: string): IntakeDetail {
  intake.reviewStatus = reviewStatus;
  intake.auditLogs.push(
    audit(
      nextAuditId++,
      intake.id,
      "ReviewStatusChanged",
      actor,
      new Date().toISOString(),
      reviewNote?.trim() || `Review status changed to ${reviewStatus} in the fictional workflow.`
    )
  );
  return intake;
}

function makeRiskFlag(intake: IntakeDetail, label: string, severity: RiskSeverity, reason: string, keyword: string): RiskFlag {
  const source = [
    { sourceType: "IntakeText" as ContextSourceType, sourceLabel: "Original intake note", content: intake.intakeText },
    ...intake.contextEvents
  ].find((item) => item.content.toLowerCase().includes(keyword));

  return {
    id: nextRiskFlagId++,
    intakeId: intake.id,
    label,
    severity,
    reason,
    evidenceSourceType: source?.sourceType ?? null,
    evidenceSourceLabel: source?.sourceLabel ?? null,
    evidenceSnippet: source ? excerpt(source.content, keyword) : null
  };
}

function assessMedicationQuality(entries: MedicationEntry[]): MedicationDocumentationQuality {
  if (entries.length === 0) {
    return emptyMedicationQuality();
  }

  const issues: MedicationDocumentationIssue[] = [];
  for (const entry of entries) {
    if (!entry.dose) issues.push({ medicationEntryId: entry.id, medicationName: entry.medicationName, field: "dose", reason: "Dose is missing." });
    if (!entry.frequency) issues.push({ medicationEntryId: entry.id, medicationName: entry.medicationName, field: "frequency", reason: "Frequency is missing." });
    if (!entry.route) issues.push({ medicationEntryId: entry.id, medicationName: entry.medicationName, field: "route", reason: "Route is missing." });
    if (entry.source === "Unknown") issues.push({ medicationEntryId: entry.id, medicationName: entry.medicationName, field: "source", reason: "Information source is unknown." });
  }

  const expectedFields = entries.length * 4;
  const score = Math.max(0, Math.round(((expectedFields - issues.length) / expectedFields) * 100));
  const status = score >= 90 ? "WellDocumented" : score >= 65 ? "NeedsClarification" : "Incomplete";
  return {
    score,
    status,
    summary:
      issues.length === 0
        ? "The captured fictional medication fields are mostly complete."
        : `${issues.length} medication-history field(s) should be clarified by a human reviewer.`,
    issues,
    disclaimer: documentationDisclaimer
  };
}

function emptyMedicationQuality(): MedicationDocumentationQuality {
  return {
    score: null,
    status: "NotAssessed",
    summary: "No medication context has been recorded for this fictional intake.",
    issues: [],
    disclaimer: documentationDisclaimer
  };
}

function toListItems(items: IntakeDetail[]): IntakeListItem[] {
  return [...items]
    .sort((left, right) => right.createdAt.localeCompare(left.createdAt))
    .map((intake) => ({
      id: intake.id,
      patientAlias: intake.patientAlias,
      age: intake.age,
      source: intake.source,
      reviewStatus: intake.reviewStatus,
      createdAt: intake.createdAt,
      createdBy: intake.createdBy,
      highestRiskSeverity: highestSeverity(intake.riskFlags)
    }));
}

function toReviewQueue(items: IntakeDetail[]): ReviewQueueItem[] {
  return items
    .filter((intake) => intake.reviewStatus === "NeedsReview")
    .map((intake) => ({
      id: intake.id,
      patientAlias: intake.patientAlias,
      age: intake.age,
      source: intake.source,
      createdAt: intake.createdAt,
      highestRiskSeverity: highestSeverity(intake.riskFlags) ?? "Low",
      riskFlags: clone(intake.riskFlags)
    }));
}

function toFhirStyleExport(intake: IntakeDetail): FhirStyleExport {
  return {
    resourceType: "Bundle",
    exportMode: "FHIR-style fictional export",
    disclaimer: "Fictional interoperability example only. This is not a validated FHIR implementation or a live EHR integration.",
    generatedAt: new Date().toISOString(),
    patient: { resourceType: "Patient", id: `fictional-patient-${intake.id}`, displayName: intake.patientAlias },
    intakeQuestionnaireResponse: { resourceType: "QuestionnaireResponse", id: `intake-${intake.id}`, status: "completed" },
    reviewTask: { resourceType: "Task", id: `review-${intake.id}`, localReviewStatus: intake.reviewStatus },
    medicationStatements: intake.medicationEntries.map((entry) => ({ resourceType: "MedicationStatement", id: `medication-${entry.id}`, medicationText: entry.medicationName })),
    provenance: intake.contextEvents.map((event) => ({ resourceType: "Provenance", id: `context-${event.id}`, sourceType: event.sourceType, sourceLabel: event.sourceLabel })),
    auditEvents: intake.auditLogs.map((event) => ({ resourceType: "AuditEvent", id: `audit-${event.id}`, action: event.action, actor: event.actor }))
  };
}

function highestSeverity(flags: RiskFlag[]): RiskSeverity | null {
  const rank: Record<RiskSeverity, number> = { Low: 1, Medium: 2, High: 3 };
  return flags.reduce<RiskSeverity | null>((highest, flag) => (!highest || rank[flag.severity] > rank[highest] ? flag.severity : highest), null);
}

function findIntake(id: number): IntakeDetail {
  const intake = intakes.find((item) => item.id === id);
  if (!intake) throw new Error("Intake not found");
  return intake;
}

function audit(id: number, intakeId: number, action: string, actor: string, timestamp: string, details: string): AuditLog {
  return { id, intakeId, action, actor, timestamp, details };
}

function medication(
  id: number,
  intakeId: number,
  name: string,
  dose: string | null,
  frequency: string | null,
  source: MedicationEntry["source"],
  createdAt: string
): MedicationEntry {
  return {
    id,
    intakeId,
    medicationName: name,
    normalizedName: name.toLowerCase(),
    category: "Current",
    dose,
    route: "oral",
    frequency,
    startedAt: null,
    stoppedAt: null,
    reasonForUse: null,
    source,
    prescribedBy: null,
    notes: dose && frequency ? null : "Dose or frequency needs clarification.",
    createdAt
  };
}

function truncate(value: string, maxLength: number) {
  return value.length <= maxLength ? value : `${value.slice(0, maxLength - 3)}...`;
}

function excerpt(content: string, keyword: string) {
  const index = content.toLowerCase().indexOf(keyword);
  const start = Math.max(0, index - 45);
  const end = Math.min(content.length, index + keyword.length + 70);
  return `${start > 0 ? "..." : ""}${content.slice(start, end)}${end < content.length ? "..." : ""}`;
}

function clone<T>(value: T): T {
  return structuredClone(value);
}
