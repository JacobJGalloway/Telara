using ModelContextProtocol;
using Telara.Mcp.Haiku.Tools;

namespace Telara.Mcp.Haiku.Tests.Tools;

public class EquipmentRegistrationToolsTests
{
    [Fact]
    public async Task RegisterEquipment_GreenPath_ReturnsInternalServerResponse()
    {
        var provider = TestFixture.CreateProvider(isError: false,
            content: """{"facilityId":"F1","stationId":"S1","equipmentId":"EQ-1","equipmentTypeId":6,"status":"Idle"}""");

        var result = await EquipmentRegistrationTools.RegisterEquipment(provider, "F1", "S1", "EQ-1", 6, CancellationToken.None);

        Assert.Contains("EQ-1", result);
    }

    [Fact]
    public async Task RegisterEquipment_Throws_McpException_WhenAlreadyRegistered()
    {
        var provider = TestFixture.CreateProvider(isError: true, content: "Equipment EQ-1 at station S1 is already registered.");

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentRegistrationTools.RegisterEquipment(provider, "F1", "S1", "EQ-1", 6, CancellationToken.None));

        Assert.Equal("Equipment EQ-1 at station S1 is already registered.", ex.Message);
    }

    [Fact]
    public async Task RegisterEquipment_Throws_McpException_WithGenericMessage_WhenInternalServerOmitsText()
    {
        var provider = TestFixture.CreateProvider(isError: true, content: null);

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentRegistrationTools.RegisterEquipment(provider, "F1", "S1", "EQ-1", 6, CancellationToken.None));

        Assert.Equal("register_station_equipment failed on the Internal Functionality Server.", ex.Message);
    }
}
