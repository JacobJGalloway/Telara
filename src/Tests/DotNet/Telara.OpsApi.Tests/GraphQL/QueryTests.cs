using Telara.Domain.Entities;

namespace Telara.OpsApi.Tests.GraphQL;

public class QueryTests
{
    [Fact]
    public async Task GetStations_ReturnsStationsForFacility_ExcludingOtherFacilities()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1", IsLoadingDock = true });
        db.Stations.Add(new Station { FacilityId = "F2", StationId = "OTHER" });
        await db.SaveChangesAsync();

        var result = await Telara.OpsApi.GraphQL.Query.GetStations(sender, "F1", CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Equal("S1", summary.StationId);
        Assert.True(summary.IsLoadingDock);
    }

    [Fact]
    public async Task GetEquipmentTypes_ReturnsSeededTypes()
    {
        var (sender, db) = TestFixture.Create();
        db.EquipmentTypes.Add(new EquipmentType { Id = 1, Name = "Conveyor" });
        await db.SaveChangesAsync();

        var result = await Telara.OpsApi.GraphQL.Query.GetEquipmentTypes(sender);

        Assert.Contains(result, t => t.Name == "Conveyor");
    }

    [Fact]
    public async Task GetStationWorkflow_WalksChain_ToLoadingDock()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "A", NextStationId = "B" });
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "B", IsLoadingDock = true });
        await db.SaveChangesAsync();

        var result = await Telara.OpsApi.GraphQL.Query.GetStationWorkflow(sender, "F1", "A", CancellationToken.None);

        Assert.Equal(["A", "B"], result.Select(s => s.StationId));
        Assert.True(result[^1].IsLoadingDock);
    }

    [Fact]
    public async Task GetStationOutput_ReturnsRecordedOutput()
    {
        var (sender, db) = TestFixture.Create();
        db.Stations.Add(new Station { FacilityId = "F1", StationId = "S1" });
        db.StationOutputRecords.Add(new StationOutputRecord { FacilityId = "F1", StationId = "S1", UnitsProduced = 10m, RecordedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await Telara.OpsApi.GraphQL.Query.GetStationOutput(sender);

        Assert.Single(result);
    }
}
