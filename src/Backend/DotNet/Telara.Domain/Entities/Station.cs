using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class Station : IStation
{
    public string FacilityId { get; set; } = default!;
    public string StationId { get; set; } = default!;
    public DateTime? LastOperatorActionUtc { get; set; }
    public ICollection<StationEquipment> Equipment { get; set; } = [];

    public bool IsOperational() =>
        Equipment.Count > 0 && Equipment.All(e => e.Status == EquipmentStatus.Operational);
}
