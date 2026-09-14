namespace Telara.Client.Models;

public record WorkflowEquipment(string EquipmentId, string EquipmentTypeName, string Status);

public record WorkflowStation(string StationId, bool IsLoadingDock, IReadOnlyList<WorkflowEquipment> Equipment);

public record StationWorkflowResponse(List<WorkflowStation> StationWorkflow);
