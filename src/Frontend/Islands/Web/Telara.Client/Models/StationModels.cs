namespace Telara.Client.Models;

public record StationSummary(string StationId, bool IsLoadingDock, string? NextStationId);

public record StationsResponse(List<StationSummary> Stations);

public record RegisterStationResult(string FacilityId, string StationId, bool IsLoadingDock, List<string> PredecessorStationIds);

public record RegisterStationResponse(RegisterStationResult RegisterStation);

public record EquipmentType(int Id, string Name);

public record EquipmentTypesResponse(List<EquipmentType> EquipmentTypes);

public record RegisterStationEquipmentResult(string FacilityId, string StationId, string EquipmentId, int EquipmentTypeId);

public record RegisterStationEquipmentResponse(RegisterStationEquipmentResult RegisterStationEquipment);
