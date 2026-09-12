namespace Telara.OpsApi.GraphQL.Types;

public record WorkflowStation(
    string StationId,
    bool IsLoadingDock,
    decimal? TargetOutputPerShift,
    IReadOnlyList<WorkflowEquipment> Equipment);

public record WorkflowEquipment(
    string EquipmentId,
    string EquipmentTypeName,
    string Status,
    IReadOnlyList<LatestSensorReading> LatestReadings);
