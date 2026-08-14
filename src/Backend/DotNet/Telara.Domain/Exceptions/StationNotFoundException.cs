namespace Telara.Domain.Exceptions;

public sealed class StationNotFoundException(string facilityId, string stationId)
    : Exception($"Station {stationId} at facility {facilityId} is not registered.")
{
    public string FacilityId { get; } = facilityId;
    public string StationId { get; } = stationId;
}
