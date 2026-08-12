namespace ClinicalIntake.Api.Contracts;

public sealed record SystemCapabilitiesResponse(
    string ApplicationMode,
    string AiProvider,
    bool ExternalProvidersEnabled,
    bool RealPatientDataPermitted,
    bool DiagnosisEnabled,
    bool PrescribingEnabled,
    bool AutonomousTriageEnabled,
    bool LiveIntegrationsEnabled,
    bool ClinicalValidationCompleted,
    bool WorkflowRehearsalClinicalMeaning,
    IReadOnlyList<string> JurisdictionLenses,
    string Disclaimer);
