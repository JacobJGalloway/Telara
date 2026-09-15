using Telara.Domain.Entities;
using Telara.Mcp.Internal.Tools;

namespace Telara.Mcp.Internal.Tests.Tools;

public class SensorReadingToolsTests
{
    [Fact]
    public async Task RecordStationOutput_GreenPath_ReturnsTrue_WhenStationExists()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();

        var result = await SensorReadingTools.RecordStationOutput(sender, "F1", "S1", 42m, CancellationToken.None);

        Assert.True(result);
        Assert.Single(db.StationOutputRecords);
    }

    [Fact]
    public async Task RecordStationOutput_ReturnsFalse_WhenStationDoesNotExist()
    {
        var (sender, _) = TestFixture.Create();

        var result = await SensorReadingTools.RecordStationOutput(sender, "F1", "missing", 42m, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetLatestSensorReadings_GreenPath_ReturnsOneRowPerSensor_MostRecentOnly()
    {
        var (sender, db) = TestFixture.Create();
        var older = DateTime.UtcNow.AddMinutes(-5);
        var newer = DateTime.UtcNow;
        db.SensorReadings.Add(new SensorReading { FacilityId = "F1", StationId = "S1", EquipmentId = "EQ-1", SensorId = "temp", ReadingType = "Temperature", Value = 90m, ReadingAtUtc = older });
        db.SensorReadings.Add(new SensorReading { FacilityId = "F1", StationId = "S1", EquipmentId = "EQ-1", SensorId = "temp", ReadingType = "Temperature", Value = 95m, ReadingAtUtc = newer });
        await db.SaveChangesAsync();

        var result = await SensorReadingTools.GetLatestSensorReadings(sender, "F1", "S1", "EQ-1", CancellationToken.None);

        var reading = Assert.Single(result);
        Assert.Equal(95m, reading.Value);
    }

    [Fact]
    public async Task GetLatestSensorReadings_ReturnsEmpty_WhenNoReadingsExist()
    {
        var (sender, _) = TestFixture.Create();

        var result = await SensorReadingTools.GetLatestSensorReadings(sender, "F1", "S1", "EQ-1", CancellationToken.None);

        Assert.Empty(result);
    }
}
