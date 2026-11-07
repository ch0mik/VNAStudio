namespace Vna.Core;

public enum DeviceCapabilityKind
{
    AnalysisMode,
    DeviceCommand,
    ApplicationTool,
}

public enum DeviceCapabilityAvailability
{
    Unknown,
    Available,
    Partial,
    Blocked,
}

public enum DeviceEvidenceLevel
{
    Unknown,
    ProfileDataOnly,
    RelatedModelOnly,
    ApplicationLogicPresent,
    SourceCodeInspected,
    UserReportedOnDevice,
    LabelObserved,
    ReferenceMeasured,
}

public enum DeviceConnectorRole
{
    Unknown,
    ReflectionMeasurement,
    TransmissionMeasurement,
    GeneratorOutput,
    CalibrationReference,
}

/// <summary>
/// Evidence-gated description of one app mode, device command, or application analysis tool.
/// Availability describes implementation state; evidence describes what supports that claim.
/// </summary>
public sealed record DeviceCapabilityDescriptor(
    string Id,
    DeviceCapabilityKind Kind,
    DeviceCapabilityAvailability Availability,
    DeviceEvidenceLevel Evidence,
    string? EvidenceNote = null);

/// <summary>Separates a connector's observed label from any inferred electrical function.</summary>
public sealed record DeviceConnectorDescriptor(
    string Id,
    string Label,
    DeviceConnectorRole Role,
    DeviceEvidenceLevel LabelEvidence,
    DeviceEvidenceLevel RoleEvidence);
