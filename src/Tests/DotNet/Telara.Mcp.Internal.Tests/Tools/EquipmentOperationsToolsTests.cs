using ModelContextProtocol;
using Telara.Domain.CQRS.Commands;
using Telara.Domain.Entities;
using Telara.Mcp.Internal.Tools;

namespace Telara.Mcp.Internal.Tests.Tools;

public class EquipmentOperationsToolsTests
{
    [Fact]
    public async Task ClaimEquipmentLease_GreenPath_ClaimsOnFirstCall()
    {
        var (sender, _) = TestFixture.Create();

        var result = await EquipmentOperationsTools.ClaimEquipmentLease(
            sender, "F1", "S1", "EQ-1", 6, "instance-a", TimeSpan.FromMinutes(15), CancellationToken.None);

        Assert.Equal("EQ-1", result.EquipmentId);
        Assert.True(result.LeaseExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task ClaimEquipmentLease_Throws_McpException_WhenAlreadyOperationalUnderAnotherInstance()
    {
        var (sender, db) = TestFixture.Create();
        db.StationEquipment.Add(new StationEquipment
        {
            FacilityId = "F1",
            StationId = "S1",
            EquipmentId = "EQ-1",
            EquipmentTypeId = 6,
            Status = EquipmentStatus.Operational,
            ActiveInstanceId = "instance-a",
            LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentOperationsTools.ClaimEquipmentLease(
                sender, "F1", "S1", "EQ-1", 6, "instance-b", TimeSpan.FromMinutes(15), CancellationToken.None));
        Assert.NotEmpty(ex.Message);
    }

    [Fact]
    public async Task RecordSensorReadings_GreenPath_ReturnsTrue_WhenEquipmentRegistered()
    {
        var (sender, db) = TestFixture.Create();
        db.StationEquipment.Add(new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "EQ-1", EquipmentTypeId = 6, Status = EquipmentStatus.Idle });
        await db.SaveChangesAsync();

        var result = await EquipmentOperationsTools.RecordSensorReadings(
            sender, "F1", "S1", "EQ-1", [new SensorReadingInput("temp", "Temperature", 98.6m)], CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RecordSensorReadings_ReturnsFalse_WhenEquipmentNotRegistered()
    {
        var (sender, _) = TestFixture.Create();

        var result = await EquipmentOperationsTools.RecordSensorReadings(
            sender, "F1", "S1", "missing", [new SensorReadingInput("temp", "Temperature", 98.6m)], CancellationToken.None);

        Assert.False(result);
    }
}
