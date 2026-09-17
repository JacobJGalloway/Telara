namespace Telara.Client.Models;

public record LatestSensorReading(string SensorId, string ReadingType, decimal Value, DateTime ReadingAtUtc);

public record WorkflowEquipment(
    string EquipmentId, string EquipmentTypeName, string Status, IReadOnlyList<LatestSensorReading> LatestReadings);

public record WorkflowStation(string StationId, bool IsLoadingDock, IReadOnlyList<WorkflowEquipment> Equipment);

public record StationWorkflowResponse(List<WorkflowStation> StationWorkflow);

public record SensorReadingPoint(string ReadingType, decimal Value, DateTime ReadingAtUtc);

public record SensorReadingsResponse(List<SensorReadingPoint> SensorReadings);
