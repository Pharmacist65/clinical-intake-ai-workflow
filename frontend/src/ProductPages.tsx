import { useMemo, useState } from "react";
import {
  ArrowUpRight,
  Check,
  CircleAlert,
  FileCheck2,
  Landmark,
  LockKeyhole,
  Play,
  RotateCcw,
  ShieldCheck,
  UserCheck
} from "lucide-react";
import type { SystemCapabilities } from "./types";

type Jurisdiction = "UK" | "US";

const governanceContent = {
  UK: {
    icon: Landmark,
    title: "United Kingdom review lens",
    subtitle: "NHS deployment guidance, clinical safety and information governance prompts.",
    signal: "1,239",
    signalLabel: "GP practices in the NHS Midlands AVT rollout reported in July 2026",
    evidenceNote:
      "NHS England also reported 70,000 clinicians across 15 trusts in the Midlands rollout. These are NHS-reported implementation figures, not performance claims for this project.",
    prompts: [
      "Define and document intended purpose before selecting or configuring a product.",
      "Treat human verification, traceability and error handling as release controls.",
      "Plan DCB0129 clinical risk management and local DCB0160 responsibilities where applicable.",
      "Complete information governance, data protection and supplier assurance work before real use.",
      "Do not use a short pilot to bypass safety, regulatory or governance obligations."
    ],
    sources: [
      {
        label: "NHS England ambient scribing guidance",
        url: "https://www.england.nhs.uk/long-read/guidance-on-the-use-of-ai-enabled-ambient-scribing-products-in-health-and-care-settings/"
      },
      {
        label: "NHS Midlands AVT rollout report",
        url: "https://www.england.nhs.uk/midlands/2026/07/15/midlands-leads-the-way-on-ambient-voice-technology/"
      },
      {
        label: "NHS DCB0129 standard",
        url: "https://digital.nhs.uk/data-and-information/information-standards/governance/latest-activity/standards-and-collections/dcb0129-clinical-risk-management-its-application-in-the-manufacture-of-health-it-systems/"
      },
      {
        label: "MHRA software and AI as a medical device",
        url: "https://www.gov.uk/government/publications/software-and-artificial-intelligence-ai-as-a-medical-device"
      }
    ]
  },
  US: {
    icon: ShieldCheck,
    title: "United States review lens",
    subtitle: "Intended use, health IT transparency and implementation evidence prompts.",
    signal: "72%",
    signalLabel: "of physicians reported incorporating at least one health AI use case in the AMA 2026 survey",
    evidenceNote:
      "The AMA headline reports more than 80% professional use; the underlying chart distinguishes 72% reporting at least one use case, 9% uncertain and 19% no use. The denominator nuance is retained here.",
    prompts: [
      "Write a narrow intended-use statement and reassess it whenever output or workflow scope changes.",
      "Determine whether FDA device oversight is relevant from intended purpose, not from the AI label alone.",
      "Apply HTI-1 predictive DSI transparency requirements only where the certified health IT context applies.",
      "Document privacy, security, vendor contracting and data-flow responsibilities before real deployment.",
      "Evaluate note quality, omissions, workflow burden and human correction instead of relying on adoption metrics."
    ],
    sources: [
      {
        label: "AMA physician AI sentiment report",
        url: "https://www.ama-assn.org/system/files/physician-ai-sentiment-report.pdf"
      },
      {
        label: "ASTP/ONC HTI-1 final rule",
        url: "https://healthit.gov/regulations/hti-rules/hti-1-final-rule/"
      },
      {
        label: "ASTP/ONC decision support interventions",
        url: "https://healthit.gov/test-method/decision-support-interventions/"
      },
      {
        label: "FDA AI-enabled medical device list",
        url: "https://www.fda.gov/medical-devices/software-medical-device-samd/artificial-intelligence-enabled-medical-devices"
      }
    ]
  }
} as const;

