import { FormEvent, useEffect, useMemo, useState, type ReactNode } from "react";
import { api } from "./api";
import type {
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
  MedicationCategory,
  MedicationDocumentationQuality,
  MedicationEntry,
  MedicationSource,
  MedicationSignal,
  ReviewQueueItem,
  RiskSeverity
} from "./types";

type Route =
  | { name: "dashboard" }
  | { name: "create" }
  | { name: "queue" }
  | { name: "detail"; id: number };

const initialForm: CreateIntakePayload = {
  patientAlias: "",
  age: 10,
  source: "family phone note",
  intakeText: "",
  createdBy: "demo-user"
};

const initialMedicationForm: MedicationFormState = {
  medicationName: "",
  category: "Current",
  dose: "",
  route: "",
  frequency: "",
  startedAt: "",
  stoppedAt: "",
  reasonForUse: "",
  source: "Unknown",
  prescribedBy: "",
  notes: ""
};

const initialContextEventForm: ContextEventFormState = {
  sourceType: "ManualNote",
  sourceLabel: "",
  content: "",
  capturedAt: "",
  createdBy: "demo-user",
  confidenceScore: "",
  metadataJson: ""
};

const initialTranscriptContextForm: TranscriptContextFormState = {
  transcriptLabel: "Mock family call transcript",
  transcriptText: "",
  capturedAt: "",
  createdBy: "demo-user",
  confidenceScore: "",
  speakerContext: "Fictional family call"
};

const initialDocumentContextForm: DocumentContextFormState = {
  documentLabel: "Mock referral note",
  documentText: "",
  capturedAt: "",
  createdBy: "demo-user",
  confidenceScore: "",
  documentType: "Referral note",
  pageReference: "page 1"
};

type ContextEventFormState = {
  sourceType: ContextSourceType;
  sourceLabel: string;
  content: string;
  capturedAt: string;
  createdBy: string;
  confidenceScore: string;
  metadataJson: string;
};

type TranscriptContextFormState = {
  transcriptLabel: string;
  transcriptText: string;
  capturedAt: string;
  createdBy: string;
  confidenceScore: string;
  speakerContext: string;
};

type DocumentContextFormState = {
  documentLabel: string;
  documentText: string;
  capturedAt: string;
  createdBy: string;
  confidenceScore: string;
  documentType: string;
  pageReference: string;
};

type MedicationFormState = {
  medicationName: string;
  category: MedicationCategory;
  dose: string;
  route: string;
  frequency: string;
  startedAt: string;
  stoppedAt: string;
  reasonForUse: string;
  source: MedicationSource;
  prescribedBy: string;
  notes: string;
};

