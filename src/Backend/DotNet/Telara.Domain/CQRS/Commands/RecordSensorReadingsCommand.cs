using Mediator;

namespace Telara.Domain.CQRS.Commands;

public record SensorReadingInput(string SensorId, string ReadingType, decimal Value);

public record RecordSensorReadingsCommand(
    string FacilityId,
    string StationId,
    string EquipmentId,
    IReadOnlyList<SensorReadingInput> Readings) : IRequest<bool>;