const productPatterns = [
  {
    title: "Evidence-linked verification",
    body: "Review prompts retain a short source excerpt and provenance label so the reviewer can inspect why a deterministic rule fired.",
    boundary: "A source link explains the trigger; it does not prove clinical correctness."
  },
  {
    title: "Configurable document workflow",
    body: "Structured summary sections and downstream export shapes are separated from the provider boundary.",
    boundary: "This build uses fixed mock output and no live EHR document write-back."
  },
  {
    title: "Pre-review context assembly",
    body: "Intake, pasted transcript text, document text and medication history remain distinct, provenance-tracked sources.",
    boundary: "There is no ambient audio capture, OCR or autonomous clinical reasoning."
  },
  {
    title: "Release-gated human review",
    body: "Low confidence or configured high-severity terms create visible workflow state and an auditable handoff.",
    boundary: "Routing is a workflow control, not autonomous triage."
  }
];

export function GovernancePage({ capabilities }: { capabilities: SystemCapabilities | null }) {
  const [jurisdiction, setJurisdiction] = useState<Jurisdiction>("UK");
  const content = governanceContent[jurisdiction];
  const JurisdictionIcon = content.icon;

  return (
    <section className="page-section governance-page">
      <header className="page-header governance-header">
        <div>
          <p className="eyebrow dark">Governance explorer</p>
          <h2>One workflow, two review lenses</h2>
          <p>Current public evidence translated into implementation questions, never a compliance verdict.</p>
        </div>
        <span className="boundary-badge"><LockKeyhole size={15} /> review prompts only</span>
      </header>

      <div className="segmented-control" role="tablist" aria-label="Jurisdiction lens">
        {(["UK", "US"] as const).map((item) => (
          <button
            key={item}
            className={jurisdiction === item ? "active" : ""}
            role="tab"
            aria-selected={jurisdiction === item}
            onClick={() => setJurisdiction(item)}
          >
            {item === "UK" ? "United Kingdom" : "United States"}
          </button>
        ))}
      </div>

      <section className="governance-overview" aria-live="polite">
        <div className="jurisdiction-intro">
          <JurisdictionIcon size={24} aria-hidden="true" />
          <div>
            <h3>{content.title}</h3>
            <p>{content.subtitle}</p>
          </div>
        </div>
        <div className="market-signal">
          <strong>{content.signal}</strong>
          <span>{content.signalLabel}</span>
          <small>{content.evidenceNote}</small>
        </div>
      </section>

      <div className="governance-grid">
        <article className="panel prompt-panel">
          <div className="panel-title-row">
            <FileCheck2 size={19} aria-hidden="true" />
            <h3>Implementation review prompts</h3>
          </div>
          <ol className="review-prompt-list">
            {content.prompts.map((prompt) => (
              <li key={prompt}>{prompt}</li>
            ))}
          </ol>
        </article>

        <article className="panel source-panel">
          <div className="panel-title-row">
            <ShieldCheck size={19} aria-hidden="true" />
            <h3>Primary sources</h3>
          </div>
          <div className="source-link-list">
            {content.sources.map((source) => (
              <a key={source.url} href={source.url} target="_blank" rel="noreferrer">
                <span>{source.label}</span>
                <ArrowUpRight size={16} aria-hidden="true" />
              </a>
            ))}
          </div>
          <p className="source-date">Evidence review date: 12 August 2026</p>
        </article>
      </div>

      <section className="pattern-section">
        <div className="section-heading compact-heading">
          <div>
            <p className="eyebrow dark">Product synthesis</p>
            <h2>Useful industry patterns, narrower safety claims</h2>
          </div>
        </div>
        <div className="pattern-grid">
          {productPatterns.map((pattern, index) => (
            <article className="pattern-card" key={pattern.title}>
              <span className="pattern-index">0{index + 1}</span>
              <h3>{pattern.title}</h3>
              <p>{pattern.body}</p>
              <small>{pattern.boundary}</small>
            </article>
          ))}
        </div>
      </section>

      <CapabilityManifest capabilities={capabilities} />
    </section>
  );
}

