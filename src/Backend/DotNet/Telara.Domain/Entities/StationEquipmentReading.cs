using Telara.Domain.Entities.Interfaces;

namespace Telara.Domain.Entities;

public class StationEquipmentReading : IStationEquipmentReading
{
    public long Id { get; set; }
    public string StationEquipmentId { get; set; } = default!;
    public DateTime ReadingDateTime { get; set; }
    public decimal? BeltSpeed { get; set; }
    public decimal? BeltTemp { get; set; }
    public decimal? OilTemp { get; set; }
    public decimal? BladeSpeed { get; set; }
    public decimal? BladeTemp { get; set; }
    public decimal? MotorSpeed { get; set; }
    public decimal? MotorTemp { get; set; }
    public decimal? BeltVibration { get; set; }
}
