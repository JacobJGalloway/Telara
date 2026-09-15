namespace Telara.OpsApi.GraphQL.Types;

public record StationSummary(string StationId, bool IsLoadingDock, string? NextStationId);
