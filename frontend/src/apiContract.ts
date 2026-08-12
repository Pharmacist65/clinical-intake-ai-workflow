import type {
  ContextEvent,
  CreateContextEventPayload,
  CreateDocumentContextPayload,
  CreateIntakePayload,
  CreateMedicationPayload,
  CreateTranscriptContextPayload,
  FhirStyleExport,
  IntakeDetail,
  IntakeListItem,
  MedicationDocumentationQuality,
  MedicationEntry,
  MedicationSignal,
  ReviewQueueItem,
  ReviewStatus,
  SystemCapabilities
} from "./types";

export interface ClinicalIntakeApi {
  getSystemCapabilities(): Promise<SystemCapabilities>;
  listIntakes(): Promise<IntakeListItem[]>;
  getIntake(id: number): Promise<IntakeDetail>;
  createIntake(payload: CreateIntakePayload): Promise<IntakeDetail>;
  generateSummary(id: number): Promise<IntakeDetail>;
  addContextEvent(id: number, payload: CreateContextEventPayload): Promise<ContextEvent>;
  addTranscriptContext(id: number, payload: CreateTranscriptContextPayload): Promise<ContextEvent>;
  addDocumentContext(id: number, payload: CreateDocumentContextPayload): Promise<ContextEvent>;
  listContextEvents(id: number): Promise<ContextEvent[]>;
  addMedication(id: number, payload: CreateMedicationPayload): Promise<MedicationEntry>;
  listMedications(id: number): Promise<MedicationEntry[]>;
  analyseMedicationContext(id: number): Promise<IntakeDetail>;
  listMedicationSignals(id: number): Promise<MedicationSignal[]>;
  getMedicationDocumentationQuality(id: number): Promise<MedicationDocumentationQuality>;
  getFhirStyleExport(id: number): Promise<FhirStyleExport>;
  listReviewQueue(): Promise<ReviewQueueItem[]>;
  updateReviewStatus(
    id: number,
    reviewStatus: ReviewStatus,
    reviewNote?: string | null,
    actor?: string
  ): Promise<IntakeDetail>;
}
