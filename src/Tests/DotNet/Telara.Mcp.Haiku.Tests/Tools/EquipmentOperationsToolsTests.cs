using ModelContextProtocol;
using Telara.Mcp.Haiku.Tools;

namespace Telara.Mcp.Haiku.Tests.Tools;

public class EquipmentOperationsToolsTests
{
    [Fact]
    public async Task ClaimEquipmentLease_GreenPath_ReturnsInternalServerResponse()
    {
        var provider = TestFixture.CreateProvider(isError: false, content: """{"equipmentId":"EQ-1","leaseExpiresAtUtc":"2026-09-16T12:00:00Z"}""");

        var result = await EquipmentOperationsTools.ClaimEquipmentLease(
            provider, "F1", "S1", "EQ-1", 6, "instance-a", TimeSpan.FromMinutes(15), CancellationToken.None);

        Assert.Contains("EQ-1", result);
    }

    [Fact]
    public async Task ClaimEquipmentLease_Throws_McpException_WhenInternalServerReportsError()
    {
        var provider = TestFixture.CreateProvider(isError: true, content: "Equipment EQ-1 is already operational under another instance.");

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentOperationsTools.ClaimEquipmentLease(
                provider, "F1", "S1", "EQ-1", 6, "instance-b", TimeSpan.FromMinutes(15), CancellationToken.None));

        Assert.Equal("Equipment EQ-1 is already operational under another instance.", ex.Message);
    }

    [Fact]
    public async Task IngestSensorReadings_GreenPath_ReturnsInternalServerResponse()
    {
        var provider = TestFixture.CreateProvider(isError: false, content: "true");

        var result = await EquipmentOperationsTools.IngestSensorReadings(
            provider, "F1", "S1", "EQ-1", [new EquipmentReadingInput("temp", "Temperature", 98.6m)], CancellationToken.None);

        Assert.Equal("true", result);
    }

    [Fact]
    public async Task IngestSensorReadings_Throws_McpException_WhenEquipmentNotRegistered()
    {
        var provider = TestFixture.CreateProvider(isError: true, content: "Station S1 at facility F1 is not registered.");

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentOperationsTools.IngestSensorReadings(
                provider, "F1", "S1", "missing", [new EquipmentReadingInput("temp", "Temperature", 98.6m)], CancellationToken.None));

        Assert.Contains("is not registered", ex.Message);
    }

    [Fact]
    public async Task IngestStationOutput_GreenPath_ReturnsInternalServerResponse()
    {
        var provider = TestFixture.CreateProvider(isError: false, content: "true");

        var result = await EquipmentOperationsTools.IngestStationOutput(provider, "F1", "S1", 42.5m, CancellationToken.None);

        Assert.Equal("true", result);
    }

    [Fact]
    public async Task IngestStationOutput_Throws_McpException_OnUnexpectedFailure()
    {
        var provider = TestFixture.CreateProvider(isError: true, content: "Something unexpected happened.");

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            EquipmentOperationsTools.IngestStationOutput(provider, "F1", "S1", 42.5m, CancellationToken.None));

        Assert.Equal("Something unexpected happened.", ex.Message);
    }
}
