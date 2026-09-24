namespace Telara.Client.Models;

public record StationOutputPoint(DateTime RecordedAtUtc, decimal UnitsProduced);

public record StationOutputResponse(List<StationOutputPoint> StationOutput);

public record StationTarget(decimal? TargetOutputPerShift);

public record StationTargetResponse(List<StationTarget> StationWorkflow);
