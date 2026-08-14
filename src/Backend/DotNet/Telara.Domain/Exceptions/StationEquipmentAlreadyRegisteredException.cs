namespace Telara.Domain.Exceptions;

public sealed class StationEquipmentAlreadyRegisteredException(string facilityId, string stationId, string equipmentId)
    : Exception($"StationEquipment {equipmentId} at station {stationId} (facility {facilityId}) is already registered.")
{
    public string FacilityId { get; } = facilityId;
    public string StationId { get; } = stationId;
    public string EquipmentId { get; } = equipmentId;
}
