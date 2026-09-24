namespace Telara.Domain.Exceptions;

public sealed class PredecessorLinkConflictException(string facilityId, string predecessorStationId)
    : Exception($"Station {predecessorStationId} at facility {facilityId} cannot feed into a new successor - it already has one, or it is a loading dock.")
{
    public string FacilityId { get; } = facilityId;
    public string PredecessorStationId { get; } = predecessorStationId;
}
