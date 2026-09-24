using ModelContextProtocol;
using Telara.Domain.Entities;
using Telara.Mcp.Internal.Tools;

namespace Telara.Mcp.Internal.Tests.Tools;

public class StationToolsTests
{
    [Fact]
    public async Task RegisterStation_GreenPath_RegistersAndLinksPredecessor()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "UPSTREAM" });
        await db.SaveChangesAsync();

        var result = await StationTools.RegisterStation(sender, "F1", "DOWNSTREAM", ["UPSTREAM"], isLoadingDock: false, CancellationToken.None);

        Assert.Equal("DOWNSTREAM", result.StationId);
        Assert.Equal("DOWNSTREAM", (await db.Stations.FindAsync("F1", "UPSTREAM"))!.NextStationId);
    }

    [Fact]
    public async Task RegisterStation_Throws_McpException_WhenStationAlreadyExists()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            StationTools.RegisterStation(sender, "F1", "S1", [], isLoadingDock: false, CancellationToken.None));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public async Task RegisterStation_Throws_McpException_WhenPredecessorMissing()
    {
        var (sender, _) = TestFixture.Create();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            StationTools.RegisterStation(sender, "F1", "S1", ["missing"], isLoadingDock: false, CancellationToken.None));
        Assert.Contains("is not registered", ex.Message);
    }

    [Fact]
    public async Task RegisterStation_Throws_McpException_WhenPredecessorAlreadyLinked()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "UPSTREAM", NextStationId = "OTHER" });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            StationTools.RegisterStation(sender, "F1", "S1", ["UPSTREAM"], isLoadingDock: false, CancellationToken.None));
        Assert.Contains("cannot feed into a new successor", ex.Message);
    }

    [Fact]
    public async Task RegisterStationEquipment_GreenPath_RegistersEquipment()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        await db.SaveChangesAsync();

        var result = await StationTools.RegisterStationEquipment(sender, "F1", "S1", "EQ-1", 6, CancellationToken.None);

        Assert.Equal("EQ-1", result.EquipmentId);
        Assert.Equal(EquipmentStatus.Idle, result.Status);
    }

    [Fact]
    public async Task RegisterStationEquipment_Throws_McpException_WhenStationMissing()
    {
        var (sender, _) = TestFixture.Create();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            StationTools.RegisterStationEquipment(sender, "F1", "missing", "EQ-1", 6, CancellationToken.None));
        Assert.Contains("is not registered", ex.Message);
    }

    [Fact]
    public async Task RegisterStationEquipment_Throws_McpException_WhenAlreadyRegistered()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        db.StationEquipment.Add(new StationEquipment { FacilityId = "F1", StationId = "S1", EquipmentId = "EQ-1", EquipmentTypeId = 6, Status = EquipmentStatus.Idle });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            StationTools.RegisterStationEquipment(sender, "F1", "S1", "EQ-1", 6, CancellationToken.None));
        Assert.Contains("already registered", ex.Message);
    }
}
