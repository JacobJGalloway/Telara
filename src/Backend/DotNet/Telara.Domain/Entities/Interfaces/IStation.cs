namespace Telara.Domain.Entities.Interfaces;

public interface IStation
{
    string FacilityId { get; set; }
    string StationId { get; set; }
    DateTime? LastOperatorActionUtc { get; set; }
    string? NextStationId { get; set; }
    bool IsLoadingDock { get; set; }
}
