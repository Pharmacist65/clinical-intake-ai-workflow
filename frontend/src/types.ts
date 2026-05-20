export type ReviewStatus = "New" | "NeedsReview" | "Reviewed";
export type RiskSeverity = "Low" | "Medium" | "High";
export type ContextSourceType = "IntakeText" | "TranscriptText" | "DocumentText" | "MedicationHistory" | "ManualNote";
export type MedicationCategory = "Current" | "Recent" | "Past" | "OTC" | "FamilyHousehold";
export type MedicationSource = "PatientReported" | "FamilyReported" | "ClinicianReported" | "Unknown";

export interface IntakeListItem {
  id: number;
  patientAlias: string;
  age: number;
  source: string;
  reviewStatus: ReviewStatus;
  createdAt: string;
  createdBy: string;
  highestRiskSeverity: RiskSeverity | null;
}

export interface IntakeDetail {
  id: number;
  patientAlias: string;
  age: number;
  intakeText: string;
  source: string;
  reviewStatus: ReviewStatus;
  createdAt: string;
  createdBy: string;
  aiSummary: AiSummary | null;
  riskFlags: RiskFlag[];
  contextEvents: ContextEvent[];
  medicationEntries: MedicationEntry[];
  medicationSignals: MedicationSignal[];
  medicationDocumentationQuality: MedicationDocumentationQuality;
  auditLogs: AuditLog[];
}

export interface AiSummary {
  id: number;
  intakeId: number;
  presentingConcerns: string;
  relevantHistory: string;
  possibleRisks: string;
  recommendedNextStep: string;
  confidenceScore: number;
  generatedAt: string;
  disclaimer: string;
}

export interface RiskFlag {
  id: number;
  intakeId: number;
  label: string;
  severity: RiskSeverity;
  reason: string;
  evidenceSourceType: ContextSourceType | null;
  evidenceSourceLabel: string | null;
  evidenceSnippet: string | null;
}

export interface AuditLog {
  id: number;
  intakeId: number;
  action: string;
  actor: string;
  timestamp: string;
  details: string;
}

export interface ContextEvent {
  id: number;
  intakeId: number;
  sourceType: ContextSourceType;
  sourceLabel: string;
  content: string;
  capturedAt: string;
  createdBy: string;
  confidenceScore: number | null;
  metadataJson: string | null;
  createdAt: string;
}

export interface MedicationEntry {
  id: number;
  intakeId: number;
  medicationName: string;
  normalizedName: string;
  category: MedicationCategory;
  dose: string | null;
  route: string | null;
  frequency: string | null;
  startedAt: string | null;
  stoppedAt: string | null;
  reasonForUse: string | null;
  source: MedicationSource;
  prescribedBy: string | null;
  notes: string | null;
  createdAt: string;
}

export interface MedicationSignal {
  id: number;
  intakeId: number;
  medicationEntryId: number | null;
  label: string;
  severity: RiskSeverity;
  rationale: string;
  reviewerQuestion: string;
  evidenceSourceType: ContextSourceType | null;
  evidenceSourceLabel: string | null;
  evidenceSnippet: string | null;
  createdAt: string;
}

export interface MedicationDocumentationQuality {
  score: number | null;
  status: "NotAssessed" | "WellDocumented" | "NeedsClarification" | "Incomplete";
  summary: string;
  issues: MedicationDocumentationIssue[];
  disclaimer: string;
}

export interface MedicationDocumentationIssue {
  medicationEntryId: number | null;
  medicationName: string;
  field: string;
  reason: string;
}

export interface ReviewQueueItem {
  id: number;
  patientAlias: string;
  age: number;
  source: string;
  createdAt: string;
  highestRiskSeverity: RiskSeverity;
  riskFlags: RiskFlag[];
}

export interface CreateIntakePayload {
  patientAlias: string;
  age: number;
  intakeText: string;
  source: string;
  createdBy: string;
}

export interface CreateMedicationPayload {
  medicationName: string;
  category: MedicationCategory;
  dose: string | null;
  route: string | null;
  frequency: string | null;
  startedAt: string | null;
  stoppedAt: string | null;
  reasonForUse: string | null;
  source: MedicationSource;
  prescribedBy: string | null;
  notes: string | null;
}

export interface CreateContextEventPayload {
  sourceType: ContextSourceType;
  sourceLabel: string;
  content: string;
  capturedAt: string | null;
  createdBy: string;
  confidenceScore: number | null;
  metadataJson: string | null;
}

export interface CreateTranscriptContextPayload {
  transcriptLabel: string;
  transcriptText: string;
  capturedAt: string | null;
  createdBy: string;
  confidenceScore: number | null;
  speakerContext: string | null;
}

export interface CreateDocumentContextPayload {
  documentLabel: string;
  documentText: string;
  capturedAt: string | null;
  createdBy: string;
  confidenceScore: number | null;
  documentType: string | null;
  pageReference: string | null;
}
