namespace Telara.Domain.Entities.Interfaces;

public interface IStationEquipmentReading
{
    long Id { get; set; }
    string StationEquipmentId { get; set; }
    DateTime ReadingDateTime { get; set; }
    decimal? BeltSpeed { get; set; }
    decimal? BeltTemp { get; set; }
    decimal? OilTemp { get; set; }
    decimal? BladeSpeed { get; set; }
    decimal? BladeTemp { get; set; }
    decimal? MotorSpeed { get; set; }
    decimal? MotorTemp { get; set; }
    decimal? BeltVibration { get; set; }
}
