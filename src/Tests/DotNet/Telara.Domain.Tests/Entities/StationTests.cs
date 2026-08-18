using Telara.Domain.Entities;

namespace Telara.Domain.Tests.Entities;

public class StationTests
{
    [Fact]
    public void IsOperational_ReturnsFalse_WhenNoEquipment()
    {
        var station = new Station { FacilityId = "F1", StationId = "S1" };

        Assert.False(station.IsOperational());
    }

    [Fact]
    public void IsOperational_ReturnsTrue_WhenAllEquipmentOperational()
    {
        var station = new Station
        {
            FacilityId = "F1",
            StationId = "S1",
            Equipment =
            [
                new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "E1", Status = EquipmentStatus.Operational },
                new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "E2", Status = EquipmentStatus.Operational },
            ],
        };

        Assert.True(station.IsOperational());
    }

    [Fact]
    public void IsOperational_ReturnsFalse_WhenAnyEquipmentFaulted()
    {
        var station = new Station
        {
            FacilityId = "F1",
            StationId = "S1",
            Equipment =
            [
                new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "E1", Status = EquipmentStatus.Operational },
                new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "E2", Status = EquipmentStatus.Faulted },
            ],
        };

        Assert.False(station.IsOperational());
    }
}
