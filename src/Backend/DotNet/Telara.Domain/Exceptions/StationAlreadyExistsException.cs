namespace Telara.Domain.Exceptions;

public sealed class StationAlreadyExistsException(string facilityId, string stationId)
    : Exception($"Station {stationId} at facility {facilityId} is already registered.")
{
    public string FacilityId { get; } = facilityId;
    public string StationId { get; } = stationId;
}