function CapabilityManifest({ capabilities }: { capabilities: SystemCapabilities | null }) {
  const disabledCapabilities = [
    ["Real patient data", capabilities?.realPatientDataPermitted ?? false],
    ["Diagnosis", capabilities?.diagnosisEnabled ?? false],
    ["Prescribing", capabilities?.prescribingEnabled ?? false],
    ["Autonomous triage", capabilities?.autonomousTriageEnabled ?? false],
    ["Live integrations", capabilities?.liveIntegrationsEnabled ?? false],
    ["Clinical validation completed", capabilities?.clinicalValidationCompleted ?? false],
    ["Rehearsal has clinical meaning", capabilities?.workflowRehearsalClinicalMeaning ?? false],
    ["External AI providers", capabilities?.externalProvidersEnabled ?? false]
  ] as const;

  return (
    <section className="capability-manifest">
      <div>
        <p className="eyebrow dark">Machine-readable boundary</p>
        <h2>Runtime capability manifest</h2>
        <p>The UI reads the same safety contract exposed by the API.</p>
      </div>
      <div className="capability-table" role="table" aria-label="Runtime capabilities">
        <div role="row" className="capability-row enabled">
          <span role="cell"><Check size={16} /> deterministic mock provider</span>
          <strong role="cell">{capabilities?.aiProvider ?? "Mock"}</strong>
        </div>
        {disabledCapabilities.map(([label, enabled]) => (
          <div role="row" className={`capability-row ${enabled ? "enabled" : "disabled"}`} key={label}>
            <span role="cell"><CircleAlert size={16} /> {label}</span>
            <strong role="cell">{enabled ? "enabled" : "not enabled"}</strong>
          </div>
        ))}
      </div>
      <p className="manifest-disclaimer">
        {capabilities?.disclaimer ?? "This manifest is a design boundary, not regulatory clearance or clinical validation."}
      </p>
    </section>
  );
}

type RehearsalEvent = {
  actor: string;
  event: string;
  resultingState: string;
  evidence: string;
};

type RehearsalScenario = {
  id: string;
  title: string;
  objective: string;
  control: string;
  events: RehearsalEvent[];
  debrief: string[];
};

const rehearsalScenarios: RehearsalScenario[] = [
  {
    id: "provenance-gate",
    title: "Missing provenance gate",
    objective: "Verify that an output cannot appear release-ready when its source label is absent.",
    control: "Source traceability",
    events: [
      { actor: "Coordinator", event: "Adds fictional document text without a source label", resultingState: "Context incomplete", evidence: "sourceLabel = empty" },
      { actor: "Workflow", event: "Checks the release preconditions", resultingState: "Release held", evidence: "provenance gate failed" },
      { actor: "Coordinator", event: "Adds the source label and page reference", resultingState: "Context complete", evidence: "sourceLabel + pageReference recorded" },
      { actor: "Reviewer", event: "Inspects the source-linked output", resultingState: "Human review pending", evidence: "review event appended" }
    ],
    debrief: ["The release state changed only after provenance was complete.", "No clinical interpretation was tested.", "The same event sequence can be replayed deterministically."]
  },
  {
    id: "confidence-handoff",
    title: "Low-confidence handoff",
    objective: "Verify that the configured confidence threshold creates a visible human-review state.",
    control: "Human review routing",
    events: [
      { actor: "Mock provider", event: "Generates deterministic structured output", resultingState: "Summary available", evidence: "provider = Mock" },
      { actor: "Workflow", event: "Compares confidence with 0.75 threshold", resultingState: "Needs review", evidence: "confidence = 0.68" },
      { actor: "Reviewer", event: "Opens original text beside generated output", resultingState: "Evidence inspected", evidence: "source and output visible together" },
      { actor: "Reviewer", event: "Records a workflow note and status", resultingState: "Reviewed", evidence: "audit event appended" }
    ],
    debrief: ["The threshold changed routing, not clinical priority.", "The original text remained available.", "The reviewer action produced an audit record."]
  },
  {
    id: "medication-gap",
    title: "Medication documentation gap",
    objective: "Verify that incomplete medication fields generate a clarification prompt without advice.",
    control: "Documentation completeness",
    events: [
      { actor: "Coordinator", event: "Adds a fictional OTC medication entry", resultingState: "Medication captured", evidence: "dose and frequency absent" },
      { actor: "Workflow", event: "Runs deterministic completeness rules", resultingState: "Gap detected", evidence: "missing field list generated" },
      { actor: "Workflow", event: "Creates a reviewer question", resultingState: "Clarification requested", evidence: "no treatment recommendation" },
      { actor: "Pharmacist reviewer", event: "Records workflow disposition", resultingState: "Review documented", evidence: "audit event appended" }
    ],
    debrief: ["The output asks for missing information only.", "No drug interaction or appropriateness claim was made.", "The reviewer remains responsible for any clinical interpretation."]
  }
];