function parseRoute(): Route {
  const hash = window.location.hash.replace(/^#\/?/, "");
  const [first, second] = hash.split("/");

  if (first === "create") {
    return { name: "create" };
  }

  if (first === "queue") {
    return { name: "queue" };
  }

  if (first === "intakes" && Number(second)) {
    return { name: "detail", id: Number(second) };
  }

  return { name: "dashboard" };
}

function navigate(path: string) {
  window.location.hash = path;
}

export default function App() {
  const [route, setRoute] = useState<Route>(parseRoute());

  useEffect(() => {
    const handleHashChange = () => setRoute(parseRoute());
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">Clinical intake workflow demo</p>
          <h1>Intake Review</h1>
        </div>
        <nav aria-label="Primary navigation">
          <button className={route.name === "dashboard" ? "active" : ""} onClick={() => navigate("/")}>
            Dashboard
          </button>
          <button className={route.name === "create" ? "active" : ""} onClick={() => navigate("/create")}>
            Create Intake
          </button>
          <button className={route.name === "queue" ? "active" : ""} onClick={() => navigate("/queue")}>
            Review Queue
          </button>
        </nav>
      </aside>

      <main className="content">
        {route.name === "dashboard" && <Dashboard />}
        {route.name === "create" && <CreateIntake />}
        {route.name === "queue" && <ReviewQueue />}
        {route.name === "detail" && <IntakeDetailPage intakeId={route.id} />}
      </main>
    </div>
  );
}

function Dashboard() {
  const [intakes, setIntakes] = useState<IntakeListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listIntakes()
      .then(setIntakes)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const counts = useMemo(
    () => ({
      newCount: intakes.filter((intake) => intake.reviewStatus === "New").length,
      needsReview: intakes.filter((intake) => intake.reviewStatus === "NeedsReview").length,
      reviewed: intakes.filter((intake) => intake.reviewStatus === "Reviewed").length
    }),
    [intakes]
  );

  return (
    <section className="page-section">
      <PageHeader
        title="Dashboard"
        subtitle="A lightweight view of intake volume and human review state."
        action={<button onClick={() => navigate("/create")}>New intake</button>}
      />
      <StatusMessage loading={loading} error={error} />
      <div className="metric-grid">
        <Metric label="New intakes" value={counts.newCount} />
        <Metric label="Needs review" value={counts.needsReview} tone="amber" />
        <Metric label="Reviewed" value={counts.reviewed} tone="green" />
      </div>
      <div className="panel">
        <h2>Recent intakes</h2>
        <IntakeTable intakes={intakes.slice(0, 8)} />
      </div>
    </section>
  );
}

function CreateIntake() {
  const [form, setForm] = useState<CreateIntakePayload>(initialForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);

    try {
      const intake = await api.createIntake(form);
      navigate(`/intakes/${intake.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to create intake");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="page-section">
      <PageHeader title="Create Intake" subtitle="Record a fictional intake note for mock AI workflow support." />
      <p className="muted">Fictional demo data only. Do not enter real patient data.</p>
      {error && <p className="alert">{error}</p>}
      <form className="form-panel" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label>
            Patient alias
            <input
              required
              value={form.patientAlias}
              onChange={(event) => setForm({ ...form, patientAlias: event.target.value })}
              placeholder="Patient A"
            />
          </label>
          <label>
            Age
            <input
              required
              type="number"
              min={0}
              max={120}
              value={form.age}
              onChange={(event) => setForm({ ...form, age: Number(event.target.value) })}
            />
          </label>
          <label>
            Source
            <input
              required
              value={form.source}
              onChange={(event) => setForm({ ...form, source: event.target.value })}
            />
          </label>
          <label>
            Created by
            <input
              required
              value={form.createdBy}
              onChange={(event) => setForm({ ...form, createdBy: event.target.value })}
            />
          </label>
        </div>
        <label>
          Intake text
          <textarea
            required
            rows={10}
            value={form.intakeText}
            onChange={(event) => setForm({ ...form, intakeText: event.target.value })}
            placeholder="Family reports sleep problems, school difficulty and attention concerns..."
          />
        </label>
        <div className="form-actions">
          <button type="submit" disabled={saving}>
            {saving ? "Saving..." : "Create intake"}
          </button>
        </div>
      </form>
    </section>
  );
}

function ReviewQueue() {
  const [items, setItems] = useState<ReviewQueueItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .listReviewQueue()
      .then(setItems)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  return (
    <section className="page-section">
      <PageHeader title="Review Queue" subtitle="Cases routed for human attention by risk flags or low confidence." />
      <StatusMessage loading={loading} error={error} />
      <div className="panel">
        {items.length === 0 && !loading ? (
          <p className="empty">No intakes are currently marked as needing review.</p>
        ) : (
          <div className="queue-list">
            {items.map((item) => (
              <button className="queue-item" key={item.id} onClick={() => navigate(`/intakes/${item.id}`)}>
                <span>
                  <strong>{item.patientAlias}</strong>
                  <small>
                    Age {item.age} · {item.source} · {formatDate(item.createdAt)}
                  </small>
                </span>
                <SeverityBadge severity={item.highestRiskSeverity} />
              </button>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function IntakeDetailPage({ intakeId }: { intakeId: number }) {
  const [intake, setIntake] = useState<IntakeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reviewNote, setReviewNote] = useState("");
  const [fhirExport, setFhirExport] = useState<FhirStyleExport | null>(null);

  useEffect(() => {
    setLoading(true);
    setReviewNote("");
    setFhirExport(null);
    api
      .getIntake(intakeId)
      .then(setIntake)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, [intakeId]);

  async function runAction(action: () => Promise<IntakeDetail>) {
    setBusy(true);
    setError(null);
    try {
      setIntake(await action());
      setFhirExport(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Action failed");
    } finally {
      setBusy(false);
    }
  }

  async function loadFhirStyleExport() {
    setBusy(true);
    setError(null);
    try {
      setFhirExport(await api.getFhirStyleExport(intakeId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load FHIR-style export");
    } finally {
      setBusy(false);
    }
  }

  async function markReviewed() {
    await runAction(() => api.updateReviewStatus(intakeId, "Reviewed", reviewNote));
    setReviewNote("");
  }

  if (loading) {
    return <StatusMessage loading={loading} error={error} />;
  }

  if (!intake) {
    return <p className="alert">Intake not found.</p>;
  }

  return (
    <section className="page-section">
      <PageHeader
        title={intake.patientAlias}
        subtitle={`Age ${intake.age} · ${intake.source} · created ${formatDate(intake.createdAt)}`}
        action={<StatusBadge status={intake.reviewStatus} />}
      />
      {error && <p className="alert">{error}</p>}

      <div className="toolbar">
        <button disabled={busy} onClick={() => runAction(() => api.generateSummary(intake.id))}>
          {intake.aiSummary ? "Regenerate summary" : "Generate summary"}
        </button>
        <button
          className="secondary"
          disabled={busy || intake.reviewStatus === "Reviewed"}
          onClick={markReviewed}
        >
          Mark reviewed
        </button>
        <button className="secondary" disabled={busy} onClick={loadFhirStyleExport}>
          View FHIR-style export
        </button>
      </div>

      {intake.reviewStatus !== "Reviewed" && (
        <div className="review-note-panel">
          <label>
            Reviewer note
            <textarea
              rows={2}
              maxLength={1000}
              value={reviewNote}
              onChange={(event) => setReviewNote(event.target.value)}
              placeholder="Optional workflow note for the audit log; not clinical advice."
            />
          </label>
          <p className="muted">Saved with the status change audit log when the case is marked reviewed.</p>
        </div>
      )}

      <div className="detail-grid">
        <article className="panel">
          <h2>Original Intake</h2>
          <p className="note-text">{intake.intakeText}</p>
        </article>

        <article className="panel">
          <h2>AI-Style Summary</h2>
          {intake.aiSummary ? (
            <div className="summary-list">
              <SummaryRow title="Presenting concerns" body={intake.aiSummary.presentingConcerns} />
              <SummaryRow title="Relevant history" body={intake.aiSummary.relevantHistory} />
              <SummaryRow title="Possible risks" body={intake.aiSummary.possibleRisks} />
              <SummaryRow title="Recommended next step" body={intake.aiSummary.recommendedNextStep} />
              <SummaryRow title="Confidence score" body={`${Math.round(intake.aiSummary.confidenceScore * 100)}%`} />
              <p className="disclaimer">{intake.aiSummary.disclaimer}</p>
            </div>
          ) : (
            <p className="empty">No summary generated yet.</p>
          )}
        </article>
      </div>

      <ContextEventsSection intake={intake} busy={busy} runAction={runAction} />

      <MedicationContextSection intake={intake} busy={busy} runAction={runAction} />

      {fhirExport && <FhirStyleExportPanel exportData={fhirExport} />}

      <div className="detail-grid">
        <article className="panel">
          <h2>Risk Flags</h2>
          {intake.riskFlags.length === 0 ? (
            <p className="empty">No configured risk keywords detected.</p>
          ) : (
            <div className="flag-list">
              {intake.riskFlags.map((flag) => (
                <div className="flag-item" key={flag.id}>
                  <SeverityBadge severity={flag.severity} />
                  <div>
                    <strong>{flag.label}</strong>
                    <p>{flag.reason}</p>
                    <EvidenceSnippet
                      sourceType={flag.evidenceSourceType}
                      sourceLabel={flag.evidenceSourceLabel}
                      snippet={flag.evidenceSnippet}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </article>

        <article className="panel">
          <h2>Audit Log</h2>
          <div className="timeline">
            {intake.auditLogs.map((log) => (
              <div className="timeline-item" key={log.id}>
                <strong>{log.action}</strong>
                <small>
                  {log.actor} · {formatDate(log.timestamp)}
                </small>
                <p>{log.details}</p>
              </div>
            ))}
          </div>
        </article>
      </div>
    </section>
  );
}

function ContextEventsSection({
  intake,
  busy,
  runAction
}: {
  intake: IntakeDetail;
  busy: boolean;
  runAction: (action: () => Promise<IntakeDetail>) => Promise<void>;
}) {
  const [form, setForm] = useState<ContextEventFormState>(initialContextEventForm);
  const [transcriptForm, setTranscriptForm] = useState<TranscriptContextFormState>(initialTranscriptContextForm);
  const [documentForm, setDocumentForm] = useState<DocumentContextFormState>(initialDocumentContextForm);

  async function handleContextSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await runAction(async () => {
      await api.addContextEvent(intake.id, toContextEventPayload(form));
      return api.getIntake(intake.id);
    });
    setForm(initialContextEventForm);
  }

  async function handleTranscriptSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await runAction(async () => {
      await api.addTranscriptContext(intake.id, toTranscriptContextPayload(transcriptForm));
      return api.getIntake(intake.id);
    });
    setTranscriptForm(initialTranscriptContextForm);
  }

  async function handleDocumentSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await runAction(async () => {
      await api.addDocumentContext(intake.id, toDocumentContextPayload(documentForm));
      return api.getIntake(intake.id);
    });
    setDocumentForm(initialDocumentContextForm);
  }

  return (
    <section className="panel context-events">
      <div className="section-heading">
        <div>
          <h2>Transcript & Context Sources</h2>
          <p>Additional text context is stored with source provenance for workflow support and human review.</p>
        </div>
      </div>

      <article className="subpanel transcript-ingestion">
        <h3>Mock Transcript Ingestion</h3>
        <p className="muted">Paste fictional transcript text only. No audio processing, diagnosis or autonomous triage is performed.</p>
        <form className="context-form" onSubmit={handleTranscriptSubmit}>
          <div className="form-grid">
            <label>
              Transcript label
              <input
                required
                value={transcriptForm.transcriptLabel}
                onChange={(event) => setTranscriptForm({ ...transcriptForm, transcriptLabel: event.target.value })}
              />
            </label>
            <label>
              Captured at
              <input
                type="datetime-local"
                value={transcriptForm.capturedAt}
                onChange={(event) => setTranscriptForm({ ...transcriptForm, capturedAt: event.target.value })}
              />
            </label>
            <label>
              Created by
              <input
                required
                value={transcriptForm.createdBy}
                onChange={(event) => setTranscriptForm({ ...transcriptForm, createdBy: event.target.value })}
              />
            </label>
            <label>
              Confidence score
              <input
                min={0}
                max={1}
                step={0.01}
                type="number"
                value={transcriptForm.confidenceScore}
                onChange={(event) => setTranscriptForm({ ...transcriptForm, confidenceScore: event.target.value })}
                placeholder="Optional, 0 to 1"
              />
            </label>
            <label className="wide-field">
              Speaker/context note
              <input
                value={transcriptForm.speakerContext}
                onChange={(event) => setTranscriptForm({ ...transcriptForm, speakerContext: event.target.value })}
                placeholder="Fictional family call, care team note, referral follow-up..."
              />
            </label>
          </div>
          <label>
            Transcript text
            <textarea
              required
              rows={4}
              value={transcriptForm.transcriptText}
              onChange={(event) => setTranscriptForm({ ...transcriptForm, transcriptText: event.target.value })}
              placeholder="Family reports sleep disruption and school support needs in a fictional call transcript..."
            />
          </label>
          <div className="form-actions">
            <button type="submit" disabled={busy}>
              Add mock transcript
            </button>
          </div>
        </form>
      </article>

      <article className="subpanel document-ingestion">
        <h3>Mock Document/OCR Text</h3>
        <p className="muted">Paste fictional document text only. No OCR, image interpretation, diagnosis or autonomous triage is performed.</p>
        <form className="context-form" onSubmit={handleDocumentSubmit}>
          <div className="form-grid">
            <label>
              Document label
              <input
                required
                value={documentForm.documentLabel}
                onChange={(event) => setDocumentForm({ ...documentForm, documentLabel: event.target.value })}
              />
            </label>
            <label>
              Document type
              <input
                value={documentForm.documentType}
                onChange={(event) => setDocumentForm({ ...documentForm, documentType: event.target.value })}
                placeholder="Referral note, medication list, care summary..."
              />
            </label>
            <label>
              Page/reference
              <input
                value={documentForm.pageReference}
                onChange={(event) => setDocumentForm({ ...documentForm, pageReference: event.target.value })}
                placeholder="page 1, section 2..."
              />
            </label>
            <label>
              Captured at
              <input
                type="datetime-local"
                value={documentForm.capturedAt}
                onChange={(event) => setDocumentForm({ ...documentForm, capturedAt: event.target.value })}
              />
            </label>
            <label>
              Created by
              <input
                required
                value={documentForm.createdBy}
                onChange={(event) => setDocumentForm({ ...documentForm, createdBy: event.target.value })}
              />
            </label>
            <label>
              Confidence score
              <input
                min={0}
                max={1}
                step={0.01}
                type="number"
                value={documentForm.confidenceScore}
                onChange={(event) => setDocumentForm({ ...documentForm, confidenceScore: event.target.value })}
                placeholder="Optional, 0 to 1"
              />
            </label>
          </div>
          <label>
            Document text
            <textarea
              required
              rows={4}
              value={documentForm.documentText}
              onChange={(event) => setDocumentForm({ ...documentForm, documentText: event.target.value })}
              placeholder="Fictional referral note or medication-list text copied from a safe non-patient example..."
            />
          </label>
          <div className="form-actions">
            <button type="submit" disabled={busy}>
              Add mock document text
            </button>
          </div>
        </form>
      </article>

      <form className="context-form" onSubmit={handleContextSubmit}>
        <h3>Additional Context Source</h3>
        <div className="form-grid">
          <label>
            Source type
            <select
              value={form.sourceType}
              onChange={(event) => setForm({ ...form, sourceType: event.target.value as ContextSourceType })}
            >
              <option value="IntakeText">Intake text</option>
              <option value="TranscriptText">Transcript text</option>
              <option value="DocumentText">Document/OCR text</option>
              <option value="MedicationHistory">Medication history</option>
              <option value="ManualNote">Manual note</option>
            </select>
          </label>
          <label>
            Source label
            <input
              required
              value={form.sourceLabel}
              onChange={(event) => setForm({ ...form, sourceLabel: event.target.value })}
              placeholder="Family call transcript"
            />
          </label>
          <label>
            Captured at
            <input
              type="datetime-local"
              value={form.capturedAt}
              onChange={(event) => setForm({ ...form, capturedAt: event.target.value })}
            />
          </label>
          <label>
            Created by
            <input
              required
              value={form.createdBy}
              onChange={(event) => setForm({ ...form, createdBy: event.target.value })}
            />
          </label>
          <label>
            Confidence score
            <input
              min={0}
              max={1}
              step={0.01}
              type="number"
              value={form.confidenceScore}
              onChange={(event) => setForm({ ...form, confidenceScore: event.target.value })}
              placeholder="Optional, 0 to 1"
            />
          </label>
          <label>
            Metadata JSON
            <input
              value={form.metadataJson}
              onChange={(event) => setForm({ ...form, metadataJson: event.target.value })}
              placeholder='Optional, e.g. {"page":1}'
            />
          </label>
        </div>
        <label>
          Context content
          <textarea
            required
            rows={4}
            value={form.content}
            onChange={(event) => setForm({ ...form, content: event.target.value })}
            placeholder="Fictional source text only. Do not enter real patient data."
          />
        </label>
        <div className="form-actions">
          <button type="submit" disabled={busy}>
            Add context source
          </button>
        </div>
      </form>

      <ContextEventList contextEvents={intake.contextEvents} />
    </section>
  );
}

function ContextEventList({ contextEvents }: { contextEvents: ContextEvent[] }) {
  return (
    <article className="subpanel">
      <h3>Captured Context</h3>
      {contextEvents.length === 0 ? (
        <p className="empty">No additional context sources recorded yet.</p>
      ) : (
        <div className="context-event-list">
          {contextEvents.map((contextEvent) => (
            <div className="context-event-item" key={contextEvent.id}>
              <div className="context-event-title">
                <strong>{contextEvent.sourceLabel}</strong>
                <span>{formatContextSourceType(contextEvent.sourceType)}</span>
              </div>
              <small>
                Captured {formatDate(contextEvent.capturedAt)} · added by {contextEvent.createdBy}
                {contextEvent.confidenceScore === null
                  ? ""
                  : ` · confidence ${Math.round(contextEvent.confidenceScore * 100)}%`}
              </small>
              <p>{contextEvent.content}</p>
              {contextEvent.metadataJson && <code>{contextEvent.metadataJson}</code>}
            </div>
          ))}
        </div>
      )}
    </article>
  );
}

function MedicationContextSection({
  intake,
  busy,
  runAction
}: {
  intake: IntakeDetail;
  busy: boolean;
  runAction: (action: () => Promise<IntakeDetail>) => Promise<void>;
}) {
  const [form, setForm] = useState<MedicationFormState>(initialMedicationForm);

  async function handleMedicationSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await runAction(async () => {
      await api.addMedication(intake.id, toMedicationPayload(form));
      return api.getIntake(intake.id);
    });
    setForm(initialMedicationForm);
  }

  return (
    <section className="panel medication-context">
      <div className="section-heading">
        <div>
          <h2>Medication Context</h2>
          <p>Medication signals are workflow support only and must be reviewed by a clinician or pharmacist.</p>
        </div>
        <button disabled={busy} onClick={() => runAction(() => api.analyseMedicationContext(intake.id))}>
          Analyse medication context
        </button>
      </div>

      <form className="medication-form" onSubmit={handleMedicationSubmit}>
        <div className="form-grid">
          <label>
            Medication name
            <input
              required
              value={form.medicationName}
              onChange={(event) => setForm({ ...form, medicationName: event.target.value })}
              placeholder="Ibuprofen"
            />
          </label>
          <label>
            Category
            <select
              value={form.category}
              onChange={(event) => setForm({ ...form, category: event.target.value as MedicationCategory })}
            >
              <option value="Current">Current</option>
              <option value="Recent">Recent</option>
              <option value="Past">Past</option>
              <option value="OTC">OTC</option>
              <option value="FamilyHousehold">Family/Household</option>
            </select>
          </label>
          <label>
            Dose
            <input value={form.dose} onChange={(event) => setForm({ ...form, dose: event.target.value })} />
          </label>
          <label>
            Frequency
            <input
              value={form.frequency}
              onChange={(event) => setForm({ ...form, frequency: event.target.value })}
              placeholder="e.g. twice daily"
            />
          </label>
          <label>
            Route
            <input value={form.route} onChange={(event) => setForm({ ...form, route: event.target.value })} />
          </label>
          <label>
            Source
            <select
              value={form.source}
              onChange={(event) => setForm({ ...form, source: event.target.value as MedicationSource })}
            >
              <option value="PatientReported">Patient reported</option>
              <option value="FamilyReported">Family reported</option>
              <option value="ClinicianReported">Clinician reported</option>
              <option value="Unknown">Unknown</option>
            </select>
          </label>
          <label>
            Started
            <input
              type="date"
              value={form.startedAt}
              onChange={(event) => setForm({ ...form, startedAt: event.target.value })}
            />
          </label>
          <label>
            Stopped
            <input
              type="date"
              value={form.stoppedAt}
              onChange={(event) => setForm({ ...form, stoppedAt: event.target.value })}
            />
          </label>
          <label>
            Reason for use
            <input
              value={form.reasonForUse}
              onChange={(event) => setForm({ ...form, reasonForUse: event.target.value })}
            />
          </label>
          <label>
            Prescribed by
            <input value={form.prescribedBy} onChange={(event) => setForm({ ...form, prescribedBy: event.target.value })} />
          </label>
        </div>
        <label>
          Notes
          <textarea
            rows={3}
            value={form.notes}
            onChange={(event) => setForm({ ...form, notes: event.target.value })}
            placeholder="Allergy, side effect, OTC context, household medication, uncertainty..."
          />
        </label>
        <div className="form-actions">
          <button type="submit" disabled={busy}>
            Add medication
          </button>
        </div>
      </form>

      <div className="detail-grid">
        <MedicationQualityPanel quality={intake.medicationDocumentationQuality} />
        <MedicationTimeline medications={intake.medicationEntries} />
        <MedicationSignals signals={intake.medicationSignals} />
      </div>
    </section>
  );
}

function MedicationQualityPanel({ quality }: { quality: MedicationDocumentationQuality }) {
  return (
    <article className="subpanel medication-quality">
      <h3>Documentation Quality</h3>
      <div className="quality-score">
        <strong>{quality.score === null ? "Not assessed" : `${quality.score}%`}</strong>
        <span>{formatMedicationQualityStatus(quality.status)}</span>
      </div>
      <p>{quality.summary}</p>
      {quality.issues.length > 0 && (
        <div className="quality-issues">
          {quality.issues.slice(0, 6).map((issue) => (
            <p key={`${issue.medicationEntryId}-${issue.field}-${issue.reason}`}>
              <strong>{issue.medicationName}</strong>: {issue.reason}
            </p>
          ))}
          {quality.issues.length > 6 && <p>{quality.issues.length - 6} more documentation items need clarification.</p>}
        </div>
      )}
      <small>{quality.disclaimer}</small>
    </article>
  );
}

function MedicationTimeline({ medications }: { medications: MedicationEntry[] }) {
  return (
    <article className="subpanel">
      <h3>Medication Timeline</h3>
      {medications.length === 0 ? (
        <p className="empty">No medication context recorded yet.</p>
      ) : (
        <div className="medication-list">
          {medications.map((medication) => (
            <div className="medication-item" key={medication.id}>
              <div className="medication-title">
                <strong>{medication.medicationName}</strong>
                <span>{formatMedicationCategory(medication.category)}</span>
              </div>
              <small>
                {formatMedicationTiming(medication)} · {formatMedicationSource(medication.source)}
              </small>
              <p>
                {[medication.dose, medication.frequency, medication.route].filter(Boolean).join(" · ") ||
                  "Dose/frequency not documented"}
              </p>
              {medication.reasonForUse && <p>Reason: {medication.reasonForUse}</p>}
              {medication.notes && <p>Notes: {medication.notes}</p>}
            </div>
          ))}
        </div>
      )}
    </article>
  );
}

function MedicationSignals({ signals }: { signals: MedicationSignal[] }) {
  return (
    <article className="subpanel">
      <h3>Medication Signals</h3>
      {signals.length === 0 ? (
        <p className="empty">No medication review signals generated yet.</p>
      ) : (
        <div className="flag-list">
          {signals.map((signal) => (
            <div className="flag-item" key={signal.id}>
              <SeverityBadge severity={signal.severity} />
              <div>
                <strong>{signal.label}</strong>
                <p>{signal.rationale}</p>
                <p className="review-question">Question: {signal.reviewerQuestion}</p>
                <EvidenceSnippet
                  sourceType={signal.evidenceSourceType}
                  sourceLabel={signal.evidenceSourceLabel}
                  snippet={signal.evidenceSnippet}
                />
                <small>Workflow support only · {formatDate(signal.createdAt)}</small>
              </div>
            </div>
          ))}
        </div>
      )}
    </article>
  );
}

function FhirStyleExportPanel({ exportData }: { exportData: FhirStyleExport }) {
  return (
    <section className="panel fhir-export">
      <div className="section-heading">
        <div>
          <h2>FHIR-Style Export</h2>
          <p>Fictional interoperability example only. This is not a live EHR/FHIR integration.</p>
        </div>
      </div>
      <p className="disclaimer">{exportData.disclaimer}</p>
      <pre className="json-preview">{JSON.stringify(exportData, null, 2)}</pre>
    </section>
  );
}

function PageHeader({
  title,
  subtitle,
  action
}: {
  title: string;
  subtitle: string;
  action?: ReactNode;
}) {
  return (
    <header className="page-header">
      <div>
        <h2>{title}</h2>
        <p>{subtitle}</p>
      </div>
      {action}
    </header>
  );
}

function StatusMessage({ loading, error }: { loading: boolean; error: string | null }) {
  if (loading) {
    return <p className="muted">Loading...</p>;
  }

  if (error) {
    return <p className="alert">{error}</p>;
  }

  return null;
}

function Metric({ label, value, tone }: { label: string; value: number; tone?: "amber" | "green" }) {
  return (
    <div className={`metric ${tone ?? ""}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function IntakeTable({ intakes }: { intakes: IntakeListItem[] }) {
  if (intakes.length === 0) {
    return <p className="empty">No intakes yet. Create one to start the workflow.</p>;
  }

  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Patient</th>
            <th>Status</th>
            <th>Risk</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {intakes.map((intake) => (
            <tr key={intake.id} onClick={() => navigate(`/intakes/${intake.id}`)}>
              <td>
                <strong>{intake.patientAlias}</strong>
                <small>Age {intake.age}</small>
              </td>
              <td>
                <StatusBadge status={intake.reviewStatus} />
              </td>
              <td>{intake.highestRiskSeverity ? <SeverityBadge severity={intake.highestRiskSeverity} /> : "None"}</td>
              <td>{formatDate(intake.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SummaryRow({ title, body }: { title: string; body: string }) {
  return (
    <div className="summary-row">
      <strong>{title}</strong>
      <p>{body}</p>
    </div>
  );
}

function EvidenceSnippet({
  sourceType,
  sourceLabel,
  snippet
}: {
  sourceType: ContextSourceType | null;
  sourceLabel: string | null;
  snippet: string | null;
}) {
  if (!snippet) {
    return null;
  }

  return (
    <div className="evidence-snippet">
      <small>
        Evidence · {sourceType ? formatContextSourceType(sourceType) : "Source"} · {sourceLabel ?? "Unlabelled source"}
      </small>
      <p>{snippet}</p>
    </div>
  );
}

function toContextEventPayload(form: ContextEventFormState): CreateContextEventPayload {
  const clean = (value: string) => {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  };

  const confidence = clean(form.confidenceScore);

  return {
    sourceType: form.sourceType,
    sourceLabel: form.sourceLabel.trim(),
    content: form.content.trim(),
    capturedAt: clean(form.capturedAt),
    createdBy: form.createdBy.trim(),
    confidenceScore: confidence === null ? null : Number(confidence),
    metadataJson: clean(form.metadataJson)
  };
}

function toTranscriptContextPayload(form: TranscriptContextFormState): CreateTranscriptContextPayload {
  const clean = (value: string) => {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  };

  const confidence = clean(form.confidenceScore);

  return {
    transcriptLabel: form.transcriptLabel.trim(),
    transcriptText: form.transcriptText.trim(),
    capturedAt: clean(form.capturedAt),
    createdBy: form.createdBy.trim(),
    confidenceScore: confidence === null ? null : Number(confidence),
    speakerContext: clean(form.speakerContext)
  };
}

function toDocumentContextPayload(form: DocumentContextFormState): CreateDocumentContextPayload {
  const clean = (value: string) => {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  };

  const confidence = clean(form.confidenceScore);

  return {
    documentLabel: form.documentLabel.trim(),
    documentText: form.documentText.trim(),
    capturedAt: clean(form.capturedAt),
    createdBy: form.createdBy.trim(),
    confidenceScore: confidence === null ? null : Number(confidence),
    documentType: clean(form.documentType),
    pageReference: clean(form.pageReference)
  };
}

function toMedicationPayload(form: MedicationFormState): CreateMedicationPayload {
  const clean = (value: string) => {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  };

  return {
    medicationName: form.medicationName.trim(),
    category: form.category,
    dose: clean(form.dose),
    route: clean(form.route),
    frequency: clean(form.frequency),
    startedAt: clean(form.startedAt),
    stoppedAt: clean(form.stoppedAt),
    reasonForUse: clean(form.reasonForUse),
    source: form.source,
    prescribedBy: clean(form.prescribedBy),
    notes: clean(form.notes)
  };
}

function StatusBadge({ status }: { status: string }) {
  return <span className={`status status-${status.toLowerCase()}`}>{status}</span>;
}

function SeverityBadge({ severity }: { severity: RiskSeverity }) {
  return <span className={`severity severity-${severity.toLowerCase()}`}>{severity}</span>;
}

function formatMedicationCategory(category: MedicationCategory) {
  return category === "FamilyHousehold" ? "Family/Household" : category;
}

function formatMedicationSource(source: MedicationSource) {
  const labels: Record<MedicationSource, string> = {
    PatientReported: "Patient reported",
    FamilyReported: "Family reported",
    ClinicianReported: "Clinician reported",
    Unknown: "Unknown source"
  };

  return labels[source];
}

function formatMedicationQualityStatus(status: MedicationDocumentationQuality["status"]) {
  const labels: Record<MedicationDocumentationQuality["status"], string> = {
    NotAssessed: "Not assessed",
    WellDocumented: "Mostly complete",
    NeedsClarification: "Needs clarification",
    Incomplete: "Incomplete"
  };

  return labels[status];
}

function formatContextSourceType(sourceType: ContextSourceType) {
  const labels: Record<ContextSourceType, string> = {
    IntakeText: "Intake text",
    TranscriptText: "Transcript text",
    DocumentText: "Document/OCR text",
    MedicationHistory: "Medication history",
    ManualNote: "Manual note"
  };

  return labels[sourceType];
}

function formatMedicationTiming(medication: MedicationEntry) {
  const started = medication.startedAt ? formatDateOnly(medication.startedAt) : "start unknown";
  const stopped = medication.stoppedAt ? formatDateOnly(medication.stoppedAt) : "not stopped";
  return `${started} to ${stopped}`;
}

function formatDateOnly(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium"
  }).format(new Date(value));
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}
