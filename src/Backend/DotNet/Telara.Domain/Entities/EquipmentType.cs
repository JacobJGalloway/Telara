using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class EquipmentType : IEquipmentType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}
