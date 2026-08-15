namespace Telara.OpsApi.GraphQL.Types;

public record LatestSensorReading(string SensorId, string ReadingType, decimal Value, DateTime ReadingAtUtc);