export function WorkflowRehearsalPage() {
  const [scenarioId, setScenarioId] = useState(rehearsalScenarios[0].id);
  const [step, setStep] = useState(0);
  const scenario = rehearsalScenarios.find((item) => item.id === scenarioId) ?? rehearsalScenarios[0];
  const visibleEvents = scenario.events.slice(0, step + 1);
  const complete = step === scenario.events.length - 1;
  const replayId = useMemo(() => stableReplayId(scenario.id, step), [scenario.id, step]);

  function selectScenario(id: string) {
    setScenarioId(id);
    setStep(0);
  }

  return (
    <section className="page-section rehearsal-page">
      <header className="page-header rehearsal-header">
        <div>
          <p className="eyebrow dark">Secondary evaluation surface</p>
          <h2>Workflow rehearsal</h2>
          <p>Deterministic checks for operational controls, state transitions and audit evidence.</p>
        </div>
        <span className="boundary-badge"><UserCheck size={15} /> clinical meaning: false</span>
      </header>

      <div className="rehearsal-tabs" role="tablist" aria-label="Rehearsal scenario">
        {rehearsalScenarios.map((item) => (
          <button
            key={item.id}
            role="tab"
            aria-selected={scenario.id === item.id}
            className={scenario.id === item.id ? "active" : ""}
            onClick={() => selectScenario(item.id)}
          >
            <span>{item.control}</span>
            <strong>{item.title}</strong>
          </button>
        ))}
      </div>

      <section className="rehearsal-workbench">
        <div className="rehearsal-summary">
          <span className="run-state">{complete ? "Run complete" : `Event ${step + 1} of ${scenario.events.length}`}</span>
          <h3>{scenario.title}</h3>
          <p>{scenario.objective}</p>
          <dl className="run-contract">
            <div><dt>Replay ID</dt><dd>{replayId}</dd></div>
            <div><dt>Evaluation scope</dt><dd>operational controls</dd></div>
            <div><dt>Clinical validity</dt><dd>not assessed</dd></div>
          </dl>
          <div className="rehearsal-controls">
            <button disabled={complete} onClick={() => setStep((current) => Math.min(current + 1, scenario.events.length - 1))}>
              <Play size={16} fill="currentColor" /> Advance event
            </button>
            <button className="icon-button secondary" onClick={() => setStep(0)} aria-label="Reset rehearsal" title="Reset rehearsal">
              <RotateCcw size={17} />
            </button>
          </div>
        </div>

        <div className="rehearsal-timeline" aria-live="polite">
          {visibleEvents.map((event, index) => (
            <article className={index === visibleEvents.length - 1 ? "current" : ""} key={`${scenario.id}-${index}`}>
              <span className="event-number">{String(index + 1).padStart(2, "0")}</span>
              <div>
                <small>{event.actor}</small>
                <h4>{event.event}</h4>
                <p><strong>{event.resultingState}</strong> | {event.evidence}</p>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className={`debrief-panel ${complete ? "visible" : ""}`} aria-hidden={!complete}>
        <div>
          <p className="eyebrow dark">Debrief</p>
          <h3>What this run demonstrates</h3>
        </div>
        <div className="debrief-checks">
          {scenario.debrief.map((item) => <p key={item}><Check size={16} /> {item}</p>)}
        </div>
        <p className="debrief-boundary">This rehearsal does not measure diagnosis, triage, prescribing, clinical accuracy or patient outcomes.</p>
      </section>
    </section>
  );
}

function stableReplayId(scenarioId: string, step: number) {
  let hash = 2166136261;
  for (const character of `${scenarioId}:${step}`) {
    hash ^= character.charCodeAt(0);
    hash = Math.imul(hash, 16777619);
  }
  return `replay-${(hash >>> 0).toString(16).padStart(8, "0")}`;
}
