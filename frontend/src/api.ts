import type {
  ContextEvent,
  CreateContextEventPayload,
  CreateDocumentContextPayload,
  CreateIntakePayload,
  CreateMedicationPayload,
  CreateTranscriptContextPayload,
  IntakeDetail,
  IntakeListItem,
  MedicationDocumentationQuality,
  MedicationEntry,
  MedicationSignal,
  ReviewQueueItem,
  ReviewStatus
} from "./types";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5108";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...options?.headers
    },
    ...options
  });

  if (!response.ok) {
    const fallback = `Request failed with ${response.status}`;
    const body = await response.json().catch(() => ({ error: fallback }));
    const validationDetails = Array.isArray(body.errors)
      ? body.errors.map((error: { field: string; message: string }) => `${error.field}: ${error.message}`).join(" ")
      : null;

    throw new Error(validationDetails ?? body.message ?? body.error ?? fallback);
  }

  return response.json() as Promise<T>;
}

export const api = {
  listIntakes: () => request<IntakeListItem[]>("/api/intakes"),
  getIntake: (id: number) => request<IntakeDetail>(`/api/intakes/${id}`),
  createIntake: (payload: CreateIntakePayload) =>
    request<IntakeDetail>("/api/intakes", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  generateSummary: (id: number) =>
    request<IntakeDetail>(`/api/intakes/${id}/generate-summary`, {
      method: "POST"
    }),
  addContextEvent: (id: number, payload: CreateContextEventPayload) =>
    request<ContextEvent>(`/api/intakes/${id}/context-events`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  addTranscriptContext: (id: number, payload: CreateTranscriptContextPayload) =>
    request<ContextEvent>(`/api/intakes/${id}/transcript-context`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  addDocumentContext: (id: number, payload: CreateDocumentContextPayload) =>
    request<ContextEvent>(`/api/intakes/${id}/document-context`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  listContextEvents: (id: number) => request<ContextEvent[]>(`/api/intakes/${id}/context-events`),
  addMedication: (id: number, payload: CreateMedicationPayload) =>
    request<MedicationEntry>(`/api/intakes/${id}/medications`, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  listMedications: (id: number) => request<MedicationEntry[]>(`/api/intakes/${id}/medications`),
  analyseMedicationContext: (id: number) =>
    request<IntakeDetail>(`/api/intakes/${id}/analyse-medication-context`, {
      method: "POST"
  }),
  listMedicationSignals: (id: number) => request<MedicationSignal[]>(`/api/intakes/${id}/medication-signals`),
  getMedicationDocumentationQuality: (id: number) =>
    request<MedicationDocumentationQuality>(`/api/intakes/${id}/medication-documentation-quality`),
  listReviewQueue: () => request<ReviewQueueItem[]>("/api/review-queue"),
  updateReviewStatus: (id: number, reviewStatus: ReviewStatus, reviewNote?: string | null, actor = "demo-reviewer") =>
    request<IntakeDetail>(`/api/intakes/${id}/review-status`, {
      method: "PATCH",
      body: JSON.stringify({ reviewStatus, actor, reviewNote: reviewNote?.trim() || null })
    })
};
