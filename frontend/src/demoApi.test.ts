import { beforeEach, describe, expect, it } from "vitest";
import { createDemoApi, resetDemoApiState } from "./demoApi";

describe("browser-only demo API", () => {
  beforeEach(() => resetDemoApiState());

  it("exposes explicit non-clinical capability boundaries", async () => {
    const capabilities = await createDemoApi().getSystemCapabilities();

    expect(capabilities).toMatchObject({
      applicationMode: "FictionalWorkflowDemo",
      aiProvider: "Mock",
      externalProvidersEnabled: false,
      realPatientDataPermitted: false,
      diagnosisEnabled: false,
      prescribingEnabled: false,
      autonomousTriageEnabled: false,
      liveIntegrationsEnabled: false,
      clinicalValidationCompleted: false,
      workflowRehearsalClinicalMeaning: false
    });
  });

  it("keeps the seeded human-review queue deterministic", async () => {
    const api = createDemoApi();

    const intakes = await api.listIntakes();
    const queue = await api.listReviewQueue();

    expect(intakes).toHaveLength(3);
    expect(queue).toHaveLength(2);
    expect(queue.map((item) => item.highestRiskSeverity)).toEqual(["High", "Medium"]);
  });

  it("runs the fictional create, summary and human-review lifecycle in memory", async () => {
    const api = createDemoApi();
    const created = await api.createIntake({
      patientAlias: "Fictional Contract Case",
      age: 34,
      source: "fictional test note",
      intakeText: "Fictional note reports sleep concerns; details are unclear and require human review.",
      createdBy: "frontend-contract-test"
    });

    expect(created.reviewStatus).toBe("New");

    const summarised = await api.generateSummary(created.id);
    expect(summarised.reviewStatus).toBe("NeedsReview");
    expect(summarised.aiSummary?.confidenceScore).toBe(0.68);
    expect(summarised.riskFlags.map((flag) => flag.label)).toContain("Sleep concern documented");

    const reviewed = await api.updateReviewStatus(
      created.id,
      "Reviewed",
      "Fictional workflow review completed; no clinical advice recorded.",
      "frontend-contract-test"
    );

    expect(reviewed.reviewStatus).toBe("Reviewed");
    expect(reviewed.auditLogs.at(-1)).toMatchObject({
      action: "ReviewStatusChanged",
      actor: "frontend-contract-test",
      details: "Fictional workflow review completed; no clinical advice recorded."
    });
  });
});
